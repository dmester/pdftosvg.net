// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using PdfToSvg.Fonts.Type1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.Type1
{
    internal class Type1DecryptorTests
    {
        [Test]
        public void DecodeAscii_NotHex()
        {
            var random = new Random(0);
            var originalBytes = new byte[1024];
            random.NextBytes(originalBytes);

            var resultBytes = (byte[])originalBytes.Clone();
            var newLength = Type1Decryptor.DecodeAscii(resultBytes, 0, resultBytes.Length);

            Assert.AreEqual(originalBytes.Length, newLength);
            Assert.AreEqual(originalBytes, resultBytes);
        }

        [Test]
        public void DecodeAscii_Hex()
        {
            var bytes = Encoding.ASCII.GetBytes("  \n   \t  0   f ab cdef0122345678  ");
            var expectedBytes = new byte[] { 0x0f, 0xab, 0xcd, 0xef, 0x01, 0x22, 0x34, 0x56, 0x78 };
            
            var newLength = Type1Decryptor.DecodeAscii(bytes, 0, bytes.Length);
            var resultBytes = bytes.Slice(0, newLength);

            Assert.AreEqual(expectedBytes, resultBytes);
        }
    }
}
