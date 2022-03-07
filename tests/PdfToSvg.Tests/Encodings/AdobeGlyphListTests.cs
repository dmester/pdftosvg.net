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
    public class AdobeGlyphListTests
    {
        [TestCase("u20AC", "\u20AC")]

        // Examples from:
        // https://github.com/adobe-type-tools/agl-specification#3-examples
        [TestCase("Lcommaaccent", "\u013B")]
        [TestCase("uni20AC0308", "\u20AC\u0308")]
        [TestCase("u1040C", "\uD801\uDC0C")]
        [TestCase("uniD801DC0C", null)]
        [TestCase("uni20ac", null)]
        [TestCase("Lcommaaccent_uni20AC0308_u1040C.alternate", "\u013B\u20AC\u0308\uD801\uDC0C")]

        // More tests
        [TestCase("u000", null)]
        [TestCase("u0000", "\u0000")]
        [TestCase("uD7FF", "\uD7FF")]
        [TestCase("uD800", null)]
        [TestCase("uDFFF", null)]
        [TestCase("uE000", "\uE000")]
        [TestCase("u10FFFF", "\u10FFFF")]
        [TestCase("u110000", null)]
        [TestCase("u010FFFF", null)]

        // Symbols should only be mapped when Zapf Dingbats is used, but we will always map them,
        // since an invalid char will cause garbage output anyways.
        [TestCase("a10", "\u2721")]
        [TestCase("a169", "\u279E")]

        public void Map(string source, string expectedResult)
        {
            Assert.AreEqual(expectedResult != null, AdobeGlyphList.TryMap(source, out var actualResult));
            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}
