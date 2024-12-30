// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jbig2.Coding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Model
{
    [DebuggerDisplay("{Symbols.Length} symbols")]
    internal class JbigSymbolDictionary
    {
        public JbigArithmeticContext? RetainedGBContext;
        public JbigArithmeticContext? RetainedGRContext;

        public JbigBitmap[] Symbols = ArrayUtils.Empty<JbigBitmap>();
    }
}
