// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.CompactFonts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.CompactFonts
{
    public class CompactFontReaderTests
    {
        private static CompactFontReader CreateReader(string spec)
        {
            var parsed = Hex.Decode(spec);
            return new CompactFontReader(parsed);
        }

        [Test]
        public void ReadDict()
        {
            var reader = new CompactFontReader(new byte[]
            {
                // Preceding data
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                // Start of DICT
                0x20, // Operand -107
                0x01, // Operator

                0xfa, 0x7c, // Operand 1000
                0xF5, // Operand 106
                0xF6, // Operand 107
                0x0c, 0x02, // Operator

                0x1e, 0xe2, 0xa2, 0x5f, // Operand -2.25
                0x14, // Operator

                // Data
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            });

            reader.Position = 16;

            var actual = reader.ReadDict(13);

            var expected = new Dictionary<int, double[]>
            {
                { 0x01, new double[] { -107 } },
                { 0x0c02, new double[] { 1000, 106, 107 } },
                { 0x14, new double[] { -2.25 } },
            };

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ReadIndex_Size0()
        {
            var reader = new CompactFontReader(new byte[]
            {
                // Preceding data
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                // Start of INDEX
                0x00, 0x00, // count
                
                // Data
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            });

            reader.Position = 16;

            Assert.AreEqual(new int[] { 18 }, reader.ReadIndex());
            Assert.AreEqual(18, reader.Position);
        }

        [Test]
        public void ReadIndex_Size1()
        {
            var reader = new CompactFontReader(new byte[]
            {
                // Preceding data
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                // Start of INDEX
                0x00, 0x03, // count
                0x01, // offSize

                0x01, 0x03, 0x05, 0x06, // offset

                // Data
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            });

            reader.Position = 16;

            Assert.AreEqual(new int[] { 23, 25, 27, 28 }, reader.ReadIndex());
        }

        [Test]
        public void ReadIndex_Size4()
        {
            var reader = new CompactFontReader(new byte[]
            {
                // Preceding data
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                // Start of index
                0x00, 0x03, // count
                0x04, // offSize

                0x00, 0x00, 0x00, 0x01, // offset
                0x00, 0x00, 0x00, 0x03,
                0x00, 0x00, 0x00, 0x05,
                0x00, 0x00, 0x01, 0x06,

                // Data
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            });

            reader.Position = 16;

            Assert.AreEqual(new int[] { 35, 37, 39, 296 }, reader.ReadIndex());
        }

        // Test cases from CFF spec Table 4
        [TestCase(0, "8b")]
        [TestCase(100, "ef")]
        [TestCase(-100, "27")]
        [TestCase(1000, "fa 7c")]
        [TestCase(-1000, "fe 7c")]
        [TestCase(10000, "1c 27 10")]
        [TestCase(-10000, "1c d8 f0")]
        [TestCase(100000, "1d 00 01 86 a0")]
        [TestCase(-100000, "1d ff fe 79 60")]
        public void ReadInteger(int expectedInteger, string bytes)
        {
            var reader = CreateReader(bytes);
            Assert.AreEqual(expectedInteger, reader.ReadInteger());
            Assert.AreEqual(reader.Length, reader.Position);
        }

        [Test]
        public void ReadInteger_NotAnInteger()
        {
            var reader = new CompactFontReader(new byte[] { 3, 45 });
            Assert.Throws<CompactFontException>(() => reader.ReadInteger());
        }

        [TestCase()]
        [TestCase(247)]
        [TestCase(252)]
        [TestCase(28, 1)]
        [TestCase(29, 1, 1, 1)]
        public void ReadInteger_EndOfStream(params int[] input)
        {
            var reader = new CompactFontReader(input.Select(x => (byte)x).ToArray());
            Assert.Throws<EndOfStreamException>(() => reader.ReadInteger());
        }

        [TestCase("1e")]
        [TestCase("1e e4 ab cc")]
        public void ReadReal_EndOfStream(string bytes)
        {
            var reader = CreateReader(bytes);
            Assert.Throws<EndOfStreamException>(() => reader.ReadReal());
        }

        [Test]
        public void ReadReal_Overflow()
        {
            var reader = new CompactFontReader(new byte[10000]);
            Assert.Throws<CompactFontException>(() => reader.ReadReal());
        }

        [TestCase("cc ff")]
        [TestCase("1e aa ff")]
        [TestCase("1e bb ff")]
        [TestCase("1e cc ff")]
        public void ReadReal_Invalid(string bytes)
        {
            var reader = CreateReader(bytes);
            Assert.Throws<CompactFontException>(() => reader.ReadReal());
        }

        [TestCase(-2.25, "1e e2 a2 5f")]
        [TestCase(0.140541E-3, "1e 0a 14 05 41 c3 ff")]
        public void ReadReal(double expected, string bytes)
        {
            var reader = CreateReader(bytes);
            Assert.AreEqual(expected, reader.ReadReal());
        }
    }
}
