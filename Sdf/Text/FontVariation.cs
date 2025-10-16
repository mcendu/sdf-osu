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

namespace Sdf.Text;

/// <summary>
/// Represents the configuration of an OpenType variable font.
/// </summary>
public class FontVariation
{
    /// <summary>
    /// The named instance to use.
    /// </summary>
    /// <remarks>
    /// If both <see cref="NamedInstance"/> and <see cref="Axes"/> are set,
    /// only <see cref="Axes"/> is used. 
    /// </remarks>
    public uint NamedInstance { get; init; }

    /// <summary>
    /// The configuration of the variable font.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Numbers are in 16.16 fixed point format.
    /// </para>
    /// <para>
    /// If both <see cref="NamedInstance"/> and <see cref="Axes"/> are set,
    /// only <see cref="Axes"/> is used.
    /// </para>
    /// </remarks>
    public CLong[]? Axes { get; init; }
}
