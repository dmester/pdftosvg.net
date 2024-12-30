// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Model
{
    internal enum JbigSegmentType
    {
        // ITU-T_T_88__08_2018
        // Section 7.3

        SymbolDictionary = 0,

        IntermediateTextRegion = 4,
        ImmediateTextRegion = 6,
        ImmediateLosslessTextRegion = 7,

        PatternDictionary = 16,

        IntermediateHalftoneRegion = 20,
        ImmediateHalftoneRegion = 22,
        ImmediateLosslessHalftoneRegion = 23,

        IntermediateGenericRegion = 36,
        ImmediateGenericRegion = 38,
        ImmediateLosslessGenericRegion = 39,

        IntermediateGenericRefinementRegion = 40,
        ImmediateGenericRefinementRegion = 42,
        ImmediateLosslessGenericRefinementRegion = 43,

        PageInformation = 48,

        EndOfPage = 49,
        EndOfStripe = 50,
        EndOfFile = 51,

        Profiles = 52,
        Tables = 53,
        ColourPalette = 54,
        Extension = 62,
    }
}
