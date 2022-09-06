// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using Newtonsoft.Json;
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
        private readonly OpenTypeCMap cmap = new OpenTypeCMap(0, 0, new[]
        {
            new OpenTypeCMapRange(0x0000, 0x00ff, 0x100),
            new OpenTypeCMapRange(0x10435, 0x1043a, 1),
            new OpenTypeCMapRange(0x24B62, 0x24B62, 10),
        });

        [Test]
        public void OptimizedRanges()
        {
            var ranges = new List<OpenTypeCMapRange>
            {
                // The following three will be grouped to a single range
                new OpenTypeCMapRange(0, 3, 300),
                new OpenTypeCMapRange(4, 7, 304),
                new OpenTypeCMapRange(8, 11, 308),

                // The following two should be grouped together
                new OpenTypeCMapRange(70, 72, 200),
                new OpenTypeCMapRange(73, 74, 500),

                new OpenTypeCMapRange(12, 50, 400),
                new OpenTypeCMapRange(60, 64, 208),
            };

            var actualGroups = OpenTypeCMapEncoder.GroupRanges(ranges);

            var expectedGroups = new[]
            {
                new []
                {
                    new OpenTypeCMapRange(0, 11, 300),
                },
                new []
                {
                    new OpenTypeCMapRange(12, 50, 400),
                },
                new []
                {
                    new OpenTypeCMapRange(60, 64, 208),
                },
                new []
                {
                    new OpenTypeCMapRange(70, 72, 200),
                    new OpenTypeCMapRange(73, 74, 500),
                },
            };

            Assert.AreEqual(
                JsonConvert.SerializeObject(expectedGroups),
                JsonConvert.SerializeObject(actualGroups));
        }

        private static object[][] charMappings = new[]
        {
            new object[]{ "$", 0x124u },
            new object[]{ "\uD801\uDC37", 3u },
            new object[]{ "\uD852\uDF62", 10u },
        };

        [TestCaseSource(nameof(charMappings))]
        public void ToUnicode(string unicode, uint glyphIndex)
        {
            Assert.AreEqual(unicode, cmap.ToUnicode(glyphIndex));
        }

        [TestCaseSource(nameof(charMappings))]
        public void ToGlyphIndex(string unicode, uint glyphIndex)
        {
            Assert.AreEqual(glyphIndex, cmap.ToGlyphIndex(unicode));
        }
    }
}
