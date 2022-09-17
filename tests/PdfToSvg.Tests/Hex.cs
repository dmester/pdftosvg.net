// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests
{
    internal class Hex
    {
        public static byte[] Decode(string input)
        {
            var result = new byte[(input.Length + 1) / 2];
            var resultCursor = 0;

            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];

                int digit;

                if (ch >= '0' && ch <= '9')
                {
                    digit = ch - '0';
                }
                else if (ch >= 'a' && ch <= 'f')
                {
                    digit = ch - 'a' + 10;
                }
                else if (ch >= 'A' && ch <= 'F')
                {
                    digit = ch - 'A' + 10;
                }
                else
                {
                    continue;
                }

                if ((resultCursor & 1) == 0)
                {
                    result[resultCursor >> 1] = (byte)(digit << 4);
                }
                else
                {
                    result[resultCursor >> 1] = (byte)(digit | result[resultCursor >> 1]);
                }

                resultCursor++;
            }

            var length = (resultCursor + 1) / 2;
            if (length != result.Length)
            {
                result = result.Slice(0, length);
            }

            return result;
        }

        [TestCase("")]
        [TestCase("000108090A0b0E0F", 0x00, 0x01, 0x08, 0x09, 0x0a, 0x0b, 0x0e, 0x0f)]
        [TestCase("F0F1F8F9FAFbFEFF", 0xf0, 0xf1, 0xf8, 0xf9, 0xfa, 0xfb, 0xfe, 0xff)]
        [TestCase("00", 0x00)]
        [TestCase("00CA0", 0x00, 0xCA, 0x00)]
        [TestCase("00CA1 ", 0x00, 0xCA, 0x10)]
        [TestCase("00CA02", 0x00, 0xCA, 0x02)]
        [TestCase("00 CA 02", 0x00, 0xCA, 0x02)]
        [TestCase("00 CA 02  ", 0x00, 0xCA, 0x02)]
        [TestCase("  00 CA 02", 0x00, 0xCA, 0x02)]
        [TestCase("<00 CA 02>", 0x00, 0xCA, 0x02)]
        public void TestDecode(string input, params int[] expectedOutput)
        {
            Assert.AreEqual(expectedOutput, Decode(input));
        }
    }
}
