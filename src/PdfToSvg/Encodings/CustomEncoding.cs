// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal class CustomEncoding : ITextDecoder
    {
        private readonly Dictionary<int, string> lookup = new Dictionary<int, string>();
        private readonly ITextDecoder baseEncoding;

        private CustomEncoding(PdfDictionary encodingDict)
        {
            // PDF spec 1.7, Table 114, page 271
            if (encodingDict.TryGetName(Names.BaseEncoding, out var baseEncodingName))
            {
                baseEncoding = EncodingFactory.Create(baseEncodingName);
            }

            if (baseEncoding == null)
            {
                baseEncoding = new StandardEncoding();
            }

            if (encodingDict.TryGetArray(Names.Differences, out var differences))
            {
                var nextCharCode = 0;

                for (var i = 0; i < differences.Length; i++)
                {
                    var item = differences[i];

                    if (item is PdfName glyphName)
                    {
                        if (AdobeGlyphList.TryMap(glyphName, out var ch))
                        {
                            lookup[nextCharCode] = ch.ToString();
                        }

                        nextCharCode++;
                    }
                    else if (MathUtils.ToInt(item, out var charCode))
                    {
                        nextCharCode = charCode;
                    }
                }
            }
        }

        public static CustomEncoding Create(PdfDictionary encodingDict)
        {
            return new CustomEncoding(encodingDict);
        }

        public CharacterCode GetCharacter(PdfString value, int index)
        {
            if (lookup.TryGetValue(value[index], out var ch))
            {
                return new CharacterCode(value[index], 1, ch);
            }

            return baseEncoding.GetCharacter(value, index);
        }
    }
}
