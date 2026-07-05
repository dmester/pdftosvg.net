// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal class JpegHuffmanTableTests
    {
        [TestCase(1, 0b010, 3)]
        [TestCase(7, 0b11110, 5)]
        public void EncodeOrThrow_Existing(int value, int expectedCode, int expectedCodeLength)
        {
            var code = JpegHuffmanTable.DefaultLuminanceDCTable.EncodeOrThrow(value);

            Assert.AreEqual(expectedCode, code.Code);
            Assert.AreEqual(expectedCodeLength, code.CodeLength);
        }

        [Test]
        public void EncodeOrThrow_NonExisting()
        {
            Assert.Throws<JpegException>(() =>
            {
                JpegHuffmanTable.DefaultLuminanceDCTable.EncodeOrThrow(125);
            });
        }

        [Test]
        public void ReadValueFrom()
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

            Assert.AreEqual(0, table.ReadValueFrom(reader));
            Assert.AreEqual(6, table.ReadValueFrom(reader));
            Assert.AreEqual(5, table.ReadValueFrom(reader));
            Assert.AreEqual(9, table.ReadValueFrom(reader));
            Assert.AreEqual(4, table.ReadValueFrom(reader));
            Assert.AreEqual(11, table.ReadValueFrom(reader));
            Assert.AreEqual(3, table.ReadValueFrom(reader));
            Assert.AreEqual(-1, table.ReadValueFrom(reader));
        }

        [Test]
        public void FuzzyRead()
        {
            var table = JpegHuffmanTable.DefaultLuminanceACTable;

            var expectedBytes = new byte[1000];

            var random = new Random(0);
            for (var i = 0; i < expectedBytes.Length; i++)
            {
                var huffValIndex = random.Next(0, table.Huffval.Count);
                expectedBytes[i] = table.Huffval.Array[table.Huffval.Offset + huffValIndex];
            }

            var memoryStream = new MemoryStream();

            using (var writer = new JpegImageDataWriter(memoryStream))
            {
                foreach (var b in expectedBytes)
                {
                    var code = table.EncodeOrThrow(b);
                    writer.WriteCode(code);
                }
            }

            var data = memoryStream.ToArray();
            var reader = new JpegImageDataReader(data);

            for (var i = 0; i < expectedBytes.Length; i++)
            {
                Assert.AreEqual(expectedBytes[i], table.ReadValueFrom(reader), "Index {0}", i);
            }
        }
    }
}
