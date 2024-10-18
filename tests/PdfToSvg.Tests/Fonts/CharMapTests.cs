// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.CMaps;
using PdfToSvg.Fonts;
using PdfToSvg.Fonts.WidthMaps;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

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

                // ToUnicode specifies control character
                new CharInfo { CharCode = 50, Unicode = "\u0000", GlyphIndex = 50 },
                new CharInfo { CharCode = 51, Unicode = "\u0009", GlyphIndex = 51 },
                new CharInfo { CharCode = 52, Unicode = "\u000a", GlyphIndex = 52 },
                new CharInfo { CharCode = 53, Unicode = "\u000d", GlyphIndex = 53 },
                new CharInfo { CharCode = 54, Unicode = "\u001f", GlyphIndex = 54 },
                new CharInfo { CharCode = 55, Unicode = "\u007f", GlyphIndex = 55 },

                // ToUnicode specifies unassigned Unicode char
                new CharInfo { CharCode = 60, Unicode = "\ufdd0", GlyphIndex = 60 },
                new CharInfo { CharCode = 61, Unicode = "\ufdef", GlyphIndex = 61 },
                new CharInfo { CharCode = 62, Unicode = "\ufffd", GlyphIndex = 62 },
                new CharInfo { CharCode = 63, Unicode = "\ufffe", GlyphIndex = 63 },
                new CharInfo { CharCode = 64, Unicode = "\uffff", GlyphIndex = 64 },
                new CharInfo { CharCode = 65, Unicode = "\ud87f\udffe", GlyphIndex = 65 },

                // ToUnicode specifies incomplete surrogate pair
                new CharInfo { CharCode = 70, Unicode = "\udbff", GlyphIndex = 70 }, // High surrogate
                new CharInfo { CharCode = 71, Unicode = "\udffd", GlyphIndex = 71 }, // Low surrogate
            };

            var unicodeMapData = new CMapData();
            unicodeMapData.BfChars.AddRange(unicodeChars);

            var unicodeMap = UnicodeMap.Create(unicodeMapData);

            var clonedChars = chars.Select(x => x.Clone());

            optimizedForEmbeddedFont.TryPopulate(() => clonedChars, unicodeMap, null, new EmptyWidthMap(), optimizeForEmbeddedFont: true);
            optimizedForTextExtract.TryPopulate(() => clonedChars, unicodeMap, null, new EmptyWidthMap(), optimizeForEmbeddedFont: false);
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
        [TestCase(50, "\ue005", 50)]
        [TestCase(51, "\ue006", 51)]
        [TestCase(52, "\ue007", 52)]
        [TestCase(53, "\ue008", 53)]
        [TestCase(54, "\ue009", 54)]
        [TestCase(55, "\ue00a", 55)]
        [TestCase(60, "\ue00b", 60)]
        [TestCase(61, "\ue00c", 61)]
        [TestCase(62, "\ue00d", 62)]
        [TestCase(63, "\ue00e", 63)]
        [TestCase(64, "\ue00f", 64)]
        [TestCase(65, "\ue010", 65)]
        [TestCase(70, "\ue011", 70)]
        [TestCase(71, "\ue012", 71)]
        public void OptimizedForEmbeddedFont(int charCode, string expectedUnicode, int expectedGlyph)
        {
            Assert.IsTrue(optimizedForEmbeddedFont.TryGetChar((uint)charCode, out var ch), nameof(CharMap.TryGetChar));
            Assert.AreEqual(EscapeString(expectedUnicode), EscapeString(ch.Unicode), nameof(ch.Unicode));
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
        [TestCase(30, "u04AC", 30)]
        [TestCase(31, "ft", 31)]
        [TestCase(32, "fq", 32)]
        [TestCase(40, "ufffd", 40)]
        [TestCase(41, "ufffd", 41)]
        [TestCase(50, "u0000", 50)]
        [TestCase(51, "u0009", 51)]
        [TestCase(52, "u000a", 52)]
        [TestCase(53, "u000d", 53)]
        [TestCase(54, "u001f", 54)]
        [TestCase(55, "u007f", 55)]
        [TestCase(60, "ufdd0", 60)]
        [TestCase(61, "ufdef", 61)]
        [TestCase(62, "ufffd", 62)]
        [TestCase(63, "ufffe", 63)]
        [TestCase(64, "uffff", 64)]
        [TestCase(65, "\ud87f\udffe", 65)]
        [TestCase(70, "udbff", 70)]
        [TestCase(71, "udffd", 71)]
        public void OptimizedForTextExtract(int charCode, string expectedUnicode, int expectedGlyph)
        {
            // NUnit runner did not like some of the control chars
            if (Regex.IsMatch(expectedUnicode, "^u[0-9a-fA-F]{4}$"))
            {
                expectedUnicode = ((char)int.Parse(expectedUnicode.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString();
            }

            Assert.IsTrue(optimizedForTextExtract.TryGetChar((uint)charCode, out var ch), nameof(CharMap.TryGetChar));
            Assert.AreEqual(EscapeString(expectedUnicode), EscapeString(ch.Unicode), nameof(ch.Unicode));
            Assert.AreEqual(expectedGlyph, ch.GlyphIndex, nameof(ch.GlyphIndex));
        }

        [Test]
        public void PopulateOnlyOnce()
        {
            var map = new CharMap();
            map.TryPopulate(() => new[] { new CharInfo { CharCode = 1, Unicode = "a" } }, UnicodeMap.Empty, null, new EmptyWidthMap(), false);
            map.TryPopulate(() => new[] { new CharInfo { CharCode = 2, Unicode = "b" } }, UnicodeMap.Empty, null, new EmptyWidthMap(), false);

            Assert.IsTrue(map.TryGetChar(1, out _));
            Assert.IsFalse(map.TryGetChar(2, out _));
        }

        [Test]
        public void DisallowConcurrentPopulationToPreventDeadlocks()
        {
            using var startEvent = new ManualResetEventSlim();
            using var stopEvent = new ManualResetEventSlim();

            var map = new CharMap();

            var thread = new Thread(_ =>
            {
                map.TryPopulate(() =>
                {
                    startEvent.Set();
                    stopEvent.Wait(5000);
                    return Enumerable.Empty<CharInfo>();
                }, UnicodeMap.Empty, null, new EmptyWidthMap(), false);
            });

            thread.IsBackground = true;
            thread.Start();

            Assert.IsTrue(startEvent.Wait(5000), "Thread did not start");
            Assert.IsFalse(map.TryPopulate(() => new[] { new CharInfo { CharCode = 2, Unicode = "b" } }, UnicodeMap.Empty, null, new EmptyWidthMap(), false));

            stopEvent.Set();
            thread.Join();
        }

        private static string EscapeString(string s)
        {
            return string.Join(" ", s.Select(c => "U+" + ((int)c).ToString("x4")));
        }
    }
}
