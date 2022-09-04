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
    internal class CustomEncoding : SingleByteEncoding
    {
        private CustomEncoding(string?[] toUnicode, string?[] toGlyphName) : base(toUnicode, toGlyphName)
        {
        }

        public static CustomEncoding Create(PdfDictionary encodingDict)
        {
            // PDF spec 1.7, Table 114, page 271

            SingleByteEncoding? baseEncoding = null;

            if (encodingDict.TryGetName(Names.BaseEncoding, out var baseEncodingName))
            {
                baseEncoding = EncodingFactory.Create(baseEncodingName);
            }

            if (baseEncoding == null)
            {
                baseEncoding = new StandardEncoding();
            }

            var toUnicode = new string?[256];
            var toGlyphName = new string?[256];

            for (var i = 0; i < toUnicode.Length; i++)
            {
                toUnicode[i] = baseEncoding.GetUnicode((byte)i);
                toGlyphName[i] = baseEncoding.GetGlyphName((byte)i);
            }

            if (encodingDict.TryGetArray(Names.Differences, out var differences))
            {
                var nextCharCode = 0;

                for (var i = 0; i < differences.Length; i++)
                {
                    var item = differences[i];

                    if (item is PdfName glyphName)
                    {
                        toGlyphName[nextCharCode] = glyphName.Value;

                        if (AdobeGlyphList.TryGetUnicode(glyphName, out var unicode))
                        {
                            toUnicode[nextCharCode] = unicode;
                        }
                        else
                        {
                            toUnicode[nextCharCode] = "\uFFFD";
                        }

                        nextCharCode++;
                    }
                    else if (MathUtils.ToInt(item, out var charCode))
                    {
                        nextCharCode = charCode;
                    }
                }
            }

            return new CustomEncoding(toUnicode, toGlyphName);
        }
    }
}
