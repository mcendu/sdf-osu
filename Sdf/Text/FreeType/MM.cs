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
using FreeTypeSharp;

namespace Sdf.Text.FreeType;

/// <summary>
/// Bindings for FreeType support for OpenType font variations, TrueType GX,
/// and Adobe MM fonts.
/// </summary>
internal static unsafe partial class MM
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FT_Var_Axis_
    {
        byte* name;
        CLong minimum;
        CLong def;
        CLong maximum;
        CULong tag;
        uint strid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FT_Var_Named_Style_
    {
        CLong* coords;
        uint strid;
        uint psid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FT_MM_Var_
    {
        uint num_axis;
        uint num_designs;
        uint num_namedstyles;
        FT_Var_Axis_* axis;
        FT_Var_Named_Style_* namedstyle;

    }

    [LibraryImport("freetype")]
    public static partial FT_Error FT_Get_MM_Var(FT_FaceRec_* face, FT_MM_Var_** amaster);
    [LibraryImport("freetype")]
    public static partial FT_Error FT_Done_MM_Var(FT_FaceRec_* face, FT_MM_Var_* amaster);
    [LibraryImport("freetype")]
    public static partial FT_Error FT_Set_Var_Design_Coordinates(FT_FaceRec_* face, uint num_coords, CLong* coords);
    [LibraryImport("freetype")]
    public static partial FT_Error FT_Set_Named_Instance(FT_FaceRec_* face, uint instance_index);
    [LibraryImport("freetype")]
    public static partial FT_Error FT_Get_Default_Named_Instance(FT_FaceRec_* face, uint* instance_index);
}
