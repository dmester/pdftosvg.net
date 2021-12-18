// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Encodings
{
    internal class Utf16EncodingTests
    {
        // Examples from https://en.wikipedia.org/wiki/UTF-16#Examples
        [TestCase("\u0024", 0x0024u)]
        [TestCase("\u20AC", 0x20ACu)]
        [TestCase("\uD801\uDC37", 0x10437u)]
        [TestCase("\uD852\uDF62", 0x24B62u)]
        public void EncodeAndDecodeCodePoint(string utf16, uint codePoint)
        {
            Assert.AreEqual(utf16, Utf16Encoding.EncodeCodePoint(codePoint));
            Assert.AreEqual(codePoint, Utf16Encoding.DecodeCodePoint("padding" + utf16 + "padding", 7, out var length));
            Assert.AreEqual(utf16.Length, length);
        }

        // Private Use Area blocks:
        // https://en.wikipedia.org/wiki/Private_Use_Areas#Assignment
        //
        // UTF-16 encoded here:
        // https://unicode.scarfboy.com/
        //
        [TestCase(0, "\ue000")]
        [TestCase(6399, "\uf8ff")]
        [TestCase(6400, "\udb80\udc00")]
        [TestCase(71933, "\udbbf\udffd")]
        [TestCase(71934, "\udbc0\udc00")]
        [TestCase(137467, "\udbff\udffd")]
        public void GetPrivateUseChar(int offset, string expected)
        {
            Assert.AreEqual(expected, Utf16Encoding.GetPrivateUseChar(offset));
        }

        [TestCase(-1)]
        [TestCase(137468)]
        public void GetPrivateUseCharOutOfRange(int offset)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Utf16Encoding.GetPrivateUseChar(offset));
        }
    }
}
