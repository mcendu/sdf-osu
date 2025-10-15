/*
Copyright (c) 2025 McEndu.

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System.Runtime.InteropServices;
using FreeTypeSharp;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_Error;
using static FreeTypeSharp.FT_Kerning_Mode_;
using static FreeTypeSharp.FT_LOAD;
using static FreeTypeSharp.FT_Render_Mode_;

namespace Sdf.IO.Stores;

/// <summary>
/// A basic glyph store that will rasterize glyphs from outlines every character retrieval.
/// </summary>
public class OutlineGlyphStore : IGlyphStore, IResourceStore<TextureUpload>, IDisposable
{
    /// <summary>
    /// The spread of the FreeType generated signed distance fields.
    /// </summary>
    /// <remarks>
    /// This is equal to the default spread value defined by FreeType.
    /// </remarks>
    /// <seealso href="https://freetype.org/freetype2/docs/reference/ft2-properties.html#spread"/>
    private const int SDF_SPREAD = 8;

    /// <summary>
    /// The hardcoded baseline value for mapping FreeType metrics into BMFont metrics.
    /// </summary>
    private const float BASELINE = 85.0f;

    /// <summary>
    /// An instance of the FreeType library.
    /// </summary>
    private static readonly unsafe FT_LibraryRec_* freeType;

    /// <summary>
    /// Locks <see cref="freeType"/> for opening and closing fonts.
    /// </summary>
    private static readonly Lock freeTypeLock = new Lock();

    /// <summary>
    /// The underlying unmanaged font handle.
    /// </summary>
    private unsafe FT_FaceRec_* Face => (FT_FaceRec_*)completionSource.Task.GetResultSafely();

    /// <summary>
    /// Locks <see cref="Face"/> and its glyph slot for exclusive access. 
    /// </summary>
    private readonly Lock faceLock = new Lock();

    private readonly TaskCompletionSource<nint> completionSource = new TaskCompletionSource<nint>();

    protected readonly string? AssetName;

    /// <summary>
    /// The resolution of the distance fields in pixels per em.
    /// </summary>
    public uint Resolution { get; init; } = 100;

    /// <summary>
    /// The index of the face to use.
    /// </summary>
    public int FaceIndex { private get; init; } = 0;

    /// <summary>
    /// The index of the named instance to use if the font is a variable font.
    /// Ignored for non-variable fonts.
    /// </summary>
    public int NamedInstance { private get; init; } = 0;

    protected readonly ResourceStore<byte[]> Store;

    public string FontName { get; private set; }

    public float? Baseline => BASELINE;

    /// <summary>
    /// Initializes FreeType for loading outline fonts.
    /// </summary>
    /// <exception cref="FreeTypeException">FreeType failed to initialize.</exception>
    static unsafe OutlineGlyphStore()
    {
        FT_Error error;

        fixed (FT_LibraryRec_** pp = &freeType)
        {
            error = FT_Init_FreeType(pp);
        }

        if (error != FT_Err_Ok) throw new FreeTypeException(error);
    }

    public OutlineGlyphStore(ResourceStore<byte[]> store, string? assetName = null)
    {
        Store = new ResourceStore<byte[]>(store);

        Store.AddExtension("ttf");
        Store.AddExtension("otf");
        Store.AddExtension("woff");
        Store.AddExtension("woff2");
        Store.AddExtension("ttc");

        AssetName = assetName;

        // Assign tentative resource name before a proper PostScript name is available.
        FontName = assetName?.Split('/').Last() ?? string.Empty;
    }

    ~OutlineGlyphStore()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected unsafe virtual void Dispose(bool isDisposing)
    {
        if (Face is not null)
        {
            lock (freeTypeLock)
            {
                FT_Done_Face(Face);
            }
        }
    }

    /// <summary>
    /// Opens the underlying font for processing.
    /// </summary>
    /// <exception cref="FileNotFoundException">The requested font does not exist.</exception>
    /// <exception cref="FreeTypeException">FreeType refused to open the font.</exception>
    private unsafe void LoadFont()
    {
        try
        {
            Stream? s = Store.GetStream(AssetName) ?? throw new FileNotFoundException();
            var handle = GCHandle.Alloc(s);
            FT_FaceRec_* face = null;
            FT_StreamRec_* ftStream = null;

            // open the font
            try
            {
                // HACK: work around bugs in bindings and ABI differences.
                if (OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    var stream = (FTStreamWindows*)NativeMemory.AllocZeroed((nuint)sizeof(FTStreamWindows));
                    stream->size = 0x7FFFFFFF;
                    stream->descriptor = (nint)handle;
                    stream->read = &StreamReadCallback;
                    stream->close = &StreamCloseCallback;

                    ftStream = (FT_StreamRec_*)stream;
                }
                else
                {
                    var stream = (FTStream*)NativeMemory.AllocZeroed((nuint)sizeof(FTStream));
                    stream->size = 0x7FFFFFFF;
                    stream->descriptor = (nint)handle;
                    stream->read = &StreamReadCallback;
                    stream->close = &StreamCloseCallback;

                    ftStream = (FT_StreamRec_*)stream;
                }
            }
            catch (Exception)
            {
                s?.Dispose();
                handle.Free();
                throw;
            }


            var openArgs = new FT_Open_Args_
            {
                flags = 0x2, // FT_OPEN_STREAM
                stream = ftStream,
                num_params = 0,
            };

            nint compoundFaceIndex = (ushort)FaceIndex | (NamedInstance << 16);

            try
            {
                FT_Error error;

                lock (freeTypeLock)
                {
                    error = FT_Open_Face(freeType, &openArgs, compoundFaceIndex, &face);
                }

                if (error != 0) throw new FreeTypeException(error);

                // set pixel size
                error = FT_Set_Pixel_Sizes(face, 0, Resolution);

                if (error != 0) throw new FreeTypeException(error);

                // get font name
                FontName = Marshal.PtrToStringUTF8((nint)FT_Get_Postscript_Name(face)) ?? FontName;

                completionSource.SetResult((nint)face);
            }
            catch (Exception)
            {
                // At this point FreeType owns all unmanaged resources allocated above, and
                // FT_Done_Face should release them all.
                if (face is not null)
                {
                    lock (freeTypeLock)
                    {
                        FT_Done_Face(face);
                    }
                }

                throw;
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, $"Couldn't load font asset from {AssetName}.");
            completionSource.SetResult(0);
            throw;
        }
    }

    /// <summary>
    /// Stream read and seek callback used by FreeType.
    /// </summary>
    [UnmanagedCallersOnly]
    private static unsafe nuint StreamReadCallback(FT_StreamRec_* ftStream, nuint offset, byte* buffer, nuint count)
    {
        try
        {
            Stream s;

            // HACK: work around bugs in bindings and ABI differences.
            if (OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                var actualFtStream = (FTStreamWindows*)ftStream;
                s = (Stream)((GCHandle)actualFtStream->descriptor).Target!;
            }
            else
            {
                var actualFtStream = (FTStream*)ftStream;
                s = (Stream)((GCHandle)actualFtStream->descriptor).Target!;
            }

            s.Seek((long)offset, SeekOrigin.Begin);

            if (count != 0)
            {
                nuint len = (nuint)s.Read(new Span<byte>(buffer, (int)count));
                return len;
            }
            else
            {
                // Caller expects seek only operations return 0 on success.
                return 0;
            }
        }
        catch (Exception)
        {
            // The caller excepts status to be reported in the return value
            // instead of as an exception. It expects non-zero to be returned
            // for failed seek-only operations, and zero for failed seek-and-
            // read operations.
            return count == 0 ? 1u : 0;
        }
    }

    /// <summary>
    /// Stream close callback used by FreeType.
    /// </summary>
    [UnmanagedCallersOnly]
    private static unsafe void StreamCloseCallback(FT_StreamRec_* ftStream)
    {
        GCHandle handle;

        // HACK: work around bugs in bindings and ABI differences.
        if (OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            var actualFtStream = (FTStreamWindows*)ftStream;
            handle = (GCHandle)actualFtStream->descriptor;
        }
        else
        {
            var actualFtStream = (FTStream*)ftStream;
            handle = (GCHandle)actualFtStream->descriptor;
        }

        var s = (Stream?)handle.Target;

        handle.Free();
        s?.Dispose();

        // FreeType allows a `FT_Stream *` to free itself here.
        NativeMemory.Free(ftStream);
    }

    public Task LoadFontAsync() => Task.Run(LoadFont);

    /// <summary>
    /// Whether a glyph exists for the specified character in this store.
    /// </summary>
    public unsafe bool HasGlyph(uint c)
    {
        if (Face is null)
            return false;

        // FreeType presents an (actual or emulated) Unicode charmap by default.
        return FT_Get_Char_Index(Face, c) != 0;
    }

    bool IGlyphStore.HasGlyph(char c) => HasGlyph(c);

    /// <summary>
    /// Load metrics for a glyph.
    /// </summary>
    /// <param name="glyphIndex">The index of the glyph.</param>
    /// <returns>A new <see cref="GlyphMetrics"/> containing the glyph metrics.</returns>
    /// <exception cref="FreeTypeException">The metrics fails to load.</exception>
    protected virtual unsafe GlyphMetrics LoadMetrics(uint glyphIndex)
    {
        FT_Error error;
        nint horiBearingX;
        nint horiBearingY;
        nint horiAdvance;

        lock (faceLock)
        {
            error = FT_Load_Glyph(Face, glyphIndex, FT_LOAD_NO_BITMAP | FT_LOAD_NO_HINTING);
            horiBearingX = Face->glyph->metrics.horiBearingX;
            horiBearingY = Face->glyph->metrics.horiBearingY;
            horiAdvance = Face->glyph->metrics.horiAdvance;
        }

        if (error != 0)
        {
            throw new FreeTypeException(error);
        }

        // FreeType outputs metric data in 26.6 fixed point. Convert to floating point accordingly.
        FT_Glyph_Metrics_* ftMetrics = &Face->glyph->metrics;
        float xOffset = (horiBearingX / 64.0f) - SDF_SPREAD;
        float yOffset = BASELINE - (horiBearingY / 64.0f) - SDF_SPREAD;
        float advance = horiAdvance / 64.0f;

        return new GlyphMetrics
        {
            XOffset = xOffset,
            YOffset = yOffset,
            Advance = advance,
        };
    }

    /// <summary>
    /// Retrieves a <see cref="CharacterGlyph"/> that contains associated spacing information for a character.
    /// </summary>
    /// <param name="character">The character to retrieve the <see cref="CharacterGlyph"/> for.</param>
    /// <returns>The <see cref="CharacterGlyph"/> containing associated spacing information for <paramref name="character"/>.</returns>
    public unsafe CharacterGlyph? Get(uint c)
    {
        if (Face is null)
            return null;

        try
        {
            var glyph = LoadMetrics(FT_Get_Char_Index(Face, c));
            return new CharacterGlyph((char)c, glyph.XOffset, glyph.YOffset, glyph.Advance, BASELINE, this);
        }
        catch (FreeTypeException e)
        {
            Logger.Log($"Failed to load glyph for U+{(int)c:X4}: {e.Message}");
            return null;
        }
    }

    CharacterGlyph? IGlyphStore.Get(char c) => Get(c);

    public unsafe int GetKerning(char left, char right)
    {
        if (Face is null)
            return 0;

        FT_Vector_ kerning;
        uint indexLeft = FT_Get_Char_Index(Face, left);
        uint indexRight = FT_Get_Char_Index(Face, right);
        var error = FT_Get_Kerning(Face, indexLeft, indexRight, FT_KERNING_DEFAULT, &kerning);

        if (error != 0)
            return 0;

        // osu!framework only supports horizontal kerning in integral values.
        return (int)Math.Round(kerning.x / 64.0);
    }

    Task<CharacterGlyph> IResourceStore<CharacterGlyph>.GetAsync(string name, CancellationToken cancellationToken) =>
    Task.Run(() => ((IGlyphStore)this).Get(name[0]), cancellationToken)!;

    CharacterGlyph IResourceStore<CharacterGlyph>.Get(string name) => Get(name[0])!;

    public unsafe TextureUpload Get(string name)
    {
        if (Face is null)
            return null!;

        if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
            return null!;

        uint codePoint = name.Last();
        return GetCharTexture((nint)Face, codePoint);
    }

    public async Task<TextureUpload> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
            return null!;

        uint codePoint = name.Last();
        var face = await completionSource.Task.ConfigureAwait(false);

        return GetCharTexture(face, codePoint);
    }

    /// <summary>
    /// Get the <see cref="TextureUpload"/> for rendering a character.
    /// </summary>
    /// <param name="face">The unmanaged object representing the loaded font.</param>
    /// <param name="codePoint">The code point to render.</param>
    private unsafe TextureUpload GetCharTexture(nint face, uint codePoint)
    {
        uint index = FT_Get_Char_Index((FT_FaceRec_*)face, codePoint);
        return GetGlyphTexture(face, index);
    }

    /// <summary>
    /// Get the <see cref="TextureUpload"/> for rendering a glyph.
    /// </summary>
    /// <param name="ptr">A handle representing the loaded font.</param>
    /// <param name="glyphIndex">The glyph's index in the font.</param>
    /// <exception cref="FreeTypeException">Rasterization of the texture failed.</exception>
    protected virtual unsafe TextureUpload GetGlyphTexture(nint ptr, uint glyphIndex)
    {
        Image<Rgba32> image;
        FT_Error error;
        var face = (FT_FaceRec_*)ptr;

        // rasterize
        lock (faceLock)
        {
            error = FT_Load_Glyph(face, glyphIndex, FT_LOAD_NO_BITMAP | FT_LOAD_NO_HINTING | FT_LOAD_RENDER);

            if (error != 0) throw new FreeTypeException(error);

            error = FT_Render_Glyph(face->glyph, FT_RENDER_MODE_SDF);

            if (error != 0) throw new FreeTypeException(error);

            // copy to TextureUpload
            var ftBitmap = &face->glyph->bitmap;
            int width = ftBitmap->width != 0 ? (int)ftBitmap->width : 1;
            int height = ftBitmap->rows != 0 ? (int)ftBitmap->rows : 1;
            image = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, byte.MaxValue));

            for (int y = 0; y < ftBitmap->rows; ++y)
            {
                var srcRow = new ReadOnlySpan<byte>(ftBitmap->buffer + y * ftBitmap->pitch, ftBitmap->pitch);
                var dstRow = image.DangerousGetPixelRowMemory(y).Span;

                for (int x = 0; x < ftBitmap->width; ++x)
                {
                    dstRow[x] = new Rgba32(srcRow[x], srcRow[x], srcRow[x], byte.MaxValue);
                }
            }
        }

        return new TextureUpload(image);
    }

    public Stream GetStream(string name) => throw new NotSupportedException();

    public IEnumerable<string> GetAvailableResources()
    {
        uint codePoint;
        uint glyphIndex;

        (codePoint, glyphIndex) = GetFirstChar();

        while (glyphIndex != 0)
        {
            yield return $"{FontName}/{(char)codePoint}";
            (codePoint, glyphIndex) = GetNextChar(codePoint);
        }

        unsafe (uint codePoint, uint glyphIndex) GetFirstChar()
        {
            if (Face is null)
                return (0, 0);

            uint glyphIndex;
            uint codePoint = (uint)FT_Get_First_Char(Face, &glyphIndex);
            return (codePoint, glyphIndex);
        }

        unsafe (uint codePoint, uint glyphIndex) GetNextChar(uint prevCodePoint)
        {
            if (Face is null)
                return (0, 0);

            uint glyphIndex;
            uint codePoint = (uint)FT_Get_Next_Char(Face, prevCodePoint, &glyphIndex);
            return (codePoint, glyphIndex);
        }
    }

    /// <summary>
    /// The layout of FT_Stream on systems where sizeof(unsigned long) == sizeof(size_t).
    /// </summary>
    /// <remarks>
    /// Applies to most platforms, including most Unixes and ARM64 Windows.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct FTStream
    {
        public nint _base;
        public nuint size;
        public nuint pos;
        public nint descriptor;
        public nint pathname;
        public delegate* unmanaged<FT_StreamRec_*, nuint, byte*, nuint, nuint> read;
        public delegate* unmanaged<FT_StreamRec_*, void> close;
        public nint memory;
        public nint cursor;
        public nint limit;
    };

    /// <summary>
    /// The layout of FT_Stream on systems where sizeof(unsigned long) == 4.
    /// </summary>
    /// <remarks>
    /// Applies to x64 Windows. While it also applies to FreeType compiled
    /// against the ARM64EC ABI, this case is not supported.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct FTStreamWindows
    {
        public nint _base;
        public uint size;
        public uint pos;
        public nint descriptor;
        public nint pathname;
        public delegate* unmanaged<FT_StreamRec_*, nuint, byte*, nuint, nuint> read;
        public delegate* unmanaged<FT_StreamRec_*, void> close;
        public nint memory;
        public nint cursor;
        public nint limit;
    };

    /// <summary>
    /// Data used to lay out and render a character.
    /// </summary>
    protected class GlyphMetrics
    {
        /// <summary>
        /// The horizontal bearing of the character in pixels.
        /// </summary>
        public required float XOffset { get; init; }

        /// <summary>
        /// The vertical offset of the character in pixels, relative to the top of the glyph bounding box.
        /// </summary>
        /// <remarks>
        /// This is the semantics used by BMFont and osu!framework, different from FreeType where the
        /// vertical bearing is relative to the baseline.
        /// </remarks>
        public required float YOffset { get; init; }

        /// <summary>
        /// The horizontal advance of the character in pixels.
        /// </summary>
        public required float Advance { get; init; }
    }
}
