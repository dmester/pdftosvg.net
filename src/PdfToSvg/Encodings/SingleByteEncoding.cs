// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal class SingleByteEncoding : Encoding
    {
        protected const string nullchar = null;

        private readonly string?[] toGlyphName;
        private readonly string?[] toUnicode;

        private Lazy<Dictionary<string, byte>> fromUnicode;
        private Lazy<int> maxUnicodeLength;

        internal SingleByteEncoding(string?[] toUnicode, string?[] toGlyphName)
        {
            if (toUnicode == null) throw new ArgumentNullException(nameof(toUnicode));
            if (toUnicode.Length != 256) throw new ArgumentException("Expected a 256 characters long Unicode mapping string.", nameof(toUnicode));

            if (toGlyphName == null) throw new ArgumentNullException(nameof(toGlyphName));
            if (toGlyphName.Length != 256) throw new ArgumentException("Expected a 256 characters long glyph name mapping array.", nameof(toGlyphName));

            this.toUnicode = toUnicode;
            this.toGlyphName = toGlyphName;

            maxUnicodeLength = new(() =>
            {
                var localMaxUnicodeLength = 0;

                for (var i = 0; i < toUnicode.Length; i++)
                {
                    var unicode = toUnicode[i];

                    if (unicode != null &&
                        localMaxUnicodeLength < unicode.Length)
                    {
                        localMaxUnicodeLength = unicode.Length;
                    }
                }

                return localMaxUnicodeLength;
            }, LazyThreadSafetyMode.PublicationOnly);

            fromUnicode = new(() =>
            {
                var fromUnicode = new Dictionary<string, byte>(256);

                for (var i = 0; i < toUnicode.Length; i++)
                {
                    var unicode = toUnicode[i];
                    if (unicode != null)
                    {
                        fromUnicode[unicode] = unchecked((byte)i);
                    }
                }

                return fromUnicode;
            }, LazyThreadSafetyMode.PublicationOnly);
        }

        protected static string?[] GetGlyphNameLookup(string?[] toUnicode)
        {
            var result = new string?[toUnicode.Length];

            for (var i = 0; i < toUnicode.Length; i++)
            {
                var unicode = toUnicode[i];

                if (unicode != null && AdobeGlyphList.TryGetGlyphName(unicode, out var glyphName))
                {
                    result[i] = glyphName;
                }
            }

            return result;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null) throw new ArgumentNullException(nameof(chars));
            if (index < 0 || index > chars.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > chars.Length) throw new ArgumentOutOfRangeException(nameof(count));

            var fromUnicode = this.fromUnicode.Value;
            var maxUnicodeLength = this.maxUnicodeLength.Value;

            var byteCount = 0;

            for (var i = 0; i < count;)
            {
                for (var unicodeLength = 1; unicodeLength <= maxUnicodeLength && unicodeLength + i <= count; unicodeLength++)
                {
                    if (fromUnicode.TryGetValue(new string(chars, index + i, unicodeLength), out var b))
                    {
                        byteCount++;
                        i += unicodeLength;
                        goto Found;
                    }
                }

                byteCount++;
                i++;

            Found:;
            }

            return byteCount;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null) throw new ArgumentNullException(nameof(chars));
            if (bytes == null) throw new ArgumentNullException(nameof(chars));
            if (charIndex < 0 || charIndex > chars.Length) throw new ArgumentOutOfRangeException(nameof(charIndex));
            if (charCount < 0 || charIndex + charCount > chars.Length) throw new ArgumentOutOfRangeException(nameof(charCount));
            if (byteIndex < 0 || byteIndex > bytes.Length) throw new ArgumentOutOfRangeException(nameof(byteIndex));

            var fromUnicode = this.fromUnicode.Value;
            var maxUnicodeLength = this.maxUnicodeLength.Value;

            var byteCursor = byteIndex;

            for (var i = 0; i < charCount;)
            {
                if (byteCursor >= bytes.Length)
                {
                    throw new ArgumentException("bytes buffer too small", nameof(bytes));
                }

                for (var unicodeLength = 1; unicodeLength <= maxUnicodeLength && unicodeLength + i <= charCount; unicodeLength++)
                {
                    if (fromUnicode.TryGetValue(new string(chars, charIndex + i, unicodeLength), out var b))
                    {
                        bytes[byteCursor++] = b;
                        i += unicodeLength;
                        goto Found;
                    }
                }

                if (fromUnicode.TryGetValue("?", out var questionMark))
                {
                    bytes[byteCursor++] = questionMark;
                }
                else
                {
                    bytes[byteCursor++] = 0;
                }

                i++;

            Found:;
            }

            return byteCursor - byteIndex;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (index < 0 || index > bytes.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > bytes.Length) throw new ArgumentOutOfRangeException(nameof(count));

            var charCount = 0;

            for (var i = 0; i < count; i++)
            {
                var unicode = toUnicode[bytes[index + i]];

                if (unicode != null)
                {
                    charCount += unicode.Length;
                }
                else
                {
                    charCount++;
                }
            }

            return charCount;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (chars == null) throw new ArgumentNullException(nameof(chars));
            if (byteIndex < 0 || byteIndex > bytes.Length) throw new ArgumentOutOfRangeException(nameof(byteIndex));
            if (byteCount < 0 || byteIndex + byteCount > bytes.Length) throw new ArgumentOutOfRangeException(nameof(byteCount));
            if (charIndex < 0 || charIndex > chars.Length) throw new ArgumentOutOfRangeException(nameof(charIndex));

            var charCursor = charIndex;

            for (var i = 0; i < byteCount; i++)
            {
                var unicode = toUnicode[bytes[byteIndex + i]];

                if (unicode != null)
                {
                    if (charCursor + unicode.Length > chars.Length)
                    {
                        throw new ArgumentException("The chars buffer is too small.", nameof(chars));
                    }

                    for (var j = 0; j < unicode.Length; j++)
                    {
                        chars[charCursor++] = unicode[j];
                    }
                }
                else
                {
                    if (charCursor + 1 > chars.Length)
                    {
                        throw new ArgumentException("The chars buffer is too small.", nameof(chars));
                    }

                    chars[charCursor++] = '\ufffd';
                }
            }

            return charCursor - charIndex;
        }

        public override int GetMaxByteCount(int charCount) => charCount;
        public override int GetMaxCharCount(int byteCount) => maxUnicodeLength.Value * byteCount;

        public string? GetUnicode(byte charCode)
        {
            return toUnicode[charCode];
        }

        public string? GetGlyphName(byte charCode)
        {
            return toGlyphName[charCode];
        }

        public override bool IsSingleByte => true;
    }
}
