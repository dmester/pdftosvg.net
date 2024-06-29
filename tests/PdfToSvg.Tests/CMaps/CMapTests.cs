// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.CMaps;
using PdfToSvg.DocumentModel;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.CMaps
{
    internal class CMapTests
    {
        private const string sourceCMap = @"

/Parent-CMap usecmap

/CMapVersion 1.000 def

4 begincodespacerange
<01> <09>
<AA22> <AB33>
<BB1122> <BB2233>
<CC000000> <CC8899AA>
endcodespacerange

4 beginbfchar
<01> <0041>
<AA01> <0042>
<BB1122> <0043>
<CC000000> <0044>
endbfchar

6 beginbfrange
<02> <03> <0045>
<AA02> <AA03> <0045>
<BB1123> <BB1124> <0046>
<CC000001> <CC001000> <0048>
<CC001001> <CC002000> <0048>
<04> <05> [<00410042> <00430044>]
<06> <07> [<00450046>]
<08> <08> [<00470048> <0049004A>]
endbfrange

2 beginnotdefrange
<BB1125> <BB1128> 1
<CC000004> <CC000008> 2
<CC030000> <CC031000> 3
endnotdefrange

2 beginnotdefchar
<BB1129> 3
<CC000009> 4
endnotdefchar

3 begincidchar
<01> 65000
<AA01> 65001
<BB1122> 65002
<CC000000> 65003
endcidchar

1 begincidrange
<02> <03> 1
<AA02> <AA03> 2
<BB1123> <BB1124> 3
<CC001001> <CC002000> 4
<CC002001> <CC003000> 4
<04> <05> 5
<06> <08> 16412
endcidrange

";

        private CMapData cmapData;
        private CMap cmap;
        private UnicodeMap unicodeMap;

        [SetUp]
        public void Initialize()
        {
            cmapData = CMapParser.Parse(new MemoryStream(Encoding.ASCII.GetBytes(sourceCMap)), default);
            cmap = CMap.Create(cmapData);
            unicodeMap = UnicodeMap.Create(cmapData);
        }

        private static uint ParseCharCode(string charCode)
        {
            charCode = charCode.Trim('<', '>');
            return uint.Parse(charCode, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        // Bf chars
        [TestCase("<01>", "\u0041")]
        [TestCase("<AA01>", "\u0042")]
        [TestCase("<BB1122>", "\u0043")]
        [TestCase("<CC000000>", "\u0044")]

        // Bf ranges
        [TestCase("<02>", "\u0045")]
        [TestCase("<03>", "\u0046")]
        [TestCase("<AA02>", "\u0045")]
        [TestCase("<AA03>", "\u0046")]
        [TestCase("<BB1123>", "\u0046")]
        [TestCase("<BB1124>", "\u0047")]
        [TestCase("<BB1125>", null)]
        [TestCase("<BB1126>", null)]
        [TestCase("<CC000001>", "\u0048")]
        [TestCase("<CC000002>", "\u0049")]
        [TestCase("<CC000003>", "\u004A")]
        [TestCase("<CC001000>", "\u1047")]
        [TestCase("<CC001001>", "\u0048")]
        [TestCase("<CC001002>", "\u0049")]
        [TestCase("<CC001003>", "\u004A")]
        [TestCase("<CC002000>", "\u1047")]
        [TestCase("<CC002001>", null)]
        [TestCase("<04>", "\u0041\u0042")]
        [TestCase("<05>", "\u0043\u0044")]
        [TestCase("<06>", "\u0045\u0046")]
        [TestCase("<07>", null)]
        [TestCase("<08>", "\u0047\u0048")]
        [TestCase("<09>", null)]

        public void QueryUnicode(string charCode, string expectedUnicode)
        {
            var unicode = unicodeMap.GetUnicode(ParseCharCode(charCode));
            Assert.AreEqual(expectedUnicode, unicode);
        }

        // Not def ranges
        [TestCase("<BB1125>", 1)]
        [TestCase("<BB1128>", 1)]
        [TestCase("<00BB1128>", 1)]
        [TestCase("<BB112A>", null)]
        [TestCase("<CC000004>", 2)]
        [TestCase("<CC000008>", 2)]
        [TestCase("<CC02AAAA>", null)]
        [TestCase("<CC030000>", 3)]
        [TestCase("<CC031000>", 3)]
        [TestCase("<CC031001>", null)]

        // Not def chars
        [TestCase("<BB1129>", 3)]
        [TestCase("<CC000009>", 4)]
        [TestCase("<CC0000AA>", null)]
        public void GetNotDef(string charCode, int? expectedCid)
        {
            var parsedCharCode = ParseCharCode(charCode);
            var cid = cmap.GetNotDef(parsedCharCode);
            Assert.AreEqual((uint?)expectedCid, cid);
        }

        // Ranges
        [TestCase("<02>", 1)]
        [TestCase("<03>", 2)]
        [TestCase("<0003>", 2)]
        [TestCase("<AA02>", 2)]
        [TestCase("<AA03>", 3)]
        [TestCase("<00AA03>", 3)]
        [TestCase("<BB1123>", 3)]
        [TestCase("<BB1124>", 4)]
        [TestCase("<CC001001>", 4)]
        [TestCase("<CC001002>", 5)]
        [TestCase("<CC002000>", 0x1003)]
        [TestCase("<CC002001>", 4)]
        [TestCase("<CC002002>", 5)]
        [TestCase("<CC003000>", 0x1003)]
        [TestCase("<04>", 5)]
        [TestCase("<05>", 6)]
        [TestCase("<06>", 16412)]
        [TestCase("<07>", 16413)]
        [TestCase("<08>", 16414)]

        // Chars
        [TestCase("<01>", 65000)]
        [TestCase("<AA01>", 65001)]
        [TestCase("<BB1122>", 65002)]
        [TestCase("<CC000000>", 65003)]
        public void GetCid(string charCode, int? expectedCid)
        {
            var cid = cmap.GetCid(ParseCharCode(charCode));
            Assert.AreEqual((uint?)expectedCid, cid);
        }

        [TestCase(65002u, 0xBB1122u)]
        [TestCase(5u, 0x04u, 0xCC001002u, 0xCC002002u)]
        public void GetCharCodes(uint cid, params uint[] expectedCharCodes)
        {
            var charCodes = cmap.GetCharCodes(cid).OrderBy(x => x);
            Assert.AreEqual(expectedCharCodes.OrderBy(x => x), charCodes);
        }

        [TestCase("<04>", 0x04u)]
        [TestCase("<09>", 0x09u)]
        [TestCase("<AA22>", 0xAA22u)]
        [TestCase("<AA33>", 0xAA33u)]
        [TestCase("<AA44>", null)]
        [TestCase("<AB11>", null)]
        [TestCase("<BB1122>", 0xBB1122u)]
        [TestCase("<BB2233>", 0xBB2233u)]
        [TestCase("<BB1199>", null)]
        [TestCase("<CC000000>", 0xCC000000u)]
        [TestCase("<CC8899AA>", 0xCC8899AAu)]
        [TestCase("<CC0000CC>", null)]
        [TestCase("<CC00AA00>", null)]
        [TestCase("<CC990000>", null)]
        public void GetCharCode(string hexString, uint? expectedCharCode)
        {
            var binary = Hex.Decode(hexString);

            var code = cmap.GetCharCode(new PdfString(binary), 0);
            Assert.AreEqual(expectedCharCode ?? 0, code.CharCode);
            Assert.AreEqual(expectedCharCode == null ? 0 : (hexString.Length - 2) / 2, code.CharCodeLength);
        }

        [TestCase("Identity-V", "1234", 2, 0x1234)]
        [TestCase("Identity-H", "1234", 2, 0x1234)]
        [TestCase("Identity-V", "ffff", 2, 0xffff)]
        [TestCase("Identity-H", "ffff", 2, 0xffff)]
        [TestCase("UniCNS-UCS2-H", "00a2", 2, 262)]
        [TestCase("UniCNS-UCS2-H", "00a3", 2, 263)]
        [TestCase("UniCNS-UCS2-H", "01d0", 2, 18813)]
        [TestCase("UniCNS-UCS2-H", "ffe4", 2, 14050)]
        [TestCase("UniCNS-UCS2-H", "2013", 2, 121)]
        [TestCase("UniCNS-UCS2-H", "ff5d", 2, 133)]
        [TestCase("UniCNS-UCS2-V", "00a2", 2, 262)]
        [TestCase("UniCNS-UCS2-V", "00a3", 2, 263)]
        [TestCase("UniCNS-UCS2-V", "01d0", 2, 18813)]
        [TestCase("UniCNS-UCS2-V", "ffe4", 2, 14050)]
        [TestCase("UniCNS-UCS2-V", "2013", 2, 120)]
        [TestCase("UniCNS-UCS2-V", "ff5d", 2, 135)]
        [TestCase("HKscs-B5-V", "c6e4", 2, 14097)]
        [TestCase("HKscs-B5-V", "89d9", 2, 17308)]
        [TestCase("HKscs-B5-V", "1a", 1, 1)]
        public void PredefinedCMap(string cmapName, string hexString, int expectedLength, int expectedCid)
        {
            var binary = Hex.Decode(hexString);

            var cmap = CMap.Create(cmapName, default);
            var code = cmap.GetCharCode(new PdfString(binary), 0);
            var cid = cmap.GetCid(code.CharCode) ?? cmap.GetNotDef(code.CharCode);
            var reverseCode = cmap.GetCharCodes(cid ?? 0);

            Assert.AreEqual(expectedLength, code.CharCodeLength, "Char code length");
            Assert.AreEqual(expectedCid, cid, "Cid");
            Assert.IsTrue(reverseCode.Contains(code.CharCode), "Reverse char code lookup");
        }

        [Test]
        public void Parse()
        {
            var expectedCodeSpaceRanges = new[]
            {
                new CMapCodeSpaceRange(0x01, 0x09, 1),
                new CMapCodeSpaceRange(0xAA22, 0xAB33, 2),
                new CMapCodeSpaceRange(0xBB1122, 0xBB2233, 3),
                new CMapCodeSpaceRange(0xCC000000, 0xCC8899AA, 4),
            };

            var expectedBfChars = new[]
            {
                new CMapChar(0x01, 1, "\u0041"),
                new CMapChar(0xAA01, 2, "\u0042"),
                new CMapChar(0xBB1122, 3, "\u0043"),
                new CMapChar(0xCC000000, 4, "\u0044"),

                new CMapChar(0x04, 1, "\u0041\u0042"),
                new CMapChar(0x05, 1, "\u0043\u0044"),
                new CMapChar(0x06, 1, "\u0045\u0046"),
                new CMapChar(0x08, 1, "\u0047\u0048"),
            };

            var expectedBfRanges = new[]
            {
                new CMapRange(0x02, 0x03, 1, 0x0045),
                new CMapRange(0xAA02, 0xAA03, 2, 0x0045),
                new CMapRange(0xBB1123, 0xBB1124, 3, 0x0046),
                new CMapRange(0xCC000001, 0xCC001000, 4, 0x0048),
                new CMapRange(0xCC001001, 0xCC002000, 4, 0x0048),
            };

            var expectedNotDefRanges = new[]
            {
                new CMapRange(0xBB1125, 0xBB1128, 3, 1),
                new CMapRange(0xCC000004, 0xCC000008, 4, 2),
                new CMapRange(0xCC030000, 0xCC031000, 4, 3),
            };

            var expectedNotDefChars = new[]
            {
                new CMapChar(0xBB1129, 3, 3),
                new CMapChar(0xCC000009, 4, 4),
            };

            var expectedCidChars = new[]
            {
                new CMapChar(0x01, 1, 65000),
                new CMapChar(0xAA01, 2, 65001),
                new CMapChar(0xBB1122, 3, 65002),
                new CMapChar(0xCC000000, 4, 65003),
            };

            var expectedCidRanges = new[]
            {
                new CMapRange(0x02, 0x03, 1, 1),
                new CMapRange(0xAA02, 0xAA03, 2, 2),
                new CMapRange(0xBB1123, 0xBB1124, 3, 3),
                new CMapRange(0xCC001001, 0xCC002000, 4, 4),
                new CMapRange(0xCC002001, 0xCC003000, 4, 4),
                new CMapRange(0x04, 0x05, 1, 5),
                new CMapRange(0x06, 0x08, 1, 16412),
            };

            Assert.AreEqual("Parent-CMap", cmapData.UseCMap);
            Assert.AreEqual("1.000", cmapData.Version);
            Assert.AreEqual(expectedCodeSpaceRanges, cmapData.CodeSpaceRanges);
            Assert.AreEqual(expectedBfChars, cmapData.BfChars);
            Assert.AreEqual(expectedBfRanges, cmapData.BfRanges);
            Assert.AreEqual(expectedNotDefRanges, cmapData.NotDefRanges);
            Assert.AreEqual(expectedNotDefChars, cmapData.NotDefChars);
            Assert.AreEqual(expectedCidChars, cmapData.CidChars);
            Assert.AreEqual(expectedCidRanges, cmapData.CidRanges);
        }
    }
}
