// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Fax;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Fax
{
    internal class FaxReaderTests
    {
        [Test]
        public void TryReadRunLength_White()
        {
            var buffer = new byte[]
            {
                0b00110101, // White 0
                0b00110100, // White 63

                0b01001101, // White makeup 1728
                0b10011100, // White 10

                0b00000111, 0b11000000, 0b01111100, 0b11010100 // Makeup 2560 + 2560 + White 0
            };
            var reader = new VariableBitReader(buffer, 0, buffer.Length);

            int actual;
            Assert.IsTrue(reader.TryReadRunLength(FaxCodes.WhiteRunLengthCodes, out actual));
            Assert.AreEqual(0, actual);

            Assert.IsTrue(reader.TryReadRunLength(FaxCodes.WhiteRunLengthCodes, out actual));
            Assert.AreEqual(63, actual);

            Assert.IsTrue(reader.TryReadRunLength(FaxCodes.WhiteRunLengthCodes, out actual));
            Assert.AreEqual(1728 + 10, actual);

            Assert.IsTrue(reader.TryReadRunLength(FaxCodes.WhiteRunLengthCodes, out actual));
            Assert.AreEqual(2560 + 2560 + 0, actual);

            Assert.IsFalse(reader.TryReadRunLength(FaxCodes.WhiteRunLengthCodes, out _));
        }

        [Test]
        public void TryReadRunLength_Black()
        {
            var buffer = new byte[]
            {
                0b00001101, // Black 0
                0b11000001, // Black 63
                0b10011100,
                0b00001010, // Black makeup 1472
                0b10100000, // Black makeup 320
                0b01100110,
                0b00010000, // Black 10
            };
            var reader = new VariableBitReader(buffer, 0, buffer.Length);

            int actual;
            Assert.IsTrue(reader.TryReadRunLength(FaxCodes.BlackRunLengthCodes, out actual));
            Assert.AreEqual(0, actual);

            Assert.IsTrue(reader.TryReadRunLength(FaxCodes.BlackRunLengthCodes, out actual));
            Assert.AreEqual(63, actual);

            Assert.IsTrue(reader.TryReadRunLength(FaxCodes.BlackRunLengthCodes, out actual));
            Assert.AreEqual(1472 + 320 + 10, actual);

            Assert.IsFalse(reader.TryReadRunLength(FaxCodes.BlackRunLengthCodes, out _));
        }
    }
}
