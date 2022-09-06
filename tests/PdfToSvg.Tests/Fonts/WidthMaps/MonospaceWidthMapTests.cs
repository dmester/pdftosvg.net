// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts;
using PdfToSvg.Fonts.WidthMaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.WidthMaps
{
    internal class MonospaceWidthMapTests
    {
        [TestCase("abc", 3)]
        [TestCase("", 0)]
        [TestCase("📃📃📃", 3)]
        [TestCase("你好你好", 4)]
        public void GetWidth(string unicode, int length)
        {
            var map = new MonospaceWidthMap(1);
            var width = map.GetWidth(new CharInfo { Unicode = unicode });

            Assert.AreEqual(length, (int)width);
        }
    }
}
