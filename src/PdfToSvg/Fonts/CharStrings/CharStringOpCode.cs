// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal enum CharStringOpCode
    {
        None = 0,

        HStem = 1,
        VStem = 3,
        VMoveTo = 4,
        RLineTo = 5,
        HLineTo = 6,
        VLineTo = 7,
        RRCurveTo = 8,
        ClosePath = 9, // Type 1
        CallSubr = 10,
        Return = 11,
        Hsbw = 13, // Type 1
        EndChar = 14,
        HStemHm = 18,
        HintMask = 19,
        CntrMask = 20,
        RMoveTo = 21,
        HMoveTo = 22,
        VStemHm = 23,
        RCurveLine = 24,
        RLineCurve = 25,
        VVCurveTo = 26,
        HHCurveTo = 27,
        CallGSubr = 29,
        VHCurveTo = 30,
        HVCurveTo = 31,
        DotSection = (12 << 8) | 0,
        VStem3 = (12 << 8) | 1, // Type 1
        HStem3 = (12 << 8) | 2, // Type 1
        And = (12 << 8) | 3,
        Or = (12 << 8) | 4,
        Not = (12 << 8) | 5,
        Seac = (12 << 8) | 6, // Type 1
        Sbw = (12 << 8) | 7, // Type 1
        Abs = (12 << 8) | 9,
        Add = (12 << 8) | 10,
        Sub = (12 << 8) | 11,
        Div = (12 << 8) | 12,
        Neg = (12 << 8) | 14,
        Eq = (12 << 8) | 15,
        CallOtherSubr = (12 << 8) | 16, // Type 1
        Pop = (12 << 8) | 17, // Type 1
        Drop = (12 << 8) | 18,
        Put = (12 << 8) | 20,
        Get = (12 << 8) | 21,
        IfElse = (12 << 8) | 22,
        Random = (12 << 8) | 23,
        Mul = (12 << 8) | 24,
        Sqrt = (12 << 8) | 26,
        Dup = (12 << 8) | 27,
        Exch = (12 << 8) | 28,
        Index = (12 << 8) | 29,
        Roll = (12 << 8) | 30,
        SetCurrentPoint = (12 << 8) | 33, // Type 1
        HFlex = (12 << 8) | 34,
        Flex = (12 << 8) | 35,
        HFlex1 = (12 << 8) | 36,
        Flex1 = (12 << 8) | 37,
    }
}
