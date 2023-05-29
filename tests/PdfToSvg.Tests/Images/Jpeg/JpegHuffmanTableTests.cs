// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal class JpegHuffmanTableTests
    {
        // Using table K.3 from ITU T.81 as test case

        // Category | Code length | Code word
        // -------- | ----------- |----------
        //    0     |     2       | 00
        //    1     |     3       | 010
        //    2     |     3       | 011
        //    3     |     3       | 100
        //    4     |     3       | 101
        //    5     |     3       | 110
        //    6     |     4       | 1110
        //    7     |     5       | 11110
        //    8     |     6       | 111110
        //    9     |     7       | 1111110
        //   10     |     8       | 11111110
        //   11     |     9       | 111111110

        [TestCase(0b010, 3, 1)]
        [TestCase(0b11110, 5, 7)]
        [TestCase(0b11110, 6, -1)]
        [TestCase(0b11100, 5, -1)]
        public void TryDecode(int code, int codeLength, int expectedValue)
        {
            int value;

            if (JpegHuffmanTable.DefaultLuminanceDCTable.TryDecode(code, codeLength, out var byteValue))
            {
                value = byteValue;
            }
            else
            {
                value = -1;
            }

            Assert.AreEqual(expectedValue, value);
        }

        [TestCase(1, 0b010, 3)]
        [TestCase(7, 0b11110, 5)]
        public void EncodeOrThrow_Existing(int value, int expectedCode, int expectedCodeLength)
        {
            var code = JpegHuffmanTable.DefaultLuminanceDCTable.EncodeOrThrow(value);

            Assert.AreEqual(expectedCode, code.Code);
            Assert.AreEqual(expectedCodeLength, code.CodeLength);
        }

        [Test]
        public void EncodeOrThrow_NonExisting()
        {
            Assert.Throws<JpegException>(() =>
            {
                JpegHuffmanTable.DefaultLuminanceDCTable.EncodeOrThrow(125);
            });
        }
    }
}
