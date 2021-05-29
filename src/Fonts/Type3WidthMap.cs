using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts
{
    internal class Type3WidthMap : WidthMap
    {
        private readonly Dictionary<int, double> widthMap = new Dictionary<int, double>();

        private Type3WidthMap(PdfDictionary font)
        {
            // PDF Specification 1.7, Table 112, page 266
            var firstChar = font.GetValueOrDefault(Names.FirstChar, 0);

            if (font.TryGetArray<double>(Names.Widths, out var widths))
            {
                for (var i = 0; i < widths.Length; i++)
                {
                    widthMap[firstChar + i] = widths[i];
                }
            }
        }

        public new static Type3WidthMap Parse(PdfDictionary font)
        {
            return new Type3WidthMap(font);
        }

        public override double GetWidth(CharacterCode ch)
        {
            if (widthMap.TryGetValue((int)ch.Code, out var width))
            {
                return width;
            }

            return 0;
        }
    }
}
