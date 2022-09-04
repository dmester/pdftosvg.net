// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Encodings
{
    internal class SingleByteEncodingTests
    {
        private static object[] EmptyTestCases = new[]
        {
            new object[] { "", new byte[] { } },
        };

        private static object[] DuplexTestCases = new[]
        {
            new object[] { "ABCDE", new byte[] { 65, 66, 67 } },
            new object[] { "ABABAB", new byte[] { 65, 66, 65, 66, 65, 66 } },
            new object[] { "CDE", new byte[] { 67 } },
        };

        private static object[] CharToByteTestCases = new[]
        {
            new object[] { "CD", new byte[] { 100, 100 } },
            new object[] { "xx", new byte[] { 100, 100 } },
        };

        private static object[] ByteToCharTestCases = new[]
        {
            new object[] { "\ufffd", new byte[] { 99 } },
        };

        private static SingleByteEncoding encoding;

        static SingleByteEncodingTests()
        {
            var toUnicode = new string[256];
            var toGlyphName = new string[256];

            toUnicode[100] = "?";
            toUnicode[65] = "A";
            toUnicode[66] = "B";
            toUnicode[67] = "CDE";

            encoding = new SingleByteEncoding(toUnicode, toGlyphName);
        }

        [TestCaseSource(nameof(EmptyTestCases))]
        [TestCaseSource(nameof(DuplexTestCases))]
        [TestCaseSource(nameof(CharToByteTestCases))]
        public void GetBytes(string str, byte[] bytes)
        {
            Assert.AreEqual(bytes, encoding.GetBytes(str));
        }

        [TestCaseSource(nameof(EmptyTestCases))]
        [TestCaseSource(nameof(DuplexTestCases))]
        [TestCaseSource(nameof(CharToByteTestCases))]
        public void GetByteCount(string str, byte[] bytes)
        {
            Assert.AreEqual(bytes.Length, encoding.GetByteCount(str));
        }

        [TestCaseSource(nameof(EmptyTestCases))]
        [TestCaseSource(nameof(DuplexTestCases))]
        [TestCaseSource(nameof(CharToByteTestCases))]
        public void GetCharCount(string str, byte[] bytes)
        {
            Assert.AreEqual(str.Length, encoding.GetCharCount(bytes));
        }

        [TestCaseSource(nameof(EmptyTestCases))]
        [TestCaseSource(nameof(DuplexTestCases))]
        [TestCaseSource(nameof(ByteToCharTestCases))]
        public void GetString(string str, byte[] bytes)
        {
            Assert.AreEqual(str, encoding.GetString(bytes));
        }

        [TestCaseSource(nameof(DuplexTestCases))]
        [TestCaseSource(nameof(ByteToCharTestCases))]
        public void TooShortCharArray(string str, byte[] bytes)
        {
            Assert.Throws<ArgumentException>(() => encoding.GetChars(bytes, 0, bytes.Length, new char[str.Length], 1));
            Assert.Throws<ArgumentException>(() => encoding.GetChars(bytes, 0, bytes.Length, new char[str.Length - 1], 0));
            encoding.GetChars(bytes, 0, bytes.Length, new char[str.Length], 0);
        }

        [TestCaseSource(nameof(DuplexTestCases))]
        [TestCaseSource(nameof(CharToByteTestCases))]
        public void TooShortByteArray(string str, byte[] bytes)
        {
            var chars = str.ToCharArray();
            Assert.Throws<ArgumentException>(() => encoding.GetBytes(chars, 0, chars.Length, new byte[bytes.Length], 1));
            Assert.Throws<ArgumentException>(() => encoding.GetBytes(chars, 0, chars.Length, new byte[bytes.Length - 1], 0));
            encoding.GetBytes(chars, 0, chars.Length, new byte[bytes.Length], 0);
        }
    }
}
