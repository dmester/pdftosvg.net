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
        private Dictionary<int, double> widthMap;

        private Type3WidthMap() { }

        public new static Type3WidthMap Parse(PdfDictionary font)
        {
            // PDF Specification 1.7, Table 112, page 266
            var widthMap = new Dictionary<int, double>();
            var firstChar = font.GetValueOrDefault(Names.FirstChar, 0);
            
            if (font.TryGetArray<double>(Names.Widths, out var widths))
            {
                for (var i = 0; i < widths.Length; i++)
                {
                    widthMap[firstChar + i] = widths[i];
                } 
            }

            return new Type3WidthMap
            {
                widthMap = widthMap,
            };
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
