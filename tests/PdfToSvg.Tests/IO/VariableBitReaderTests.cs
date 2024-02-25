// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.IO
{
    internal class VariableBitReaderTests
    {
        [Test]
        public void ReadInt()
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
        }

        [Test]
        public void ReadLong()
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
        }
    }
}
