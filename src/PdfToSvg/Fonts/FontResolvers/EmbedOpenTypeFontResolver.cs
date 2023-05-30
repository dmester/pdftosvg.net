// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts.FontResolvers
{
    internal class EmbedOpenTypeFontResolver : FontResolver
    {
        public override Font ResolveFont(SourceFont sourceFont, CancellationToken cancellationToken)
        {
            if (sourceFont.CanBeExtracted)
            {
                try
                {
                    var otf = sourceFont.ToOpenType();
                    var otfDataUrl = "data:font/otf;base64," + Convert.ToBase64String(otf);
                    return new WebFont(openTypeUrl: otfDataUrl);
                }
                catch
                {
                }
            }
            else if (sourceFont.CanBeInlined)
            {
                return new InlinedFont(sourceFont);
            }

            return LocalFonts.ResolveFont(sourceFont, cancellationToken);
        }
    }
}
