// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jbig2.Coding;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PdfToSvg.Tests.Images.Jbig2
{
    public class JbigHuffmanTableTests
    {
        private static byte[] DecodeData(string data)
        {
            var bytes = new byte[8];
            var cursor = 0;
            var bitShift = 7;

            foreach (var ch in data)
            {
                if (ch == '0' || ch == '1')
                {
                    var val = (ch == '1' ? 1 : 0) << bitShift;

                    bytes[cursor] = (byte)(bytes[cursor] | val);

                    bitShift--;

                    if (bitShift < 0)
                    {
                        bitShift = 7;
                        cursor++;
                    }
                }
            }

            return bytes;
        }

        [TestCase("TableB1", "0 1111", 15)]
        [TestCase("TableB1", "111 0", 65808)]

        [TestCase("TableB3", "11111110 00000000", -256)]
        [TestCase("TableB3", "11111110 11111111", -1)]
        [TestCase("TableB3", "11111111 0", -257)]
        [TestCase("TableB3", "11111111 00000000 00000000 00000000 00000001", -258)]
        [TestCase("TableB3", "11110 111111", 74)]
        [TestCase("TableB3", "1111110 0", 75)]
        [TestCase("TableB3", "1111110 00000000 00000000 00000000 00000001", 76)]

        [TestCase("TableB4", "10", 2)]
        [TestCase("TableB4", "1110 000", 4)]
        [TestCase("TableB4", "1110 111", 11)]
        [TestCase("TableB4", "11110 000000", 12)]
        [TestCase("TableB4", "11110 111111", 75)]
        [TestCase("TableB4", "11111 0", 76)]
        public void StandardTable(string tableName, string data, int expectedDecodedValue)
        {
            var bytes = DecodeData(data);
            var reader = new VariableBitReader(bytes, 0, bytes.Length);

            var property = typeof(JbigStandardHuffmanTable).GetProperty(tableName, BindingFlags.Static | BindingFlags.Public);
            var table = (JbigHuffmanTable)property.GetValue(null);

            var actualDecodedValue = table.DecodeValue(reader);
            Assert.AreEqual(expectedDecodedValue, actualDecodedValue);
        }

        [TestCase("TableB2", "111111")]
        [TestCase("TableB3", "111110")]
        [TestCase("TableB8", "01")]
        [TestCase("TableB9", "00")]
        [TestCase("TableB10", "10")]
        public void StandardTable_Oob(string tableName, string data)
        {
            var bytes = DecodeData(data);
            var reader = new VariableBitReader(bytes, 0, bytes.Length);

            var property = typeof(JbigStandardHuffmanTable).GetProperty(tableName, BindingFlags.Static | BindingFlags.Public);
            var table = (JbigHuffmanTable)property.GetValue(null);

            var actualDecodedValue = table.DecodeValueOrOob(reader);
            Assert.IsTrue(actualDecodedValue.IsOob);
        }
    }
}
