// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using Newtonsoft.Json;
using NUnit.Framework;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Conversion;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Tables;
using PdfToSvg.Fonts.OpenType.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.OpenType.Conversion
{
    internal class ConversionUtilsTests
    {
        [Test]
        public void PostScriptName()
        {
            Assert.AreEqual(
                "Ab-cdefxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
                ConversionUtils.PostScriptName("Ab-c<d>[e](f)/%x\x01x\ubcdexxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"));
        }

        [TestCase(FontWidth.Medium, null)]
        [TestCase(FontWidth.Medium, "")]
        [TestCase(FontWidth.Condensed, "Arial-Condensed")]
        [TestCase(FontWidth.ExtraCondensed, "Arial-ExtraCondensed")]
        [TestCase(FontWidth.ExtraCondensed, "Arial-Extra-Condensed")]
        public void ParseFontWidth(FontWidth expected, string fontName)
        {
            Assert.AreEqual(expected, ConversionUtils.ParseFontWidth(fontName));
        }

        [TestCase("ABC", 0b1u, 0u, 0u, 0u)]
        [TestCase("AÅ", 0b11u, 0u, 0u, 0u)]
        [TestCase("\uFE70\u027A", 0b10000u, 0u, 0b1000u, 0u)]
        public void GetUnicodeRanges(string chars, uint expectedRange1, uint expectedRange2, uint expectedRange3, uint expectedRange4)
        {
            var actualRanges = ConversionUtils.GetUnicodeRanges(chars.ToCharArray().Select(x => (uint)x));
            Assert.AreEqual(new[] { expectedRange1, expectedRange2, expectedRange3, expectedRange4 }, actualRanges);
        }

        [TestCase(0, 0, 2000)]
        [TestCase(1000, 500, 2000)]
        [TestCase(2000, 1000, 2000)]
        public void ToFWord(short expected, int permille, int unitsPerEm)
        {
            Assert.AreEqual(expected, ConversionUtils.ToFWord(permille, unitsPerEm));
        }


    }
}
