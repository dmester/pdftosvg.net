// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using Newtonsoft.Json;
using NUnit.Framework;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Tables;
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
        public void CombineGlyphRanges()
        {
            var ranges = new List<OpenTypeCMapRange>
            {
                // The following three will be grouped to a single range
                new OpenTypeCMapRange(0, 3, 300),
                new OpenTypeCMapRange(4, 7, 304),
                new OpenTypeCMapRange(8, 11, 308),

                // Not combined
                new OpenTypeCMapRange(70, 72, 200),
                new OpenTypeCMapRange(73, 74, 500),
                new OpenTypeCMapRange(12, 50, 400),
                new OpenTypeCMapRange(60, 64, 208),
            };

            var actualGroups = OpenTypeCMapEncoder.CombineGlyphRanges(ranges);

            var expectedGroups = new[]
            {
                new OpenTypeCMapRange(0, 11, 300),
                new OpenTypeCMapRange(12, 50, 400),
                new OpenTypeCMapRange(60, 64, 208),
                new OpenTypeCMapRange(70, 72, 200),
                new OpenTypeCMapRange(73, 74, 500),
            };

            Assert.AreEqual(
                JsonConvert.SerializeObject(expectedGroups),
                JsonConvert.SerializeObject(actualGroups));
        }

        [Test]
        public void ToGlyphIndex_Format4Wrap()
        {
            var map = OpenTypeCMapDecoder.GetCMap(new CMapEncodingRecord
            {
                Content = new CMapFormat4
                {
                    StartCode = new ushort[] { 10 },
                    EndCode = new ushort[] { 20 },
                    IdDelta = new short[] { -15 },
                    IdRangeOffsets = new ushort[] { 0 },
                },
            });

            Assert.AreEqual(11, map.Chars.Count());
            Assert.AreEqual(65531u, map.ToGlyphIndex(10));
            Assert.AreEqual(65535u, map.ToGlyphIndex(14));
            Assert.AreEqual(0u, map.ToGlyphIndex(15));
            Assert.AreEqual(5u, map.ToGlyphIndex(20));
        }

        [Test]
        public void Chars_InvalidRanges()
        {
            var map = new OpenTypeCMap(0, 0, new[]
            {
                new OpenTypeCMapRange(10, 2, 1),
                new OpenTypeCMapRange(0, uint.MaxValue, 0),
                new OpenTypeCMapRange(0x10000, 0x10001, uint.MaxValue),
                new OpenTypeCMapRange(0xfffffff0, uint.MaxValue, 1),
            });

            Assert.AreEqual(65536, map.Chars.Count());
            Assert.AreEqual(65535u, map.ToGlyphIndex(65535));
            Assert.IsNull(map.ToGlyphIndex(65536));
            Assert.IsNull(map.ToUnicode(65536));
        }

        [Test]
        public void Chars_OverlappingRanges()
        {
            var ranges = Enumerable.Range(0, 10000).Select(i => new OpenTypeCMapRange(0, 65535, 0));
            var map = new OpenTypeCMap(0, 0, ranges);

            Assert.AreEqual(1, map.Ranges.Count);
            Assert.AreEqual(65536, map.Chars.Count());
        }

        [Test]
        public void Chars_ClippedGlyphs()
        {
            var map = new OpenTypeCMap(0, 0, new[] { new OpenTypeCMapRange(0x10fffe, uint.MaxValue, 65535) });

            Assert.AreEqual(1, map.Chars.Count());
            Assert.AreEqual(65535u, map.ToGlyphIndex(0x10fffe));
            Assert.IsNull(map.ToGlyphIndex(0x10ffff));
            Assert.AreEqual(char.ConvertFromUtf32(0x10fffe), map.ToUnicode(65535));
        }

        [Test]
        public void Chars_OverlappingSuffix()
        {
            var map = new OpenTypeCMap(0, 0, new[]
            {
                new OpenTypeCMapRange(10, 20, 100),
                new OpenTypeCMapRange(15, 25, 200),
            });

            Assert.AreEqual(16, map.Chars.Count());
            Assert.AreEqual(110u, map.ToGlyphIndex(20));
            Assert.AreEqual(206u, map.ToGlyphIndex(21));
            Assert.AreEqual(210u, map.ToGlyphIndex(25));
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
