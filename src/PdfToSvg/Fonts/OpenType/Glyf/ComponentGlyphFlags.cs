// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;

namespace PdfToSvg.Fonts.OpenType.Glyf
{
    [Flags]
    internal enum ComponentGlyphFlags : ushort
    {
        Arg1And2AreWords = 0x0001,
        ArgsAreXYValues = 0x0002,
        RoundXYToGrid = 0x0004,
        WeHaveAScale = 0x0008,
        MoreComponents = 0x0020,
        WeHaveAnXAndYScale = 0x0040,
        WeHaveATwoByTwo = 0x0080,
        WeHaveInstructions = 0x0100,
        UseMyMetrics = 0x0200,
        OverlapCompounds = 0x0400,
        ScaledComponentOffset = 0x0800,
        UnscaledComponentOffset = 0x1000,

        All = Arg1And2AreWords
            | ArgsAreXYValues
            | RoundXYToGrid
            | WeHaveAScale
            | MoreComponents
            | WeHaveAnXAndYScale
            | WeHaveATwoByTwo
            | WeHaveInstructions
            | UseMyMetrics
            | OverlapCompounds
            | ScaledComponentOffset
            | UnscaledComponentOffset,
    }
}
