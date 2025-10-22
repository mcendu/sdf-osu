/*
Copyright (c) 2025 McEndu.
Copyright (c) 2025 ppy Pty Ltd <contact@ppy.sh>.

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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace Sdf.Graphics
{
    public partial class SdfSpriteText
    {
        private class SpriteTextDrawNode : TexturedShaderDrawNode
        {
            protected new SdfSpriteText Source => (SdfSpriteText)base.Source;

            private bool shadow;
            private ColourInfo shadowColour;
            private Vector2 shadowOffset;

            private ColourInfo outlineColour;
            private float outlineThreshold;

            private IVertexBatch<SdfVertex>? vertexBatch;

            private List<ScreenSpaceCharacterPart>? parts;

            public SpriteTextDrawNode(SdfSpriteText source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                updateScreenSpaceCharacters();
                shadow = Source.Shadow;

                if (shadow)
                {
                    shadowColour = Source.ShadowColour;
                    shadowOffset = Source.premultipliedShadowOffset;
                }

                outlineColour = Source.OutlineColour;
                outlineThreshold = Source.outlineThreshold;
            }

            protected override void BindUniformResources(IShader shader, IRenderer renderer)
            {
                base.BindUniformResources(shader, renderer);

                vertexBatch ??= renderer.CreateQuadBatch<SdfVertex>(1, 3);
            }

            private Action<TexturedVertex2D> vertexAction(float threshold) => (vertex) =>
            {
                vertexBatch!.Add(new SdfVertex
                {
                    position = vertex.Position,
                    texCoords = vertex.TexturePosition,
                    colour = vertex.Colour,
                    threshold = threshold,
                });
            };

            protected override void Draw(IRenderer renderer)
            {
                Debug.Assert(parts != null);

                base.Draw(renderer);

                var avgColour = (Color4)DrawColourInfo.Colour.AverageColour;
                float shadowAlpha = MathF.Pow(Math.Max(Math.Max(avgColour.R, avgColour.G), avgColour.B), 2);

                //adjust shadow alpha based on highest component intensity to avoid muddy display of darker text.
                //squared result for quadratic fall-off seems to give the best result.
                var finalShadowColour = DrawColourInfo.Colour;
                finalShadowColour.ApplyChild(shadowColour.MultiplyAlpha(shadowAlpha));

                BindTextureShader(renderer);

                for (int i = 0; i < parts.Count; i++)
                {
                    if (shadow)
                    {
                        var shadowQuad = parts[i].DrawQuad;

                        renderer.DrawQuad(parts[i].Texture,
                            new Quad(
                                shadowQuad.TopLeft + shadowOffset,
                                shadowQuad.TopRight + shadowOffset,
                                shadowQuad.BottomLeft + shadowOffset,
                                shadowQuad.BottomRight + shadowOffset),
                            finalShadowColour,
                            vertexAction: vertexAction(outlineThreshold),
                            inflationPercentage: parts[i].InflationPercentage);
                    }
                }

                for (int i = 0; i < parts.Count; i++)
                {
                    renderer.DrawQuad(parts[i].Texture, parts[i].DrawQuad, outlineColour,
                        vertexAction: vertexAction(outlineThreshold),
                        inflationPercentage: parts[i].InflationPercentage);
                }

                for (int i = 0; i < parts.Count; i++)
                {
                    renderer.DrawQuad(parts[i].Texture, parts[i].DrawQuad, DrawColourInfo.Colour,
                        vertexAction: vertexAction(0.5f),
                        inflationPercentage: parts[i].InflationPercentage);
                }

                UnbindTextureShader(renderer);
            }

            /// <summary>
            /// The characters in screen space. These are ready to be drawn.
            /// </summary>
            private void updateScreenSpaceCharacters()
            {
                int partCount = Source.characters.Count;

                if (parts == null)
                    parts = new List<ScreenSpaceCharacterPart>(partCount);
                else
                {
                    parts.Clear();
                    parts.EnsureCapacity(partCount);
                }

                Vector2 inflationAmount = DrawInfo.MatrixInverse.ExtractScale().Xy;

                foreach (var character in Source.characters)
                {
                    parts.Add(new ScreenSpaceCharacterPart
                    {
                        DrawQuad = Source.ToScreenSpace(character.DrawRectangle.Inflate(inflationAmount)),
                        InflationPercentage = new Vector2(
                            character.DrawRectangle.Size.X == 0 ? 0 : inflationAmount.X / character.DrawRectangle.Size.X,
                            character.DrawRectangle.Size.Y == 0 ? 0 : inflationAmount.Y / character.DrawRectangle.Size.Y),
                        Texture = character.Texture
                    });
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private record struct SdfVertex : IVertex
        {
            [VertexMember(2, VertexAttribPointerType.Float)]
            public Vector2 position;
            [VertexMember(2, VertexAttribPointerType.Float)]
            public Vector2 texCoords;
            [VertexMember(4, VertexAttribPointerType.Float)]
            public Colour4 colour;
            [VertexMember(1, VertexAttribPointerType.Float)]
            public float threshold;
        }

        /// <summary>
        /// A character of a <see cref="SpriteText"/> provided with screen space draw coordinates.
        /// </summary>
        private struct ScreenSpaceCharacterPart
        {
            /// <summary>
            /// The screen-space quad for the character to be drawn in.
            /// </summary>
            public Quad DrawQuad;

            /// <summary>
            /// Extra padding for the character's texture.
            /// </summary>
            public Vector2 InflationPercentage;

            /// <summary>
            /// The texture to draw the character with.
            /// </summary>
            public Texture Texture;
        }
    }
}
