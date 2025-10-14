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
precision mediump float;
precision mediump int;

layout(location = 1) in lowp vec4 v_Colour;
layout(location = 2) in mediump vec2 v_TexCoord;

layout(location = 0) out lowp vec4 o_Colour;

layout(set = 0, binding = 0) uniform lowp texture2D m_DistanceFields;
layout(set = 0, binding = 1) uniform lowp sampler m_Sampler;

const float spread = 8.0;
const float filterSizeScale = 1.0 / (spread * 2.0);

void main(void)
{
    // Based on <https://bohdon.com/docs/smooth-sdf-shape-edges/>.
    ivec2 textureResolution = textureSize(sampler2D(m_DistanceFields, m_Sampler), 0);
    lowp float dist = texture(sampler2D(m_DistanceFields, m_Sampler), v_TexCoord).r;

    float filterX = dFdx(v_TexCoord).s * float(textureResolution.x) * filterSizeScale;

    // add 0.5 * filterX to make things legilible at smaller sizes
    float coverage = (dist - 0.5 + 0.5 * filterX) / abs(filterX);

    o_Colour = vec4(v_Colour.rgb, clamp(coverage, 0.0, 1.0));
}
