// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.CMaps;
using PdfToSvg.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts
{
    internal class CharMapTests
    {
        private CharMap optimizedForEmbeddedFont = new();
        private CharMap optimizedForTextExtract = new();

        public CharMapTests()
        {
            var unicodeChars = new[]
            {
                new CMapChar(20, 1, "E"),
                new CMapChar(21, 1, "f"),
                new CMapChar(22, 1, "GG"),
                new CMapChar(23, 1, "st"),
                new CMapChar(24, 1, "tz"),
                new CMapChar(25, 1, "ĳ"),
                new CMapChar(26, 1, "K"),
            };

            var chars = new[]
            {
                // Cascading
                new CharInfo { CharCode = 1, Unicode = "a", GlyphIndex = 1 },
                new CharInfo { CharCode = 2, Unicode = "a", GlyphIndex = 1 },
                new CharInfo { CharCode = 2, Unicode = "b", GlyphIndex = 2 },

                // Already used Unicode, but another glyph
                new CharInfo { CharCode = 3, Unicode = "a", GlyphIndex = 3 },

                // Resolving to multiple Unicode chars
                new CharInfo { CharCode = 4, Unicode = "cc", GlyphIndex = 4 },

                // Surrogate pair
                new CharInfo { CharCode = 5, Unicode = "\uD801\uDC37", GlyphIndex = 5 },

                // Ligatures
                new CharInfo { CharCode = 10, Unicode = "ff", GlyphIndex = 10 },
                new CharInfo { CharCode = 11, Unicode = "fl", GlyphIndex = 11 },
                new CharInfo { CharCode = 12, Unicode = "ﬂ", GlyphIndex = 11 },
                new CharInfo { CharCode = 13, Unicode = "Ĳ", GlyphIndex = 13 },

                // ToUnicode
                new CharInfo { CharCode = 20, Unicode = "E", GlyphIndex = 20 },
                new CharInfo { CharCode = 21, Unicode = "F", GlyphIndex = 21 },
                new CharInfo { CharCode = 22, Unicode = "G", GlyphIndex = 22 },
                new CharInfo { CharCode = 23, Unicode = "H", GlyphIndex = 23 },
                new CharInfo { CharCode = 24, Unicode = "ꜩ", GlyphIndex = 24 },
                new CharInfo { CharCode = 25, Unicode = "X", GlyphIndex = 25 },
                new CharInfo { CharCode = 26, Unicode = null, GlyphIndex = 26 },

                // Only AGL
                new CharInfo { CharCode = 30, GlyphName = "Tedescendercyrillic", GlyphIndex = 30 },
                new CharInfo { CharCode = 31, GlyphName = "f_t", GlyphIndex = 31 },
                new CharInfo { CharCode = 32, GlyphName = "f_q", GlyphIndex = 32 },

                // No Unicode mapping
                new CharInfo { CharCode = 40, GlyphIndex = 40 },
                new CharInfo { CharCode = 41, GlyphIndex = 41 },
            };

            var unicodeMapData = new CMapData();
            unicodeMapData.BfChars.AddRange(unicodeChars);

            var unicodeMap = UnicodeMap.Create(unicodeMapData);

            var clonedChars = chars.Select(x => x.Clone());

            optimizedForEmbeddedFont.TryPopulate(() => clonedChars, unicodeMap, optimizeForEmbeddedFont: true);
            optimizedForTextExtract.TryPopulate(() => clonedChars, unicodeMap, optimizeForEmbeddedFont: false);
        }

        [TestCase(1, "a", 1)]
        [TestCase(2, "a", 1)]
        [TestCase(3, "\ue000", 3)]
        [TestCase(4, "\ue001", 4)]
        [TestCase(5, "\uD801\uDC37", 5)]
        [TestCase(10, "ﬀ", 10)]
        [TestCase(11, "ﬂ", 11)]
        [TestCase(12, "ﬂ", 11)]
        [TestCase(13, "Ĳ", 13)]
        [TestCase(20, "E", 20)]
        [TestCase(21, "f", 21)]
        [TestCase(22, "G", 22)]
        [TestCase(23, "H", 23)]
        [TestCase(24, "ꜩ", 24)]
        [TestCase(25, "ĳ", 25)]
        [TestCase(26, "K", 26)]
        [TestCase(30, "\u04AC", 30)]
        [TestCase(31, "ﬅ", 31)]
        [TestCase(32, "\ue002", 32)]
        [TestCase(40, "\ue003", 40)]
        [TestCase(41, "\ue004", 41)]
        public void OptimizedForEmbeddedFont(int charCode, string expectedUnicode, int expectedGlyph)
        {
            Assert.IsTrue(optimizedForEmbeddedFont.TryGetChar((uint)charCode, out var ch), nameof(CharMap.TryGetChar));
            Assert.AreEqual(expectedUnicode, ch.Unicode, nameof(ch.Unicode));
            Assert.AreEqual(expectedGlyph, ch.GlyphIndex, nameof(ch.GlyphIndex));
        }

        [TestCase(1, "a", 1)]
        [TestCase(2, "a", 1)]
        [TestCase(3, "a", 3)]
        [TestCase(4, "cc", 4)]
        [TestCase(5, "\uD801\uDC37", 5)]
        [TestCase(10, "ff", 10)]
        [TestCase(11, "fl", 11)]
        [TestCase(12, "ﬂ", 11)]
        [TestCase(13, "Ĳ", 13)]
        [TestCase(20, "E", 20)]
        [TestCase(21, "f", 21)]
        [TestCase(22, "GG", 22)]
        [TestCase(23, "st", 23)]
        [TestCase(24, "tz", 24)]
        [TestCase(25, "ĳ", 25)]
        [TestCase(26, "K", 26)]
        [TestCase(30, "\u04AC", 30)]
        [TestCase(31, "ft", 31)]
        [TestCase(32, "fq", 32)]
        [TestCase(40, "\ufffd", 40)]
        [TestCase(41, "\ufffd", 41)]
        public void OptimizedForTextExtract(int charCode, string expectedUnicode, int expectedGlyph)
        {
            Assert.IsTrue(optimizedForTextExtract.TryGetChar((uint)charCode, out var ch), nameof(CharMap.TryGetChar));
            Assert.AreEqual(expectedUnicode, ch.Unicode, nameof(ch.Unicode));
            Assert.AreEqual(expectedGlyph, ch.GlyphIndex, nameof(ch.GlyphIndex));
        }
    }
}
