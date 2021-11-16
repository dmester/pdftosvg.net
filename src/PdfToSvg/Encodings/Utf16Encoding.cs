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

        public static string? EncodeCodePoint(uint unicodeValue)
        {
            // See https://en.wikipedia.org/wiki/UTF-16#Examples
            var utf16 = (string?)null;

            if (unicodeValue <= 0xFFFF)
            {
                utf16 = new string((char)unicodeValue, 1);
            }

            else if (unicodeValue <= 0x10FFFF)
            {
                unicodeValue -= 0x10000;

                var highSurrogate = unchecked((char)(0xD800u + (unicodeValue >> 10)));
                var lowSurrogate = unchecked((char)(0xDC00u + (unicodeValue & 0x3FFu)));

                utf16 = new string(new[] { highSurrogate, lowSurrogate });
            }

            return utf16;
        }

        public static uint DecodeCodePoint(string s, int offset, out int length)
        {
            if (s == null) throw new ArgumentOutOfRangeException(nameof(s));
            if (offset < 0 || offset >= s.Length) throw new ArgumentOutOfRangeException(nameof(offset));

            var codePoint = (uint)s[offset];
            if (codePoint >= 0xD800 && codePoint <= 0xDFFF && offset + 1 < s.Length)
            {
                var highSurrogate = codePoint;
                var lowSurrogate = s[offset + 1];

                codePoint =
                    ((highSurrogate - 0xD800u) << 10) +
                    (lowSurrogate - 0xDC00u) +
                    0x10000u;

                length = 2;
            }
            else
            {
                length = 1;
            }

            return codePoint;
        }
    }
}
