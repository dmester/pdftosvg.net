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
    internal class Type1WidthMap : WidthMap
    {
        private const double WidthMultiplier = 0.001;
        private readonly Dictionary<uint, double> widthMap = new Dictionary<uint, double>();
        private readonly uint firstChar;
        private readonly uint lastChar;
        private readonly double missingWidth;

        private Type1WidthMap(PdfDictionary font, double[] widths)
        {
            // PDF Specification 1.7, Table 111, page 263
            firstChar = (uint)font.GetValueOrDefault(Names.FirstChar, 0);
            lastChar = (uint)font.GetValueOrDefault(Names.LastChar, int.MaxValue);
            missingWidth = font.GetValueOrDefault(Names.MissingWidth, 0.0) * WidthMultiplier;

            for (var i = 0u; i < widths.Length; i++)
            {
                widthMap[firstChar + i] = widths[i] * WidthMultiplier;
            }
        }

        public new static WidthMap Parse(PdfDictionary font)
        {
            if (font.TryGetArray<double>(Names.Widths, out var widths))
            {
                return new Type1WidthMap(font, widths);
            }

            if (font.TryGetName(Names.BaseFont, out var name))
            {
                var standardWidth = StandardFontWidthMaps.GetWidths(name);
                if (standardWidth != null)
                {
                    return standardWidth;
                }
            }

            return new EmptyWidthMap();
        }

        public override double GetWidth(CharacterCode ch)
        {
            if (widthMap.TryGetValue(ch.Code, out var width))
            {
                return width;
            }

            if (ch.Code >= firstChar && ch.Code <= lastChar)
            {
                return missingWidth;
            }

            return 0;
        }
    }
}
