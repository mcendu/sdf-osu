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
using System.Security.Cryptography;
using System.Text;

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
    public string? NamedInstance { get; init; }

    /// <summary>
    /// The configuration of the variable font.
    /// </summary>
    /// <remarks>
    /// If both <see cref="NamedInstance"/> and <see cref="Axes"/> are set,
    /// only <see cref="Axes"/> is used.
    /// </remarks>
    public IEnumerable<KeyValuePair<string, double>>? Axes { get; init; }

    /// <summary>
    /// Generate a suitable font name suffix for this configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If both <see cref="NamedInstance"/> and <see cref="Axes"/> are set,
    /// only <see cref="Axes"/> is used. 
    /// </para>
    /// <para>
    /// The name generation is based on Adobe TechNote #5902, 'Generating
    /// PostScript Names for Fonts Using OpenType Font Variations'. Notable
    /// differences are:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// When the instance is an arbitrary instance, an axis value descriptor
    /// starts with <c>-</c> instead of <c>_</c> to stay compatible with
    /// osu!framework semantics.
    /// </description>
    /// </item>
    /// </list>
    /// <seealso href="https://download.macromedia.com/pub/developer/opentype/tech-notes/5902.AdobePSNameGeneration.html"/>
    /// </remarks>
    public string GenerateInstanceName(string baseName)
    {
        if (Axes is not null)
        {
            StringBuilder instanceName = new(baseName);
            List<HashedAxisParameter> hashedAxes = new();

            foreach (var (axis, value) in Axes)
            {
                // add parameter for hashing
                HashedAxisParameter parameter = new();

                unsafe
                {
                    NativeMemory.Fill(parameter.axis, 4, 0x20);
                    Encoding.UTF8.GetBytes(axis, new Span<byte>(parameter.axis, 4));
                }

                parameter.value = (int)Math.Round(value * 65536.0);
                hashedAxes.Add(parameter);

                // compute ASCII representation of parameter
                double effectiveValue = parameter.value / 65536.0;

                instanceName.Append($@"-{effectiveValue:0.#####}{axis}");

                if (instanceName.Length > 128)
                {
                    // The last resort string is constructed from a SHA-256 hash
                    // if the string form of the parameters gets too long.
                    ReadOnlySpan<byte> hashedData = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(hashedAxes));
                    return $@"{baseName}-{Convert.ToHexStringLower(SHA256.HashData(hashedData))}";
                }
            }

            return instanceName.ToString();
        
        }
        else if (NamedInstance is not null)
        {
            // strip occurences of 'Regular' here
            // TODO: add special handling of the 'Regular' weight at the osu!framework level.
            return NamedInstance.Replace("Regular", string.Empty).TrimEnd('-');
        }
        else
        {
            return baseName;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct HashedAxisParameter
    {
        public fixed byte axis[4];
        public int value;
    }
}