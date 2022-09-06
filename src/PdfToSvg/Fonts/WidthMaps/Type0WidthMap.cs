// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.WidthMaps
{
    internal static class Type0WidthMap
    {
        public static WidthMap Parse(PdfDictionary font)
        {
            if (font.TryGetArray<PdfDictionary>(Names.DescendantFonts, out var descendantFonts))
            {
                // PDF should only contain a single descendant font according to ISO 32000-2 section 9.7.1
                foreach (var descendantFont in descendantFonts)
                {
                    return CidFontWidthMap.Parse(descendantFont);
                }
            }

            return new EmptyWidthMap();
        }
    }
}
