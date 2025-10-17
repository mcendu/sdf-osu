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
using osu.Framework.Graphics.Rendering;
using osu.Framework.IO.Stores;
using Sdf.Text;

namespace Sdf.IO.Stores;

public class OutlineFontStore : FontStore
{
    private OutlineFont font;

    public OutlineFontStore(IRenderer renderer, IResourceStore<byte[]> store, string assetName, float scaleAdjust = 100)
        : base(renderer, null, scaleAdjust)
    {
        font = new OutlineFont(store, assetName, 0)
        {
            Resolution = (uint)Math.Round(scaleAdjust)
        };
    }

    protected override void Dispose(bool disposing)
    {
        font.Dispose();
    }

    public void AddInstance(FontVariation? variation)
    {
        AddTextureSource(new OutlineGlyphStore(font, variation));
    }

    public void AddInstance(string namedInstance)
    {
        AddTextureSource(new OutlineGlyphStore(font, namedInstance));
    }
}
