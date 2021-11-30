// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.OpenType.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.OpenType.Utils
{
    internal class SearchParamsTests
    {
        [Test]
        public void TestParams()
        {
            // Test values from cmap table spec:
            // https://docs.microsoft.com/en-us/typography/opentype/spec/cmap#format-4-segment-mapping-to-delta-values
            var p = new SearchParams(39, 2);

            Assert.AreEqual(64, p.SearchRange, nameof(p.SearchRange));
            Assert.AreEqual(5, p.EntrySelector, nameof(p.EntrySelector));
            Assert.AreEqual(14, p.RangeShift, nameof(p.RangeShift));
        }
    }
}
