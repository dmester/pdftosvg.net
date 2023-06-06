// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.Type1
{
    internal static class Type1Decryptor
    {
        public static void DecryptEexec(byte[] input, int offset, int count)
        {
            const ushort key = 55665;
            Decrypt(input, offset, count, key);
        }

        public static void DecryptCharString(byte[] input, int offset, int count)
        {
            const ushort key = 4330;
            Decrypt(input, offset, count, key);
        }

        public static void Decrypt(byte[] input, int offset, int count, uint key)
        {
            // Type 1 spec, section 7.1

            ushort r = (ushort)key;
            const ushort c1 = 52845;
            const ushort c2 = 22719;

            for (var i = 0; i < count; i++)
            {
                var cipher = input[offset + i];
                var plain = cipher ^ (r >> 8);

                input[offset + i] = (byte)plain;

                r = (ushort)(((cipher + r) * c1) + c2);
            }
        }

        public static int DecodeAscii(byte[] input, int offset, int count)
        {
            var sampleSize = Math.Min(200, count);

            for (var i = 0; i < sampleSize; i++)
            {
                var val = (char)input[offset + i];

                if (!PdfCharacters.IsWhiteSpace(val) &&
                    PdfCharacters.ParseHexDigit(val) < 0)
                {
                    // Not hex encoded
                    return count;
                }
            }

            int hi = -1;
            var writtenBytes = 0;

            for (var i = 0; i < count; i++)
            {
                var hex = PdfCharacters.ParseHexDigit((char)input[offset + i]);

                // Clear remainings
                input[offset + i] = (byte)' ';

                if (hex >= 0)
                {
                    if (hi < 0)
                    {
                        hi = hex;
                    }
                    else
                    {
                        input[offset + writtenBytes] = (byte)((hi << 4) | hex);
                        hi = -1;
                        writtenBytes++;
                    }
                }
            }

            return writtenBytes;
        }
    }
}
