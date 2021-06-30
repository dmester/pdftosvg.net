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

namespace PdfToSvg.Fonts
{
    internal static class Type0WidthMap
    {
        public static WidthMap Parse(PdfDictionary font)
        {
            if (font.TryGetArray<PdfDictionary>(Names.DescendantFonts, out var descendantFonts))
            {
                // TODO This is probably not correct
                foreach (var descendantFont in descendantFonts)
                {
                    return CIDFontWidthMap.Parse(descendantFont);
                }
            }

            return new EmptyWidthMap();
        }
    }
}
