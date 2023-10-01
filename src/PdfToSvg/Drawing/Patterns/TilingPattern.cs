// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace PdfToSvg.Drawing.Patterns
{
    internal class TilingPattern : Pattern
    {
        public TilingPattern(PdfDictionary definition, CancellationToken cancellationToken) : base(definition, cancellationToken)
        {
        }
    }
}
