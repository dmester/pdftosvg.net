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
        private Dictionary<int, double> widthMap;
        private uint firstChar;
        private uint lastChar;
        private double missingWidth;

        private Type1WidthMap() { }

        public new static WidthMap Parse(PdfDictionary font)
        {
            if (font.TryGetArray<double>(Names.Widths, out var widths))
            {
                // PDF Specification 1.7, Table 111, page 263
                var widthMap = new Dictionary<int, double>();
                var firstChar = font.GetValueOrDefault(Names.FirstChar, 0);
                var lastChar = font.GetValueOrDefault(Names.LastChar, int.MaxValue);
                var missingWidth = font.GetValueOrDefault(Names.MissingWidth, 0.0);

                for (var i = 0; i < widths.Length; i++)
                {
                    widthMap[firstChar + i] = widths[i];
                }

                return new Type1WidthMap
                {
                    firstChar = (uint)firstChar,
                    lastChar = (uint)lastChar,
                    missingWidth = missingWidth,
                    widthMap = widthMap,
                };
            }
            else
            {
                var name = font.GetValueOrDefault(Names.BaseFont, PdfName.Null);
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
            if (widthMap.TryGetValue((int)ch.Code, out var width))
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
