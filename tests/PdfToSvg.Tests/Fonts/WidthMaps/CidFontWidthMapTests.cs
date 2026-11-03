// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Fonts;
using PdfToSvg.Fonts.WidthMaps;
using System;
using System.Collections.Generic;

namespace PdfToSvg.Tests.Fonts.WidthMaps
{
    public class CidFontWidthMapTests
    {
        private static CidFontWidthMap Parse(params object[] widths)
        {
            return CidFontWidthMap.Parse(new PdfDictionary
            {
                { Names.DescendantFonts, new object[] { new PdfDictionary
                {
                    { Names.DW, 400 },
                    { Names.W, widths },
                } } },
            });
        }

        [Test]
        public void Parse_InvalidRanges()
        {
            var map = Parse(0, -1, 500, -1, -1, 600.5, 5, 3, 700, 8, new object[] { 800 });

            Assert.AreEqual(0.4, map.GetWidth(new CharInfo { Cid = 0 }));
            Assert.AreEqual(0.4, map.GetWidth(new CharInfo { Cid = uint.MaxValue }));
            Assert.AreEqual(0.8, map.GetWidth(new CharInfo { Cid = 8 }));
            Assert.AreEqual(0.4, map.GetWidth(new CharInfo()));
        }

        [Test]
        public void Parse_ClippedRanges()
        {
            var map = Parse(int.MinValue, int.MaxValue, 600, 65534, new object[] { 700, 800.0, 900 });

            Assert.AreEqual(0.6, map.GetWidth(new CharInfo { Cid = 0 }));
            Assert.AreEqual(0.6, map.GetWidth(new CharInfo { Cid = 65533 }));
            Assert.AreEqual(0.7, map.GetWidth(new CharInfo { Cid = 65534 }), 0.000001);
            Assert.AreEqual(0.8, map.GetWidth(new CharInfo { Cid = 65535 }));
            Assert.AreEqual(0.4, map.GetWidth(new CharInfo { Cid = 65536 }));
        }

        [Test]
        public void Parse_OverlappingRanges()
        {
            var map = Parse(0, 100, 500, 10, new object[] { 600, 700 }, 11, 20, 800.0, 15, new object[] { 900 });

            Assert.AreEqual(0.5, map.GetWidth(new CharInfo { Cid = 9 }));
            Assert.AreEqual(0.6, map.GetWidth(new CharInfo { Cid = 10 }));
            Assert.AreEqual(0.8, map.GetWidth(new CharInfo { Cid = 11 }));
            Assert.AreEqual(0.9, map.GetWidth(new CharInfo { Cid = 15 }));
            Assert.AreEqual(0.8, map.GetWidth(new CharInfo { Cid = 20 }));
            Assert.AreEqual(0.5, map.GetWidth(new CharInfo { Cid = 21 }));
        }

        [Test]
        public void Parse_RepeatedArrays()
        {
            var entries = new List<object>();
            var widths = new object[65536];
            for (var i = 0; i < widths.Length; i++)
            {
                widths[i] = 500;
            }
            for (var i = 0; i < 20; i++)
            {
                entries.Add(0);
                entries.Add(widths);
            }
            entries.Add(0);
            entries.Add(0);
            entries.Add(999);

            var map = Parse(entries.ToArray());
            Assert.AreEqual(0.5, map.GetWidth(new CharInfo { Cid = 0 }));
            Assert.AreEqual(0.5, map.GetWidth(new CharInfo { Cid = 65535 }));
        }

        [Test]
        public void Parse_RepeatedFullRanges()
        {
            var widths = new List<object>();
            for (var i = 0; i < 10000; i++)
            {
                widths.Add(0);
                widths.Add(int.MaxValue);
                widths.Add(i);
            }

            var map = Parse(widths.ToArray());
            // One million assignments retain fifteen complete ranges and part of the sixteenth.
            Assert.AreEqual(0.015, map.GetWidth(new CharInfo { Cid = 0 }), 0.000001);
            Assert.AreEqual(0.014, map.GetWidth(new CharInfo { Cid = 65535 }), 0.000001);
        }
    }
}
