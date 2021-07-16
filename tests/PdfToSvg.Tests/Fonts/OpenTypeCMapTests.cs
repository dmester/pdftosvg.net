// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.OpenType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts
{
    public class OpenTypeCMapTests
    {
        [Test]
        public void ToUnicode()
        {
            var ranges = new[]
            {
                new OpenTypeCMapRange(0x0000, 0x00ff, 0x100),
                new OpenTypeCMapRange(0x10435, 0x1043a, 1),
                new OpenTypeCMapRange(0x24B62, 0x24B62, 10),
            };
            var cmap = new OpenTypeCMap(0, 0, ranges);

            Assert.AreEqual("$", cmap.ToUnicode(0x124));
            Assert.AreEqual("\uD801\uDC37", cmap.ToUnicode(3));
            Assert.AreEqual("\uD852\uDF62", cmap.ToUnicode(10));
        }
    }
}
