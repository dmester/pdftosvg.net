// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Fonts.WidthMaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts
{
    internal sealed class Type3Font : BaseFont
    {
        public Type3Font(PdfDictionary fontDict, CancellationToken cancellationToken) : base(fontDict, cancellationToken)
        {
            widthMap = new Type3WidthMap(fontDict);
        }
    }
}
