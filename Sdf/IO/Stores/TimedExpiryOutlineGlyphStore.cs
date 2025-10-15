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

using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;

namespace Sdf.IO.Stores;

/// <summary>
/// A glyph store that caches metrics and rendered outlines in memory temporarily, to allow more efficient retrieval.
/// </summary>
public class TimedExpiryOutlineGlyphStore : OutlineGlyphStore
{
    private readonly TimedExpiryCache<uint, GlyphMetrics> metricsCache = new();
    private readonly TimedExpiryCache<uint, TextureUpload> textureCache = new();

    public TimedExpiryOutlineGlyphStore(ResourceStore<byte[]> store, string? assetName = null) : base(store, assetName)
    {
    }

    protected override GlyphMetrics LoadMetrics(uint glyphIndex)
    {
        if (!metricsCache.TryGetValue(glyphIndex, out var metrics))
        {
            metrics = base.LoadMetrics(glyphIndex);
            metricsCache.Add(glyphIndex, metrics);
        }

        return metrics;
    }

    protected override TextureUpload GetGlyphTexture(nint ptr, uint glyphIndex)
    {
        if (!textureCache.TryGetValue(glyphIndex, out var texture))
        {
            texture = base.GetGlyphTexture(ptr, glyphIndex);
            textureCache.Add(glyphIndex, texture);
        }

        return texture;
    }
}
