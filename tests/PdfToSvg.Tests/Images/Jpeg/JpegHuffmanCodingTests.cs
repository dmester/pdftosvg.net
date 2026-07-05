// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    public class JpegHuffmanCodingTests
    {
        [Test]
        // ITU T.81 Table F.1
        [TestCase(-2047, 11)]
        [TestCase(-1024, 11)]
        [TestCase(-1023, 10)]
        [TestCase(-512, 10)]
        [TestCase(-511, 9)]
        [TestCase(-256, 9)]
        [TestCase(-255, 8)]
        [TestCase(-128, 8)]
        [TestCase(-127, 7)]
        [TestCase(-64, 7)]
        [TestCase(-63, 6)]
        [TestCase(-32, 6)]
        [TestCase(-31, 5)]
        [TestCase(-16, 5)]
        [TestCase(-15, 4)]
        [TestCase(-8, 4)]
        [TestCase(-7, 3)]
        [TestCase(-4, 3)]
        [TestCase(-3, 2)]
        [TestCase(-2, 2)]
        [TestCase(-1, 1)]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(7, 3)]
        [TestCase(8, 4)]
        [TestCase(15, 4)]
        [TestCase(16, 5)]
        [TestCase(31, 5)]
        [TestCase(32, 6)]
        [TestCase(63, 6)]
        [TestCase(64, 7)]
        [TestCase(127, 7)]
        [TestCase(128, 8)]
        [TestCase(255, 8)]
        [TestCase(256, 9)]
        [TestCase(511, 9)]
        [TestCase(512, 10)]
        [TestCase(1023, 10)]
        [TestCase(1024, 11)]
        [TestCase(2047, 11)]
        public void GetSsss(int input, int expectedOutput)
        {
            var actualOutput = JpegHuffmanCoding.GetSsss(input);
            Assert.AreEqual(expectedOutput, actualOutput);
        }
    }
}
