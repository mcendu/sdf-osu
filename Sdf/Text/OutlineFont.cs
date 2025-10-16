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
using static FreeTypeSharp.FT_FACE_FLAG;
using static FreeTypeSharp.FT_Kerning_Mode_;
using static FreeTypeSharp.FT_LOAD;
using static FreeTypeSharp.FT_Render_Mode_;
using static Sdf.Text.FreeType.MM;

namespace Sdf.Text;

/// <summary>
/// Handles outline fonts using FreeType.
/// </summary>
public class OutlineFont : IDisposable
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
    /// Hardcoded baseline value for mapping FreeType metrics into BMFont metrics.
    /// </summary>
    public const float BASELINE = 85.0f;

    /// <summary>
    /// An instance of the FreeType library.
    /// </summary>
    private static readonly unsafe FT_LibraryRec_* library;

    /// <summary>
    /// Locks <see cref="library"/> for opening and closing fonts.
    /// </summary>
    private static readonly Lock libraryLock = new Lock();

    /// <summary>
    /// The underlying unmanaged font handle.
    /// </summary>
    private unsafe FT_FaceRec_* Face => (FT_FaceRec_*)completionSource.Task.GetResultSafely();

    /// <summary>
    /// Locks <see cref="Face"/> and its glyph slot for exclusive access. 
    /// </summary>
    private readonly Lock faceLock = new Lock();

    private readonly TaskCompletionSource<nint> completionSource = new TaskCompletionSource<nint>();

    /// <summary>
    /// The name of the underlying asset.
    /// </summary>
    public string AssetName { get; }

    /// <summary>
    /// The index of the face to use.
    /// </summary>
    public int FaceIndex { get; }

    /// <summary>
    /// The resolution of the rendered glyphs in pixels per em.
    /// </summary>
    public uint Resolution { get; init; } = 64;

    protected readonly ResourceStore<byte[]> Store;

    /// <summary>
    /// Initialize FreeType for loading outline fonts.
    /// </summary>
    /// <exception cref="FreeTypeException">FreeType failed to initialize.</exception>
    static unsafe OutlineFont()
    {
        FT_Error error;

        fixed (FT_LibraryRec_** pp = &library)
        {
            error = FT_Init_FreeType(pp);
        }

        if (error != FT_Err_Ok) throw new FreeTypeException(error);
    }

    /// <summary>
    /// Open an outline font.
    /// </summary>
    /// <param name="store">The resource store to use.</param>
    /// <param name="assetName">The font to open.</param>
    /// <param name="faceIndex">The index of the face to use.</param>
    public OutlineFont(ResourceStore<byte[]> store, string assetName, int faceIndex = 0)
    {
        Store = new ResourceStore<byte[]>(store);

        Store.AddExtension("ttf");
        Store.AddExtension("otf");
        Store.AddExtension("woff");
        Store.AddExtension("ttc");

        AssetName = assetName;
        FaceIndex = faceIndex;
    }

    ~OutlineFont()
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
        if (completionSource.Task.IsCompletedSuccessfully)
        {
            lock (libraryLock)
            {
                FT_Done_Face(Face);
            }
        }
    }

    /// <summary>
    /// Load the requested font.
    /// </summary>
    /// <exception cref="FileNotFoundException">The requested font does not exist.</exception>
    /// <exception cref="FreeTypeException">FreeType refused to open the font.</exception>
    public unsafe void Load()
    {
        try
        {
            // check if the requested font is loading or loaded
            if (completionSource.Task.IsCompleted || !faceLock.TryEnter())
            {
                completionSource.Task.Wait();
                return;
            }

            Stream? s = Store.GetStream(AssetName) ?? throw new FileNotFoundException();
            var handle = GCHandle.Alloc(s);
            FT_FaceRec_* face = null;
            FT_StreamRec_* ftStream = null;

            // set up unmanaged object to allow use of stream from FreeType
            try
            {
                var stream = (FTStream*)NativeMemory.AllocZeroed((nuint)sizeof(FTStream));
                stream->size = new CULong((nuint)s.Length);
                stream->descriptor.pointer = (nint)handle;
                stream->read = &StreamReadCallback;
                stream->close = &StreamCloseCallback;

                ftStream = (FT_StreamRec_*)stream;
            }
            catch (Exception)
            {
                s?.Dispose();
                handle.Free();
                throw;
            }

            // open the font
            var openArgs = new FT_Open_Args_
            {
                flags = 0x2, // FT_OPEN_STREAM
                stream = ftStream,
                num_params = 0,
            };

            try
            {
                FT_Error error;

                lock (libraryLock)
                {
                    error = FT_Open_Face(library, &openArgs, FaceIndex, &face);
                }

                if (error != 0) throw new FreeTypeException(error);

                // set pixel size
                error = FT_Set_Pixel_Sizes(face, 0, Resolution);

                if (error != 0) throw new FreeTypeException(error);

                completionSource.SetResult((nint)face);
            }
            catch (Exception)
            {
                // At this point FreeType owns all unmanaged resources allocated above, and
                // FT_Done_Face should release them all.
                if (face is not null)
                {
                    lock (libraryLock)
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
            completionSource.SetException(e);
            throw;
        }
        finally
        {
            faceLock.Exit();
        }
    }

    public Task LoadAsync() => Task.Run(Load);

    /// <summary>
    /// Stream read and seek callback used by FreeType.
    /// </summary>
    [UnmanagedCallersOnly]
    private static unsafe CULong StreamReadCallback(FTStream* ftStream, CULong offset, byte* buffer, CULong count)
    {
        try
        {
            var s = (Stream)((GCHandle)ftStream->descriptor.pointer).Target!;

            s.Seek((long)offset.Value, SeekOrigin.Begin);

            if (count.Value != 0)
            {
                return new CULong((uint)s.Read(new Span<byte>(buffer, (int)count.Value)));
            }
            else
            {
                // Caller expects seek only operations return 0 on success.
                return new CULong(0);
            }
        }
        catch (Exception)
        {
            // The caller excepts status to be reported in the return value
            // instead of as an exception. It expects non-zero to be returned
            // for failed seek-only operations, and zero for failed seek-and-
            // read operations.
            return new CULong(count.Value == 0 ? 1u : 0);
        }
    }

    /// <summary>
    /// Stream close callback used by FreeType.
    /// </summary>
    [UnmanagedCallersOnly]
    private static unsafe void StreamCloseCallback(FTStream* ftStream)
    {
        var handle = (GCHandle)ftStream->descriptor.pointer;

        var s = (Stream?)handle.Target;

        handle.Free();
        s?.Dispose();

        // FreeType allows a `FT_Stream *` to free itself here.
        NativeMemory.Free(ftStream);
    }

    /// <summary>
    /// An object used by FreeType for reading data.
    /// </summary>
    /// <remarks>
    /// This is a part of the workaround for a bug in FreeTypeSharp. Remove
    /// this struct and the workaround when the bug is fixed.
    /// </remarks>
    /// <seealso href="https://github.com/ryancheung/FreeTypeSharp/issues/31"/> 
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct FTStream
    {
        public nint _base;
        public CULong size;
        public CULong pos;
        public FTStreamDesc descriptor;
        public FTStreamDesc pathname;
        public delegate* unmanaged<FTStream*, CULong, byte*, CULong, CULong> read;
        public delegate* unmanaged<FTStream*, void> close;
        public nint memory;
        public nint cursor;
        public nint limit;
    };

    /// <summary>
    /// A union type used to store a handle to the input stream.
    /// </summary>
    /// <remarks>
    /// This is a part of the workaround for a bug in FreeTypeSharp. Remove
    /// this struct and the workaround when the bug is fixed.
    /// </remarks>
    /// <seealso href="https://github.com/ryancheung/FreeTypeSharp/issues/31"/> 
    [StructLayout(LayoutKind.Explicit)]
    private unsafe struct FTStreamDesc
    {
        [FieldOffset(0)] public CLong value;
        [FieldOffset(0)] public nint pointer;
    }

    /// <summary>
    /// Get the glyph index of a character.
    /// </summary>
    /// <param name="c">A character.</param>
    /// <returns>
    /// <para>
    /// The character's glyph index, on success.
    /// </para>
    /// <para>
    /// If the font is not loaded, or if the does not contain a glyph for the
    /// character in question, returns 0.
    /// </para>
    /// </returns>
    public unsafe uint GetGlyphIndex(int c)
    {
        if (!completionSource.Task.IsCompletedSuccessfully)
            return 0;

        lock (faceLock)
        {
            return FT_Get_Char_Index(Face, (uint)c);
        }
    }

    /// <summary>
    /// Get the glyph index of a character asynchronously.
    /// </summary>
    /// <param name="c">A character.</param>
    /// <returns>
    /// <para>
    /// The character's glyph index, on success.
    /// </para>
    /// <para>
    /// If the font is not loaded, or if the does not contain a glyph for the
    /// character in question, returns 0.
    /// </para>
    /// </returns>
    public async Task<uint> GetGlyphIndexAsync(int c)
    {
        var face = await completionSource.Task.ConfigureAwait(false);

        unsafe
        {
            lock (faceLock)
            {
                return FT_Get_Char_Index((FT_FaceRec_*)face, (uint)c);
            }
        }
    }

    /// <summary>
    /// Whether the font has a glyph for the specified character.
    /// </summary>
    /// <param name="c">A character.</param>
    /// <returns>
    /// If the font has the specified character, returns true. Otherwise,
    /// returns false.
    /// </returns>
    public bool HasGlyph(int c) => GetGlyphIndex(c) != 0;

    /// <summary>
    /// Set the parameters of the font.
    /// </summary>
    /// <param name="face">An unmanaged face object.</param>
    /// <param name="variation">
    /// The parameters of the font. If null, the default parameters will be
    /// used. This parameter must be null if the font is static.
    /// </param>
    /// <exception cref="FreeTypeException">
    /// The font parameters are invalid.
    /// </exception>
    /// <remarks>
    /// This method is not thread safe. The caller is responsible for locking
    /// <see cref="faceLock"/> before calling.
    /// </remarks>
    private static unsafe void SetVariation(FT_FaceRec_* face, FontVariation? variation)
    {
        if (variation is null)
        {
            // This check is needed to support non-variable fonts.
            if (((FT_FACE_FLAG)face->face_flags).HasFlag(FT_FACE_FLAG_MULTIPLE_MASTERS))
            {
                uint defaultInstance;
                var error = FT_Get_Default_Named_Instance(face, &defaultInstance);

                if (error != 0) throw new FreeTypeException(error);

                error = FT_Set_Named_Instance(face, defaultInstance);

                if (error != 0) throw new FreeTypeException(error);
            }
        }
        else if (variation.Axes is not null)
        {
            FT_Error error;

            fixed (CLong* p = variation.Axes)
            {
                error = FT_Set_Var_Design_Coordinates(face, (uint)variation.Axes.Length, p);
            }

            if (error != 0) throw new FreeTypeException(error);
        }
        else
        {
            var error = FT_Set_Named_Instance(face, variation.NamedInstance);

            if (error != 0) throw new FreeTypeException(error);
        }
    }

    /// <summary>
    /// Load metrics for a glyph.
    /// </summary>
    /// <param name="glyphIndex">The index of the glyph.</param>
    /// <param name="variation">
    /// The parameters of the font. If null, the default parameters will be
    /// used. This parameter must be null if the font is static.
    /// </param>
    /// <returns>A new <see cref="CharacterGlyph"/> containing the glyph metrics.</returns>
    /// <exception cref="FreeTypeException">The metrics fails to load.</exception>
    public unsafe CharacterGlyph? GetMetrics(uint glyphIndex, FontVariation? variation)
    {
        if (!completionSource.Task.IsCompletedSuccessfully)
            return null;

        FT_Error error;
        nint horiBearingX;
        nint horiBearingY;
        nint horiAdvance;

        lock (faceLock)
        {
            SetVariation(Face, variation);
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
        float xOffset = (horiBearingX / 64.0f) - SDF_SPREAD;
        float yOffset = BASELINE - (horiBearingY / 64.0f) - SDF_SPREAD;
        float advance = horiAdvance / 64.0f;

        // The noncharacter indicates that the original character is not available.
        return new CharacterGlyph('\uffff', xOffset, yOffset, advance, BASELINE, null);
    }

    /// <summary>
    /// Get the kerning between two glyphs.
    /// </summary>
    /// <param name="left">The glyph index of the left glyph.</param>
    /// <param name="right">The glyph index of the right glyph.</param>
    /// <param name="variation">
    /// The parameters of the font. If null, the default parameters will be
    /// used. This parameter must be null if the font is static.
    /// </param>
    /// <returns>The amount of kerning.</returns>
    public unsafe int GetKerning(uint left, uint right, FontVariation? variation)
    {
        if (!completionSource.Task.IsCompletedSuccessfully)
            return 0;

        FT_Vector_ kerning;
        FT_Error error;

        lock (faceLock)
        {
            SetVariation(Face, variation);
            error = FT_Get_Kerning(Face, left, right, FT_KERNING_DEFAULT, &kerning);
        }

        if (error != 0) return 0;

        // osu!framework only supports horizontal kerning in integer values.
        return (int)kerning.x / 64;
    }

    /// <summary>
    /// Rasterize a glyph.
    /// </summary>
    /// <param name="glyphIndex">The index of the glyph.</param>
    /// <param name="variation">
    /// The parameters of the font. If null, the default parameters will be
    /// used. This parameter must be null if the font is static.
    /// </param>
    /// <returns>The rasterized glyph suitable for rendering.</returns>
    /// <exception cref="FreeTypeException">Rasterization of the texture failed.</exception>
    public unsafe TextureUpload? RasterizeGlyph(uint glyphIndex, FontVariation? variation)
    {
        if (!completionSource.Task.IsCompletedSuccessfully)
            return null;

        return RasterizeGlyphInner((nint)Face, glyphIndex, variation);
    }

    /// <summary>
    /// Rasterize a character asynchronously.
    /// </summary>
    /// <param name="glyphIndex">The index of the glyph.</param>
    /// <param name="variation">
    /// The parameters of the font. If null, the default parameters will be
    /// used. This parameter must be null if the font is static.
    /// </param>
    /// <returns>The rasterized glyph suitable for rendering.</returns>
    /// <exception cref="AggregateException">The font failed to load.</exception>
    /// <exception cref="FreeTypeException">Rasterization of the texture failed.</exception>
    public async Task<TextureUpload> RasterizeGlyphAsync(uint glyphIndex, FontVariation? variation, CancellationToken cancellationToken = default)
    {
        var face = await completionSource.Task.ConfigureAwait(false);

        return await Task.Run(() => RasterizeGlyphInner(face, glyphIndex, variation), cancellationToken);
    }

    private unsafe TextureUpload RasterizeGlyphInner(nint ptr, uint glyphIndex, FontVariation? variation)
    {
        Image<Rgba32> image;
        FT_Error error;
        var face = (FT_FaceRec_*)ptr;

        // rasterize
        lock (faceLock)
        {
            SetVariation(Face, variation);

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

    /// <summary>
    /// Get the set of characters available in the font.
    /// </summary>
    public IEnumerable<char> GetAvailableChars()
    {
        if (!completionSource.Task.IsCompletedSuccessfully) yield break;

        uint codePoint;
        uint glyphIndex;

        (codePoint, glyphIndex) = GetFirstChar();

        while (glyphIndex != 0)
        {
            // The rest of osu!framework is unable to handle non-BMP characters properly.
            if (codePoint <= char.MaxValue)
                yield return (char)codePoint;

            (codePoint, glyphIndex) = GetNextChar(codePoint);
        }

        unsafe (uint codePoint, uint glyphIndex) GetFirstChar()
        {
            if (Face is null)
                return (0, 0);

            uint glyphIndex, codePoint;

            lock (faceLock)
                codePoint = (uint)FT_Get_First_Char(Face, &glyphIndex);

            return (codePoint, glyphIndex);
        }

        unsafe (uint codePoint, uint glyphIndex) GetNextChar(uint prevCodePoint)
        {
            if (Face is null)
                return (0, 0);

            uint glyphIndex, codePoint;

            lock (faceLock)
                codePoint = (uint)FT_Get_Next_Char(Face, prevCodePoint, &glyphIndex);

            return (codePoint, glyphIndex);
        }
    }
}
