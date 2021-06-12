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
using System.Threading.Tasks;

namespace PdfToSvg.Tests.IO
{
    public class BitReaderTests
    {
        [Test]
        public void Read1bitUInt()
        {
            var stream = new ByteByByteMemoryStream(0b01010111, 0b10110111);

            var reader = new BitReader(stream, bitsPerValue: 1);
            var buffer = new uint[18];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(16, read);
            Assert.AreEqual(new uint[] {
                0, 1, 0, 1, 0, 1, 1, 1,
                1, 0, 1, 1, 0, 1, 1, 1,
                0, 0,
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read2bitUInt()
        {
            var stream = new ByteByByteMemoryStream(0b01010100, 0b10110111);

            var reader = new BitReader(stream, bitsPerValue: 2);
            var buffer = new uint[10];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(8, read);
            Assert.AreEqual(new uint[] {
                1, 1, 1, 0,
                2, 3, 1, 3,
                0, 0,
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read3bitUInt()
        {
            var stream = new ByteByByteMemoryStream(0b01010101, 0b10111111);

            var reader = new BitReader(stream, bitsPerValue: 3);
            var buffer = new uint[8];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(5, read);
            Assert.AreEqual(new uint[] {
                2, 5, 3, 3, 7, 0, 0, 0
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read4bitUInt()
        {
            var stream = new ByteByByteMemoryStream(0b01010100, 0b10110111);

            var reader = new BitReader(stream, bitsPerValue: 4);
            var buffer = new uint[10];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(4, read);
            Assert.AreEqual(new uint[] {
                5, 4, 11, 7,
                0, 0, 0, 0, 0, 0,
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read8bitUInt()
        {
            var stream = new ByteByByteMemoryStream(255, 0, 127, 129);

            var reader = new BitReader(stream, bitsPerValue: 8);
            var buffer = new uint[5];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(4, read);
            Assert.AreEqual(new uint[] { 255, 0, 127, 129, 0 }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read12bitUInt()
        {
            var stream = new ByteByByteMemoryStream(0x13, 0xfa, 0x66, 0x23, 0x01, 0x99);

            var reader = new BitReader(stream, bitsPerValue: 12);
            var buffer = new uint[5];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(4, read);
            Assert.AreEqual(new uint[] { 0x13f, 0xa66, 0x230, 0x199, 0 }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read16bitUInt()
        {
            var stream = new ByteByByteMemoryStream(0x13, 0xfa, 0x66, 0x23, 0x01, 0x99);

            var reader = new BitReader(stream, bitsPerValue: 16);
            var buffer = new uint[4];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(3, read);
            Assert.AreEqual(new uint[] { 0x13fa, 0x6623, 0x0199, 0 }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read20bitUInt()
        {
            var stream = new ByteByByteMemoryStream(0x13, 0xfa, 0x66, 0x23, 0x01, 0x99, 0xfc, 0x0a, 0x71, 0xea);

            var reader = new BitReader(stream, bitsPerValue: 20);
            var buffer = new uint[4];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(4, read);
            Assert.AreEqual(new uint[] { 0x13fa6, 0x62301, 0x99fc0, 0xa71ea }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read32bitUInt()
        {
            var stream = new ByteByByteMemoryStream(0x13, 0xfa, 0x66, 0x23, 0x01, 0x99, 0x23, 0x01, 0xaf);

            var reader = new BitReader(stream, bitsPerValue: 32);
            var buffer = new uint[4];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(2, read);
            Assert.AreEqual(new uint[] { 0x13fa6623, 0x01992301, 0, 0 }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read1bitFloat()
        {
            var stream = new ByteByByteMemoryStream(0b01010111, 0b10110111);

            var reader = new BitReader(stream, bitsPerValue: 1);
            var buffer = new float[18];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(16, read);
            Assert.AreEqual(new float[] {
                0, 1, 0, 1, 0, 1, 1, 1,
                1, 0, 1, 1, 0, 1, 1, 1,
                0, 0,
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read2bitFloat()
        {
            var stream = new ByteByByteMemoryStream(0b01010100, 0b10110111);

            var reader = new BitReader(stream, bitsPerValue: 2);
            var buffer = new float[10];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(8, read);
            Assert.AreEqual(new float[] {
                1, 1, 1, 0,
                2, 3, 1, 3,
                0, 0,
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read3bitFloat()
        {
            var stream = new ByteByByteMemoryStream(0b01010101, 0b10111111);

            var reader = new BitReader(stream, bitsPerValue: 3);
            var buffer = new float[8];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(5, read);
            Assert.AreEqual(new float[] {
                2, 5, 3, 3, 7, 0, 0, 0
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read4bitFloat()
        {
            var stream = new ByteByByteMemoryStream(0b01010100, 0b10110111);

            var reader = new BitReader(stream, bitsPerValue: 4);
            var buffer = new float[10];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(4, read);
            Assert.AreEqual(new float[] {
                5, 4, 11, 7,
                0, 0, 0, 0, 0, 0,
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read8bitFloat()
        {
            var stream = new ByteByByteMemoryStream(255, 0, 127, 129);

            var reader = new BitReader(stream, bitsPerValue: 8);
            var buffer = new float[5];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(4, read);
            Assert.AreEqual(new float[] { 255, 0, 127, 129, 0 }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read12bitFloat()
        {
            var stream = new ByteByByteMemoryStream(0x13, 0xfa, 0x66, 0x23, 0x01, 0x99);

            var reader = new BitReader(stream, bitsPerValue: 12);
            var buffer = new float[5];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(4, read);
            Assert.AreEqual(new float[] { 0x13f, 0xa66, 0x230, 0x199, 0 }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read16bitFloat()
        {
            var stream = new ByteByByteMemoryStream(0x13, 0xfa, 0x66, 0x23, 0x01, 0x99);

            var reader = new BitReader(stream, bitsPerValue: 16);
            var buffer = new float[4];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(3, read);
            Assert.AreEqual(new float[] { 0x13fa, 0x6623, 0x0199, 0 }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read20bitFloat()
        {
            var stream = new ByteByByteMemoryStream(0x13, 0xfa, 0x66, 0x23, 0x01, 0x99, 0xfc, 0x0a, 0x71, 0xea);

            var reader = new BitReader(stream, bitsPerValue: 20);
            var buffer = new float[4];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(4, read);
            Assert.AreEqual(new float[] { 0x13fa6, 0x62301, 0x99fc0, 0xa71ea }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read32bitFloat()
        {
            var stream = new ByteByByteMemoryStream(0x13, 0xfa, 0x66, 0x23, 0x01, 0x99, 0x23, 0x01, 0xaf);

            var reader = new BitReader(stream, bitsPerValue: 32);
            var buffer = new uint[4];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(2, read);
            Assert.AreEqual(new uint[] { 0x13fa6623, 0x01992301, 0, 0 }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }
    }
}
