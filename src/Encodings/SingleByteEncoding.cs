// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal class SingleByteEncoding : Encoding, ITextDecoder
    {
        private readonly string toUnicode;
        private Dictionary<char, byte>? fromUnicode;

        public SingleByteEncoding(string toUnicode)
        {
            if (toUnicode == null) throw new ArgumentNullException(nameof(toUnicode));
            if (toUnicode.Length != 256) throw new ArgumentException("Expected a 256 characters long Unicode mapping string.", nameof(toUnicode));

            this.toUnicode = toUnicode;
        }

        public override int GetByteCount(char[] chars, int index, int count) => count;

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null) throw new ArgumentNullException(nameof(chars));
            if (bytes == null) throw new ArgumentNullException(nameof(chars));
            if (charIndex < 0 || charIndex > chars.Length) throw new ArgumentOutOfRangeException(nameof(charIndex));
            if (charCount < 0 || charIndex + charCount > chars.Length) throw new ArgumentOutOfRangeException(nameof(charCount));
            if (byteIndex < 0 || byteIndex > bytes.Length) throw new ArgumentOutOfRangeException(nameof(byteIndex));

            if (this.fromUnicode == null)
            {
                var fromUnicode = new Dictionary<char, byte>(256);

                for (var i = 0; i < toUnicode.Length; i++)
                {
                    var unicode = toUnicode[i];
                    if (unicode != '\0')
                    {
                        fromUnicode[toUnicode[i]] = unchecked((byte)i);
                    }
                }

                this.fromUnicode = fromUnicode;
            }

            charCount = Math.Min(charCount, bytes.Length - byteIndex);

            for (var i = 0; i < charCount; i++)
            {
                if (fromUnicode.TryGetValue(chars[charIndex + i], out var b))
                {
                    bytes[byteIndex + i] = b;
                }
                else if (fromUnicode.TryGetValue('?', out var questionMark))
                {
                    bytes[byteIndex + i] = questionMark;
                }
                else
                {
                    bytes[byteIndex + i] = 0;
                }
            }

            return charCount;
        }

        public override int GetCharCount(byte[] bytes, int index, int count) => count;

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (chars == null) throw new ArgumentNullException(nameof(chars));
            if (byteIndex < 0 || byteIndex > bytes.Length) throw new ArgumentOutOfRangeException(nameof(byteIndex));
            if (byteCount < 0 || byteIndex + byteCount > bytes.Length) throw new ArgumentOutOfRangeException(nameof(byteCount));
            if (charIndex < 0 || charIndex > chars.Length) throw new ArgumentOutOfRangeException(nameof(charIndex));

            byteCount = Math.Min(byteCount, chars.Length - charIndex);

            for (var i = 0; i < byteCount; i++)
            {
                var ch = toUnicode[bytes[byteIndex + i]];
                chars[charIndex + i] = ch == 0 ? '\ufffd' : ch;
            }

            return byteCount;
        }

        public override int GetMaxByteCount(int charCount) => charCount;
        public override int GetMaxCharCount(int byteCount) => byteCount;

        public CharacterCode GetCharacter(PdfString value, int index)
        {
            var charCode = value[index];
            var unicodeChar = toUnicode[charCode];
            return unicodeChar == 0 ? default(CharacterCode) : new CharacterCode(charCode, 1, unicodeChar.ToString());
        }

        public override bool IsSingleByte => true;
    }
}
