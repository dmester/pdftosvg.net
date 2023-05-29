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
    internal class JpegImageDataWriterTests
    {
        [Test]
        public void WriteBit_Partial()
        {
            TestWriter(new byte[] { 0b11001111 }, writer =>
            {
                writer.WriteBit(1);
                writer.WriteBit(1);
                writer.WriteBit(0);
                writer.WriteBit(0);
            });
        }

        [Test]
        public void WriteBit_SpanTwoBytes()
        {
            TestWriter(new byte[] { 0b11001010, 0b11100101 }, writer =>
            {
                writer.WriteBit(1);
                writer.WriteBit(1);
                writer.WriteBit(0);
                writer.WriteBit(0);

                writer.WriteBit(1);
                writer.WriteBit(0);
                writer.WriteBit(1);
                writer.WriteBit(0);

                writer.WriteBit(1);
                writer.WriteBit(1);
                writer.WriteBit(1);
                writer.WriteBit(0);

                writer.WriteBit(0);
                writer.WriteBit(1);
                writer.WriteBit(0);
                writer.WriteBit(1);
            });
        }

        [Test]
        public void WriteBit_ByteStuffing()
        {
            TestWriter(new byte[] { 0b11111111, 0, 0b01111111 }, writer =>
            {
                writer.WriteBit(1);
                writer.WriteBit(1);
                writer.WriteBit(1);
                writer.WriteBit(1);

                writer.WriteBit(1);
                writer.WriteBit(1);
                writer.WriteBit(1);
                writer.WriteBit(1);

                writer.WriteBit(0);
            });
        }

        [Test]
        public void WriteBits_Partial()
        {
            TestWriter(new byte[] { 0b01011001 }, writer =>
            {
                writer.WriteBits(0b0101100, 7);
            });
        }

        [Test]
        public void WriteBits_Overlap()
        {
            TestWriter(new byte[] { 0b00100110, 0b01011111 }, writer =>
            {
                writer.WriteBits(0b00, 2);

                writer.WriteBits(0b100110010, 9);
            });
        }

        [Test]
        public void WriteRestartMarker()
        {
            TestWriter(new byte[] {
                0b00111111,
                0xff, 0xd0,
                0xff, 0xd1,
                0xff, 0xd2,
                0xff, 0xd3,
                0xff, 0xd4,
                0b00000000, 0b00111111,
                0xff, 0xd5,
                0xff, 0xd6,
                0xff, 0xd7,
                0xff, 0xd0
            }, writer =>
            {
                writer.WriteBits(0, 2);

                writer.WriteRestartMarker();
                writer.WriteRestartMarker();
                writer.WriteRestartMarker();
                writer.WriteRestartMarker();
                writer.WriteRestartMarker();

                writer.WriteBits(0, 10);

                writer.WriteRestartMarker();
                writer.WriteRestartMarker();
                writer.WriteRestartMarker();
                writer.WriteRestartMarker();
            });
        }

        [TestCase(0b01111111, 0, 0)]
        [TestCase(0b00111111, 1, -1)]
        [TestCase(0b10111111, 1, 1)]
        [TestCase(0b00000111, 4, -15)]
        [TestCase(0b01110111, 4, -8)]
        [TestCase(0b10000111, 4, 8)]
        [TestCase(0b11110111, 4, 15)]
        public void WriteValue(int expectedByte, int ssss, int value)
        {
            TestWriter(new byte[] { (byte)expectedByte }, writer =>
            {
                writer.WriteValue(ssss, value);
                writer.WriteBits(0, 1);
            });
        }

        private void TestWriter(byte[] expectedResult, Action<JpegImageDataWriter> callback)
        {
            var stream = new MemoryStream();

            using (var writer = new JpegImageDataWriter(stream))
            {
                callback(writer);
            }

            var arr = stream.ToArray();
            Assert.AreEqual(expectedResult, arr);
        }
    }
}
