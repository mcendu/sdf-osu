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
using Sdf.Text;
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
/// <remarks>
/// This class uses FreeType for glyph loading and rasterization.
/// </remarks>
public class OutlineGlyphStore : IGlyphStore, IResourceStore<TextureUpload>, IDisposable
{
    private OutlineFont Font => completionSource.Task.GetResultSafely();

    private TaskCompletionSource<OutlineFont> completionSource = new();

    protected readonly string? AssetName;

    /// <summary>
    /// The resolution of the distance fields in pixels per em.
    /// </summary>
    public uint Resolution { get; init; } = 100;

    /// <summary>
    /// The index of the face to use.
    /// </summary>
    public int FaceIndex { private get; init; } = 0;

    public FontVariation? Variation { private get; init; }

    protected readonly ResourceStore<byte[]> Store;

    public string FontName { get; init; }

    public float? Baseline => OutlineFont.BASELINE;

    public OutlineGlyphStore(ResourceStore<byte[]> store, string? assetName = null)
    {
        Store = new ResourceStore<byte[]>(store);

        Store.AddExtension("ttf");
        Store.AddExtension("otf");
        Store.AddExtension("woff");
        Store.AddExtension("ttc");

        AssetName = assetName;

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

    protected virtual void Dispose(bool isDisposing)
    {
        if (completionSource.Task.IsCompletedSuccessfully)
        {
            Font.Dispose();
        }
    }

    public async Task LoadFontAsync()
    {
        var font = new OutlineFont(Store, AssetName ?? throw new ArgumentNullException(), FaceIndex)
        {
            Resolution = Resolution,
        };

        try
        {
            await font.LoadAsync();
        }
        finally
        {
            completionSource.SetResult(font);
        }
    }

    public bool HasGlyph(char c)
    {
        return Font.HasGlyph(c);
    }

    public CharacterGlyph? Get(char c)
    {
        var metrics = Font.GetMetrics(Font.GetGlyphIndex(c), Variation);

        if (metrics is null)
            return null;

        return new CharacterGlyph(c, metrics.XOffset, metrics.YOffset, metrics.XAdvance, metrics.Baseline, this);
    }

    public int GetKerning(char left, char right)
    {
        return Font.GetKerning(Font.GetGlyphIndex(left), Font.GetGlyphIndex(right), Variation);
    }

    Task<CharacterGlyph> IResourceStore<CharacterGlyph>.GetAsync(string name, CancellationToken cancellationToken)
        => Task.Run(() => ((IGlyphStore)this).Get(name[0]), cancellationToken)!;

    CharacterGlyph IResourceStore<CharacterGlyph>.Get(string name) => Get(name[0])!;

    protected virtual TextureUpload? GetCachedGlyph(uint glyphIndex) => null;

    protected virtual TextureUpload? CacheGlyph(uint index, TextureUpload? texture) => texture;

    public TextureUpload Get(string name)
    {
        if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
            return null!;

        char c = name.Last();
        uint glyphIndex = Font.GetGlyphIndex(c);

        return GetCachedGlyph(glyphIndex)
            ?? CacheGlyph(glyphIndex, Font.RasterizeGlyph(glyphIndex, Variation))!;
    }

    public async Task<TextureUpload> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
            return null!;

        char c = name.Last();
        var font = await completionSource.Task.ConfigureAwait(false);
        uint glyphIndex = await font.GetGlyphIndexAsync(c);

        return GetCachedGlyph(glyphIndex)
            ?? CacheGlyph(glyphIndex, await font.RasterizeGlyphAsync(glyphIndex, Variation, cancellationToken))!;
    }

    public Stream GetStream(string name) => throw new NotSupportedException();

    public IEnumerable<string> GetAvailableResources()
    {
        if (!completionSource.Task.IsCompletedSuccessfully)
            return Enumerable.Empty<string>();

        return Font.GetAvailableChars().Select(c => $@"{FontName}/{c}");
    }
}
