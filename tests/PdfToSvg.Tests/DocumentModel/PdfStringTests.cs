// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.DocumentModel
{
    class PdfStringTests
    {
        [Test]
        public void PDFDocString()
        {
            var str = new PdfString(new byte[] { 0x8e, 0x9c });
            Assert.AreEqual("\u201d\u0153", str.ToString());
        }

        [Test]
        public void UnicodeString()
        {
            var str = new PdfString(new byte[] { 0xfe, 0xff, 0x12, 0x34, 0x41, 0x23, 0x02, 0x04 });
            Assert.AreEqual("\u1234\u4123\u0204", str.ToString());
        }
    }
}
