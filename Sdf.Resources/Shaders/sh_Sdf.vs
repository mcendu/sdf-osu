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
precision highp float;
precision highp int;

layout(location = 0) in highp vec2 m_Position;
layout(location = 1) in highp vec2 m_TexCoord;
layout(location = 2) in lowp vec4 m_Colour;
layout(location = 3) in lowp float m_Threshold;

layout(location = 0) out highp vec2 v_MaskingPosition;
layout(location = 1) out highp vec2 v_TexCoord;
layout(location = 2) out lowp vec4 v_Colour;
layout(location = 3) out lowp float v_Threshold;

void main(void)
{
    // Transform from screen space to masking space.
    highp vec3 maskingPos = g_ToMaskingSpace * vec3(m_Position, 1.0);
    v_MaskingPosition = maskingPos.xy / maskingPos.z;

    v_Colour = m_Colour;
    v_TexCoord = m_TexCoord;
    v_Threshold = m_Threshold;

    gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);
}
