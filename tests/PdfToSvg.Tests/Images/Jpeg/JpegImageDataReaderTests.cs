// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal class JpegImageDataReaderTests
    {
        [Test]
        public void ReadRestartMarker()
        {
            var data = new byte[] { 0b01101111, 0xff, 0xd0, 0b01111111, 0xff, 0xd7, 0b11100010 };
            var reader = new JpegImageDataReader(data);

            Assert.AreEqual(0b01101111, reader.ReadBits(8));
            Assert.IsTrue(reader.ReadRestartMarker());

            Assert.AreEqual(0b01, reader.ReadBits(2));
            Assert.IsTrue(reader.ReadRestartMarker());

            Assert.IsFalse(reader.ReadRestartMarker());
        }

        [Test]
        public void ReadBits()
        {
            var data = new byte[] { 0b01101010, 0b11001110, 0b10101100, 0b11100010 };
            var reader = new JpegImageDataReader(data);

            Assert.AreEqual(1, reader.ReadBits(2));
            Assert.AreEqual(0b1010101, reader.ReadBits(7));
            Assert.AreEqual(0b1001110, reader.ReadBits(7));
            Assert.AreEqual(0b1010110011100010, reader.ReadBits(16));
            Assert.AreEqual(-1, reader.ReadBits(7));
        }

        [Test]
        public void ReadBit_ByteStuffing()
        {
            var data = new byte[] { 0b11111111, 0, 0b10101100 };
            var reader = new JpegImageDataReader(data);

            // Byte 0
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(1, reader.ReadBit());

            // Byte 1: stuffed byte

            // Byte 2
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(0, reader.ReadBit());
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(0, reader.ReadBit());
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(1, reader.ReadBit());
            Assert.AreEqual(0, reader.ReadBit());
            Assert.AreEqual(0, reader.ReadBit());
        }

        [Test]
        public void ReadBits_ByteStuffing()
        {
            var data = new byte[] { 0b11111111, 0, 0b10101100 };
            var reader = new JpegImageDataReader(data);

            Assert.AreEqual(0b1111111110101100, reader.ReadBits(16));
        }

        [Test]
        public void ReadBits_TooLong()
        {
            var data = new byte[] { 0b01101010 };
            var reader = new JpegImageDataReader(data);

            Assert.AreEqual(-1, reader.ReadBits(9));
            Assert.AreEqual(-1, reader.ReadBits(1));
            Assert.AreEqual(0, reader.ReadBits(0));
        }

        [Test]
        public void ReadValue()
        {
            var data = new byte[] { 0b01111111 };
            var reader = new JpegImageDataReader(data);

            var value = reader.ReadValue(4);
            Assert.AreEqual(-8, value);
        }
    }
}
