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
            var reader = new JpegImageDataReader(data, 0, data.Length);

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
            var reader = new JpegImageDataReader(data, 0, data.Length);

            Assert.AreEqual(1, reader.ReadBits(2));
            Assert.AreEqual(0b1010101, reader.ReadBits(7));
            Assert.AreEqual(0b1001110, reader.ReadBits(7));
            Assert.AreEqual(0b1010110011100010, reader.ReadBits(16));
            Assert.AreEqual(-1, reader.ReadBits(7));
        }

        [Test]
        public void ReadBits_ByteStuffing()
        {
            var data = new byte[] { 0b11111111, 0, 0b10101100 };
            var reader = new JpegImageDataReader(data, 0, data.Length);

            Assert.AreEqual(0b1111111110101100, reader.ReadBits(16));
        }

        [Test]
        public void ReadBits_TooLong()
        {
            var data = new byte[] { 0b01101010 };
            var reader = new JpegImageDataReader(data, 0, data.Length);

            Assert.AreEqual(-1, reader.ReadBits(9));
            Assert.AreEqual(-1, reader.ReadBits(1));
            Assert.AreEqual(-1, reader.ReadBits(0));
        }

        [Test]
        public void ReadValue()
        {
            var data = new byte[] { 0b01111111 };
            var reader = new JpegImageDataReader(data, 0, data.Length);

            var value = reader.ReadValue(4);
            Assert.AreEqual(-8, value);
        }

        [Test]
        public void ReadHuffman()
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

            var table = JpegHuffmanTable.DefaultLuminanceDCTable;

            var data = new byte[]
            {
                0, 0, 0, 0, 0,

                0b00111011, 0b01111110, // 0 6 5 9
                0b10111111, 0b11101001, // 4 11 3

                0, 0, 0, 0, 0,
            };

            var reader = new JpegImageDataReader(data, 5, 4);

            Assert.AreEqual(0, reader.ReadHuffman(table));
            Assert.AreEqual(6, reader.ReadHuffman(table));
            Assert.AreEqual(5, reader.ReadHuffman(table));
            Assert.AreEqual(9, reader.ReadHuffman(table));
            Assert.AreEqual(4, reader.ReadHuffman(table));
            Assert.AreEqual(11, reader.ReadHuffman(table));
            Assert.AreEqual(3, reader.ReadHuffman(table));
            Assert.AreEqual(-1, reader.ReadHuffman(table));
        }
    }
}
