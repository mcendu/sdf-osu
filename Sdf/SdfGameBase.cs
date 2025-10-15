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
            Resources.AddStore(new DllResourceStore(typeof(SdfResources).Assembly));

            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/Exo2-VariableFont_wght")
            {
                NamedInstance = 4,
            });
            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/NotoSansCJK-VF")
            {
                FaceIndex = 2,
                NamedInstance = 4,
            });

            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/Exo2-VariableFont_wght")
            {
                NamedInstance = 1,
            });
            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/Exo2-VariableFont_wght")
            {
                NamedInstance = 3,
            });
            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/Exo2-VariableFont_wght")
            {
                NamedInstance = 5,
            });
            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/Exo2-VariableFont_wght")
            {
                NamedInstance = 6,
            });
            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/Exo2-VariableFont_wght")
            {
                NamedInstance = 7,
            });
            Fonts.AddTextureSource(new OutlineGlyphStore(Resources, @"Fonts/Exo2-VariableFont_wght")
            {
                NamedInstance = 9,
            });

            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/NotoSansCJK-VF")
            {
                FaceIndex = 2,
                NamedInstance = 1,
            });
            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/NotoSansCJK-VF")
            {
                FaceIndex = 2,
                NamedInstance = 2,
            });
            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/NotoSansCJK-VF")
            {
                FaceIndex = 2,
                NamedInstance = 3,
            });
            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/NotoSansCJK-VF")
            {
                FaceIndex = 2,
                NamedInstance = 5,
            });
            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/NotoSansCJK-VF")
            {
                FaceIndex = 2,
                NamedInstance = 6,
            });
            Fonts.AddTextureSource(new TimedExpiryOutlineGlyphStore(Resources, @"Fonts/NotoSansCJK-VF")
            {
                FaceIndex = 2,
                NamedInstance = 7,
            });
        }
    }
}
