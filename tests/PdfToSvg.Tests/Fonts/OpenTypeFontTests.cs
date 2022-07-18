// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.OpenType;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts
{
    public class OpenTypeFontTests
    {
        private void AssertCMap(OpenTypeFont font, string expectedCMaps)
        {
            var formatted = new StringBuilder();
            foreach (var cmap in font.CMaps)
            {
                formatted.AppendLine();
                formatted.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", cmap.PlatformID, cmap.EncodingID));

                foreach (var range in cmap.Ranges.OrderBy(x => x.StartUnicode))
                {
                    formatted.AppendLine(range.ToString());
                }
            }

            Assert.AreEqual(expectedCMaps, formatted.ToString());
        }

        [Test]
        public void ParseFont()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fonts", "TestFiles", "non-symbol.ttf");
            var content = File.ReadAllBytes(path);
            var font = OpenTypeFont.Parse(content);

            Assert.AreEqual("Untitled1", font.Names.FontFamily);
            Assert.AreEqual("Regular", font.Names.FontSubfamily);
            Assert.AreEqual("Untitled1", font.Names.FullFontName);
            Assert.AreEqual("Copyright (c) 2021, Daniel", font.Names.Copyright);

            AssertCMap(font, @"
Unicode 3
65-67 <=> 3
90-90 <=> 6
65535-65535 <=> 0

Unicode 4
65-67 <=> 3
90-90 <=> 6
1114058-1114058 <=> 7

Macintosh 0
0-0 <=> 1
8-8 <=> 1
9-9 <=> 2
13-13 <=> 2
29-29 <=> 1
65-65 <=> 3
66-66 <=> 4
67-67 <=> 5
90-90 <=> 6

Windows 1
65-67 <=> 3
90-90 <=> 6
65535-65535 <=> 0

Windows 10
65-67 <=> 3
90-90 <=> 6
1114058-1114058 <=> 7
");
        }

        [Test]
        public void ParseSymbolFont()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fonts", "TestFiles", "symbol.ttf");
            var content = File.ReadAllBytes(path);
            var font = OpenTypeFont.Parse(content);

            Assert.AreEqual("Untitled1", font.Names.FontFamily);
            Assert.AreEqual("Regular", font.Names.FontSubfamily);
            Assert.AreEqual("Untitled1", font.Names.FullFontName);
            Assert.AreEqual("Copyright (c) 2021, Daniel", font.Names.Copyright);

            AssertCMap(font, @"
Macintosh 0
0-0 <=> 1
8-8 <=> 1
9-9 <=> 2
13-13 <=> 2
29-29 <=> 1
65-65 <=> 3
66-66 <=> 4
67-67 <=> 5
90-90 <=> 6

Windows 0
61505-61507 <=> 3
61530-61530 <=> 6
65535-65535 <=> 0
");
        }
    }
}
