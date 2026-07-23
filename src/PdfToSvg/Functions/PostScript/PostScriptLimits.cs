// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Functions.PostScript
{
    internal static class PostScriptLimits
    {
        public const int MaxExecutionDepth = 100;
        public const int MaxInterpretedInstructions = 5000;
        public const int MaxCompiledInstructions = 500;

        // ISO 32000-2:2020 section 7.10.5.3 states the stack should provide room for at least 100 entries
        public const int StackSize = 100;
    }
}
