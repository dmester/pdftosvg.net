// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg
{
    internal class EmbedWoffFontResolver : FontResolver
    {
        public override Font ResolveFont(SourceFont sourceFont, CancellationToken cancellationToken)
        {
            try
            {
                var woff = sourceFont.ToWoff();
                var woffDataUri = "data:font/woff;base64," + Convert.ToBase64String(woff);
                return new WebFont(woffUrl: woffDataUri);
            }
            catch
            {
                return LocalFonts.ResolveFont(sourceFont, cancellationToken);
            }
        }
    }
}
