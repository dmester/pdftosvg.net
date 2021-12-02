// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Encodings
{
    public class CMapTests
    {
        [Test]
        public void Mixed()
        {
            var cmap = new CMap();

            cmap.AddBfChar(new PdfString(new byte[] { 4 }), PdfString.FromUnicode("abc"));
            cmap.AddBfChar(new PdfString(new byte[] { 5 }), PdfString.FromUnicode("def"));
            cmap.AddBfChar(new PdfString(new byte[] { 5, 9 }), PdfString.FromUnicode("DEF"));
            cmap.AddBfChar(new PdfString(new byte[] { 9, 4 }), PdfString.FromUnicode("94"));
            cmap.AddBfChar(new PdfString(new byte[] { 9, 5 }), PdfString.FromUnicode("95"));
            cmap.AddBfRange(new PdfString(new byte[] { 6 }), new PdfString(new byte[] { 8 }), PdfString.FromUnicode("ghi"));
            cmap.AddBfRange(new PdfString(new byte[] { 9, 6 }), new PdfString(new byte[] { 9, 15 }), PdfString.FromUnicode("A"));

            cmap.AddBfRange(new PdfString(new byte[] { 10 }), new PdfString(new byte[] { 12 }),
                new[] { PdfString.FromUnicode("QQQ"), PdfString.FromUnicode("WWW"), PdfString.FromUnicode("EEE") });

            Assert.AreEqual("abc", cmap.GetCharacter(new PdfString(new byte[] { 4, 0, 0 }), 0).DestinationString);
            Assert.AreEqual("def", cmap.GetCharacter(new PdfString(new byte[] { 5, 0, 0 }), 0).DestinationString);
            Assert.AreEqual("94", cmap.GetCharacter(new PdfString(new byte[] { 9, 4, 0 }), 0).DestinationString);
            Assert.AreEqual("95", cmap.GetCharacter(new PdfString(new byte[] { 9, 5, 0 }), 0).DestinationString);
            Assert.AreEqual("ghi", cmap.GetCharacter(new PdfString(new byte[] { 6, 7, 8 }), 0).DestinationString);
            Assert.AreEqual("ghj", cmap.GetCharacter(new PdfString(new byte[] { 7, 8, 0 }), 0).DestinationString);
            Assert.AreEqual("ghk", cmap.GetCharacter(new PdfString(new byte[] { 8, 9, 0 }), 0).DestinationString);
            Assert.AreEqual("B", cmap.GetCharacter(new PdfString(new byte[] { 9, 7, 0 }), 0).DestinationString);
            Assert.AreEqual(null, cmap.GetCharacter(new PdfString(new byte[] { 0, 1, 2 }), 0).DestinationString);
            Assert.AreEqual("C", cmap.GetCharacter(new PdfString(new byte[] { 9, 8 }), 0).DestinationString);
            Assert.AreEqual("QQQ", cmap.GetCharacter(new PdfString(new byte[] { 10 }), 0).DestinationString);
            Assert.AreEqual("WWW", cmap.GetCharacter(new PdfString(new byte[] { 11 }), 0).DestinationString);

            Assert.AreEqual(1, cmap.GetCharacter(new PdfString(new byte[] { 10, 12 }), 0).SourceLength);
            Assert.AreEqual(1, cmap.GetCharacter(new PdfString(new byte[] { 5, 9 }), 0).SourceLength);
            Assert.AreEqual(2, cmap.GetCharacter(new PdfString(new byte[] { 9, 8 }), 0).SourceLength);
            Assert.AreEqual(0, cmap.GetCharacter(new PdfString(new byte[] { 9, 16 }), 0).SourceLength);
        }

        [Test]
        public void ToLookup()
        {
            var cmap = new CMap();

            cmap.AddBfChar(new PdfString(new byte[] { 4 }), PdfString.FromUnicode("abc"));
            cmap.AddBfChar(new PdfString(new byte[] { 5 }), PdfString.FromUnicode("def"));
            cmap.AddBfChar(new PdfString(new byte[] { 2, 3 }), PdfString.FromUnicode("g"));
            cmap.AddBfChar(new PdfString(new byte[] { 2, 3, 4 }), PdfString.FromUnicode("h"));

            var lookup = cmap.ToLookup();
            var expectedLookup = new Dictionary<uint, string>
            {
                { 4, "abc" },
                { 5, "def" },
                { (2 << 8) | 3, "g" },
                { (2 << 16) | (3 << 8) | 4, "h" },
            };

            Assert.AreEqual(expectedLookup, lookup);
        }
    }
}
