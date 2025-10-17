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
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.IO.Stores;
using osuTK;
using Sdf.IO.Stores;
using Sdf.Resources;
using Sdf.Text;
using System.Text;

namespace Sdf
{
    public partial class SdfGameBase : osu.Framework.Game
    {
        protected override Container<Drawable> Content { get; }

        protected SdfGameBase()
        {
            // Ensure game and tests scale with window size and screen DPI.
            base.Content.Add(Content = new DrawSizePreservingFillContainer
            {
                // You may want to change TargetDrawSize to your "default" resolution, which will decide how things scale and position when using absolute coordinates.
                TargetDrawSize = new Vector2(1366, 768)
            });
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Resources.AddStore(new DllResourceStore(typeof(SdfResources).Assembly));

            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2", "Exo2-Regular"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2Italic", "Exo2-Italic"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/NotoSansCJKsc", "NotoSansCJKsc-Regular"));

            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2", "Exo2-Thin"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2Italic", "Exo2-ThinItalic"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2", "Exo2-Light"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2Italic", "Exo2-LightItalic"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2", "Exo2-Medium"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2Italic", "Exo2-MediumItalic"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2", "Exo2-Bold"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2Italic", "Exo2-BoldItalic"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2", "Exo2-Black"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2Italic", "Exo2-BlackItalic"));

            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/NotoSansCJKsc", "NotoSansCJKsc-Thin"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/NotoSansCJKsc", "NotoSansCJKsc-Light"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/NotoSansCJKsc", "NotoSansCJKsc-Medium"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/NotoSansCJKsc", "NotoSansCJKsc-Bold"));
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/NotoSansCJKsc", "NotoSansCJKsc-Black"));
        }
    }
}
