// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    /// <summary>
    /// Part of COD and COC
    /// </summary>
    internal struct JpxCodeBlockStyle
    {
        // ITU-T T.800 (06/2019) Table A.19
        public bool SelectiveArithmeticCodingBypass;
        public bool ResetContextProbabilitiesOnCodingPassBoundaries;
        public bool TerminationOnEachCodingPass;
        public bool VerticallyCausalContext;
        public bool PredictableTermination;
        public bool SegmentationSymbolsAreUsed;
    }
}
