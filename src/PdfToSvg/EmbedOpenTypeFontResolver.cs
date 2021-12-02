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
    internal class EmbedOpenTypeFontResolver : FontResolver
    {
        public override Font ResolveFont(SourceFont sourceFont, CancellationToken cancellationToken)
        {
            try
            {
                var otf = sourceFont.ToOpenType();
                var otfDataUri = "data:font/otf;base64," + Convert.ToBase64String(otf);
                return new WebFont(openTypeUrl: otfDataUri);
            }
            catch
            {
                return LocalFonts.ResolveFont(sourceFont, cancellationToken);
            }
        }
    }
}
