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
    public class CompactFontWriterTests
    {
        private static byte[] ParseSpec(string spec)
        {
            return spec
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => byte.Parse(x, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                .ToArray();
        }

        [Test]
        public void WriteDict()
        {
            var expected = new byte[]
            {
                // Start of DICT
                0x20, // Operand -107
                0x01, // Operator

                0xfa, 0x7c, // Operand 1000
                0xF5, // Operand 106
                0xF6, // Operand 107
                0x0c, 0x02, // Operator

                0x1e, 0xe2, 0xa2, 0x5f, // Operand -2.25
                0x14, // Operator
            };

            for (var capacity = 0; capacity < expected.Length + 1; capacity++)
            {
                var writer = new CompactFontWriter(capacity);

                writer.WriteDict(new KeyValuePair<int, double[]>[]
                {
                    new KeyValuePair<int, double[]>(0x01, new double[] { -107 }),
                    new KeyValuePair<int, double[]>(0x0c02, new double[] { 1000, 106, 107 }),
                    new KeyValuePair<int, double[]>(0x14, new double[] { -2.25 }),
                });

                var actual = writer.ToArray();

                Assert.AreEqual(expected, actual);

                if (capacity >= expected.Length)
                {
                    Assert.AreEqual(capacity, writer.Capacity, "Capacity");
                }
            }
        }

        private void WriteIndexData(byte[] expected, int[] index)
        {
            for (var capacity = expected.Length - 1; capacity < expected.Length + 1; capacity++)
            {
                var writer = new CompactFontWriter(capacity);

                writer.WriteIndex(index);

                var actual = writer.ToArray();

                Assert.AreEqual(expected, actual, "Output");

                if (capacity >= expected.Length)
                {
                    Assert.AreEqual(capacity, writer.Capacity, "Capacity");
                }
            }
        }

        [Test]
        public void WriteIndexData_Size0()
        {
            var expected = new byte[]
            {
                // Start of INDEX
                0x00, 0x00, // count
            };

            WriteIndexData(expected, new int[1]);
        }

        [Test]
        public void WriteIndexData_Absolute()
        {
            var expected = new byte[]
            {
                // Start of INDEX
                0x00, 0x01, // count
                0x01, // offSize

                0x01, 0x02, // offset
            };

            WriteIndexData(expected, new int[] { 741, 742 });
        }

        [Test]
        public void WriteIndexData_Size1Min()
        {
            var expected = new byte[]
            {
                // Start of INDEX
                0x00, 0x01, // count
                0x01, // offSize

                0x01, 0x01, // offset
            };

            WriteIndexData(expected, new int[] { 1, 1 });
        }

        [Test]
        public void WriteIndexData_Size1Max()
        {
            var expected = new byte[]
            {
                // Start of INDEX
                0x00, 0x01, // count
                0x01, // offSize

                0x01, 0xff, // offset
            };

            WriteIndexData(expected, new int[] { 1, 255 });
        }

        [Test]
        public void WriteIndexData_Size2Min()
        {
            var expected = new byte[]
            {
                // Start of INDEX
                0x00, 0x01, // count
                0x02, // offSize

                0x00, 0x01, 0x01, 0x00, // offset
            };

            WriteIndexData(expected, new int[] { 1, 256 });
        }

        [Test]
        public void WriteIndexData_Size2Max()
        {
            var expected = new byte[]
            {
                // Start of INDEX
                0x00, 0x01, // count
                0x02, // offSize

                0x00, 0x01, 0xff, 0xff, // offset
            };

            WriteIndexData(expected, new int[] { 1, 0xffff });
        }

        [Test]
        public void WriteIndexData_Size3Min()
        {
            var expected = new byte[]
            {
                // Start of INDEX
                0x00, 0x01, // count
                0x03, // offSize

                0x00, 0x00, 0x01,
                0x01, 0x00, 0x00, // offset
            };

            WriteIndexData(expected, new int[] { 1, 0x10000 });
        }

        [Test]
        public void WriteIndexData_Size3Max()
        {
            var expected = new byte[]
            {
                // Start of INDEX
                0x00, 0x01, // count
                0x03, // offSize

                0x00, 0x00, 0x01,
                0xff, 0xff, 0xff, // offset
            };

            WriteIndexData(expected, new int[] { 1, 0xffffff });
        }

        [Test]
        public void WriteIndexData_Size4Min()
        {
            var expected = new byte[]
            {
                // Start of INDEX
                0x00, 0x01, // count
                0x04, // offSize

                0x00, 0x00, 0x00, 0x01, // offset
                0x01, 0x00, 0x00, 0x00, // offset
            };

            WriteIndexData(expected, new int[] { 1, 0x1000000 });
        }

        [Test]
        public void WriteIndexData_Size4Max()
        {
            var expected = new byte[]
            {
                // Start of INDEX
                0x00, 0x01, // count
                0x04, // offSize

                0x00, 0x00, 0x00, 0x01, // offset
                0x0f, 0xff, 0xff, 0xff, // offset
            };

            WriteIndexData(expected, new int[] { 1, 0xfffffff });
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
        [TestCase(-2.25, "1e e2 a2 5f")]
        [TestCase(1.40541E+300, "1e 1a 40 54 1b 30 0f")]
        [TestCase(1.40541E-300, "1e 1a 40 54 1c 30 0f")]
        public void WriteNumber(double value, string bytes)
        {
            var expected = ParseSpec(bytes);

            for (var capacity = expected.Length - 1; capacity < expected.Length + 1; capacity++)
            {
                var writer = new CompactFontWriter(capacity);

                writer.WriteNumber(value);

                var actual = writer.ToArray();

                Assert.AreEqual(expected, actual, "Output");
            }
        }
    }
}
