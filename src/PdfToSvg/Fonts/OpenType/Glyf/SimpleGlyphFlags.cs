// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;

namespace PdfToSvg.Fonts.OpenType.Glyf
{
    [Flags]
    internal enum SimpleGlyphFlags : byte
    {
        OnCurvePoint = 0x01,
        XShortVector = 0x02,
        YShortVector = 0x04,
        RepeatFlag = 0x08,
        XIsSameOrPositiveXShortVector = 0x10,
        YIsSameOrPositiveYShortVector = 0x20,
        OverlapSimple = 0x40,

        All = OnCurvePoint
            | XShortVector
            | YShortVector
            | RepeatFlag
            | XIsSameOrPositiveXShortVector
            | YIsSameOrPositiveYShortVector
            | OverlapSimple,
    }
}
