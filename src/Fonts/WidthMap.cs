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
    internal abstract class WidthMap
    {
        public abstract double GetWidth(CharacterCode ch);


        public static WidthMap Parse(PdfDictionary font)
        {
            if (font.TryGetName(Names.Subtype, out var subtype))
            {
                // PDF spec 1.7, Table 110, page 261

                if (subtype == Names.Type0)
                {
                    return Type0WidthMap.Parse(font);
                }

                if (subtype == Names.CIDFontType0 ||
                    subtype == Names.CIDFontType2)
                {
                    return CIDFontWidthMap.Parse(font);
                }

                if (subtype == Names.Type1 ||
                    subtype == Names.MMType1 ||
                    subtype == Names.TrueType)
                {
                    return Type1WidthMap.Parse(font);
                }

                if (subtype == Names.Type3)
                {
                    return Type3WidthMap.Parse(font);
                }
            }

            return new EmptyWidthMap();
        }
    }
}
