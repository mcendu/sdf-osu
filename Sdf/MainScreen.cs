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
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osuTK;
using Sdf.Graphics;

namespace Sdf
{
    public partial class MainScreen : Screen
    {
        private const string TEST_STRING = "说的道理 ABCD 1234";

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = Colour4.Violet,
                    RelativeSizeAxes = Axes.Both,
                },
                new FillFlowContainer<SpriteText>
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Padding = new()
                    {
                        Vertical = 10,
                    },
                    Spacing = new Vector2(0, 10),
                    Children = new []
                    {
                        new SdfSpriteText
                        {
                            Text = TEST_STRING,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 40, weight: "Thin"),
                        },
                        new SdfSpriteText
                        {
                            Text = TEST_STRING,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 40, weight: "Light"),
                        },
                        new SdfSpriteText
                        {
                            Text = TEST_STRING,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 40, weight: "Regular"),
                        },
                        new SdfSpriteText
                        {
                            Text = TEST_STRING,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 40, weight: "Medium"),
                        },
                        new SdfSpriteText
                        {
                            Text = TEST_STRING,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 40, weight: "Bold"),
                        },
                        new SdfSpriteText
                        {
                            Text = TEST_STRING,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 40, weight: "Black"),
                        },
                        new SdfSpriteText
                        {
                            Text = TEST_STRING,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Colour = Colour4.Black,
                            Font = FontUsage.Default.With(size: 40, weight: "Regular"),
                        },
                        new SdfSpriteText
                        {
                            Text = TEST_STRING,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Colour = Colour4.White.Opacity(64),
                            Font = FontUsage.Default.With(size: 40, weight: "Regular"),
                        },
                        new SdfSpriteText
                        {
                            Text = TEST_STRING,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 16, weight: "Regular"),
                        },
                        new SdfSpriteText
                        {
                            Text = TEST_STRING,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 16, weight: "Bold"),
                        },
                    }
                },
            };
        }
    }
}
