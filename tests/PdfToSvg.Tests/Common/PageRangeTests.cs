// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Common
{
    public class PageRangeTests
    {
        [TestCase("1234", "1234,1234")]
        [TestCase("1,2,3,4", "1,1", "2,2", "3,3", "4,4")]
        [TestCase("1,2..4,3,4..,", "1,1", "2,4", "3,3", "4,-1")]
        [TestCase("1..1", "1,1")]
        [TestCase("1..", "1,-1")]
        [TestCase("..1", "-1,1")]
        [TestCase(",,,1..2,,,", "1,2")]
        public void ParseCorrectRange(string source, params string[] expectedRanges)
        {
            Assert.IsTrue(PageRange.TryParse(source, out var parsedRanges), "PageRange.TryParse return value");

            var formattedParsedRanges = parsedRanges
                .Select(x => string.Format(CultureInfo.InvariantCulture, "{0},{1}", x.From, x.To))
                .ToList();

            Assert.AreEqual(expectedRanges, formattedParsedRanges);
        }

        [TestCase("")]
        [TestCase(",,,,")]
        [TestCase("   ")]
        [TestCase("111111111111111111111111")]
        [TestCase("abc")]
        [TestCase("..")]
        [TestCase("-1..2")]
        [TestCase("1,2,3,abc")]
        public void ParseIncorrectRange(string source)
        {
            Assert.IsFalse(PageRange.TryParse(source, out var _), "PageRange.TryParse return value");
        }

        [TestCase(1, 2, "1..2")]
        [TestCase(2, 2, "2")]
        [TestCase(-1, 2, "..2")]
        [TestCase(1, -1, "1..")]
        public void FormatRange(int from, int to, string expectedFormat)
        {
            Assert.AreEqual(expectedFormat, new PageRange(from, to).ToString());
        }

        [TestCase("1,2,3", 1, 2, 3)]
        [TestCase("..1", 1)]
        [TestCase("..0")]
        [TestCase("7.., 8, 2", 2, 7, 8, 9, 10)]
        [TestCase("1..5, 4..6", 1, 2, 3, 4, 5, 6)]
        public void EnumeratePages(string pageString, params int[] pages)
        {
            Assert.IsTrue(PageRange.TryParse(pageString, out var ranges));
            Assert.AreEqual(pages, ranges.Pages(10));
        }
    }
}
