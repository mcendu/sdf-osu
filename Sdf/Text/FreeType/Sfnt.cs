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

internal static unsafe partial class Sfnt
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FT_SfntName_
    {
        public ushort platform_id;
        public ushort encoding_id;
        public ushort language_id;
        public ushort name_id;

        public byte* _string;
        public uint string_len;
    }

    [LibraryImport(FT.LibName)]
    public static partial uint FT_Get_Sfnt_Name_Count(FT_FaceRec_* face);
    [LibraryImport(FT.LibName)]
    public static partial FT_Error FT_Get_Sfnt_Name(FT_FaceRec_* face, uint idx, FT_SfntName_* aname);
}

internal enum TT_PLATFORM : ushort
{
    APPLE_UNICODE = 0,
    MACINTOSH = 1,
    ISO = 2,
    MICROSOFT = 3,
    CUSTOM = 4,
}

internal enum TT_MS_ID : ushort
{
    SYMBOL_CS = 0,
    UNICODE_CS = 1,
    SJIS = 2,
    PRC = 3,
    BIG_5 = 4,
    WANSUNG = 5,
    JOHAB = 6,
    UCS_4 = 10,
}
