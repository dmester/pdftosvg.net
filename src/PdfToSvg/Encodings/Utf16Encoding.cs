// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Encodings
{
    internal class Utf16Encoding : ITextDecoder
    {
        public CharacterCode GetCharacter(PdfString value, int index)
        {
            if (index + 1 < value.Length)
            {
                var unicodeValue = (uint)((value[index] << 8) | value[index + 1]);
                if (unicodeValue > 30)
                {
                    return new CharacterCode(unicodeValue, 2, ((char)unicodeValue).ToString());
                }
            }

            return default;
        }
    }
}
