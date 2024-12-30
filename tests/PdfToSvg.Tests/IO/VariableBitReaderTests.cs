// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.IO
{
    internal class VariableBitReaderTests
    {
        [Test]
        public void ReadBits_Int32()
        {
            var buffer = new byte[]
            {
                0b00110101,
                0b01110100,
                0b01001101,
                0b10011100,
            };
            var reader = new VariableBitReader(buffer, 0, buffer.Length);

            Assert.AreEqual(0b00, reader.ReadBits(2));

            var cursor = reader.Cursor;
            Assert.AreEqual(0b11010101, reader.ReadBits(8));
            Assert.AreEqual(0b11010001, reader.ReadBits(8));

            reader.Cursor = cursor;
            Assert.AreEqual(0b11010101, reader.ReadBits(8));

            Assert.AreEqual(-1, reader.ReadBits(32));
            Assert.Throws<EndOfStreamException>(() => reader.ReadBitsOrThrow(32));
        }

        [Test]
        public void ReadBits_Int64()
        {
            var buffer = new byte[]
            {
                0b10110101,
                0b01110100,
                0b01001101,
                0b10011100,
            };
            var reader = new VariableBitReader(buffer, 0, buffer.Length);

            Assert.AreEqual(0b10110101011101000100110110011100L, reader.ReadLongBits(32));
            Assert.AreEqual(-1, reader.ReadLongBits(32));
            Assert.Throws<EndOfStreamException>(() => reader.ReadLongBitsOrThrow(32));
        }

        [Test]
        public void ReadBytes_Aligned()
        {
            var buffer = new byte[]
            {
                0b10110101,
                0b01110100,
                0b01001101,
                0b10011100,
            };
            var reader = new VariableBitReader(buffer, 0, buffer.Length);

            Assert.AreEqual(0b10110101, reader.ReadBytes(1));
            Assert.AreEqual(0b01110100_01001101, reader.ReadBytes(2));
            Assert.AreEqual(0b10011100, reader.ReadBytes(1));

            Assert.AreEqual(-1, reader.ReadBytes(1));
            Assert.Throws<EndOfStreamException>(() => reader.ReadBytesOrThrow(1));
        }

        [TestCase("Start of byte", 0, 2, 6, 0b110101)]
        [TestCase("Inside byte", 2, 2, 4, 0b0101)]
        [TestCase("End of byte", 6, 2, 8, 0b01110100)]
        [TestCase("Entire byte", 8, 8, 8, 0b01001101)]
        [TestCase("Several bytes", 8, 16, 8, 0b10011100)]
        [TestCase("Spanning partial bytes", 4, 16, 8, 0b1101_1001)]
        [TestCase("Skipping more bits than available", 4, 32, 8, -1)]
        public void SkipBits(string name, int bitCountBefore, int skipBits, int bitCountAfter, int expectedValue)
        {
            var buffer = new byte[]
            {
                0b00110101,
                0b01110100,
                0b01001101,
                0b10011100,
            };
            var reader = new VariableBitReader(buffer, 0, buffer.Length);

            if (bitCountBefore > 0)
            {
                reader.ReadBits(bitCountBefore);
            }

            reader.SkipBits(skipBits);

            Assert.AreEqual(expectedValue, reader.ReadBits(bitCountAfter));
        }
    }
}
