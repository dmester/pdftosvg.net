// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests
{
    public class StandardFontResolverTests
    {
        // This list is compiled from some random real life pdfs.
        [TestCase("Arial", "Arial,sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Arial,Bold", "Arial,sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Arial,BoldItalic", "Arial,sans-serif", FontWeight.Bold, FontStyle.Italic)]
        [TestCase("Arial-BoldItalicMT", "Arial,sans-serif", FontWeight.Bold, FontStyle.Italic)]
        [TestCase("Arial-BoldMT", "Arial,sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Arial-ItalicMT", "Arial,sans-serif", FontWeight.Regular, FontStyle.Italic)]
        [TestCase("ArialMT", "Arial,sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("ArialNarrow", "Arial,sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("ArialUnicodeMS", "Arial,sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("BookAntiqua", "serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("BookAntiqua,Bold", "serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("BookAntiqua,Italic", "serif", FontWeight.Regular, FontStyle.Italic)]
        [TestCase("BookAntiqua-BoldItalic", "serif", FontWeight.Bold, FontStyle.Italic)]
        [TestCase("Calibri,Light", "Calibri,sans-serif", FontWeight.Light, FontStyle.Normal)]
        [TestCase("Calibri-Light", "Calibri,sans-serif", FontWeight.Light, FontStyle.Normal)]
        [TestCase("Calibri,Bold", "Calibri,sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Calibri,Italic", "Calibri,sans-serif", FontWeight.Regular, FontStyle.Italic)]
        [TestCase("Calibri-Bold", "Calibri,sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Calibri-Italic", "Calibri,sans-serif", FontWeight.Regular, FontStyle.Italic)]
        [TestCase("Consolas", "Consolas,monospace", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Consolas,Bold", "Consolas,monospace", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Consolas,BoldItalic", "Consolas,monospace", FontWeight.Bold, FontStyle.Italic)]
        [TestCase("Consolas-Bold", "Consolas,monospace", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Consolas-Italic", "Consolas,monospace", FontWeight.Regular, FontStyle.Italic)]
        [TestCase("Courier", "'Courier New',Courier,monospace", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Courier-Bold", "'Courier New',Courier,monospace", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("CourierNewPS-BoldMT", "'Courier New',Courier,monospace", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("CourierNewPSMT", "'Courier New',Courier,monospace", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Helvetica", "Helvetica,Arial,sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Helvetica-Bold", "Helvetica,Arial,sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Helvetica-BoldOblique", "Helvetica,Arial,sans-serif", FontWeight.Bold, FontStyle.Oblique)]
        [TestCase("HelveticaNeue", "Helvetica,Arial,sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("HelveticaNeue-Bold", "Helvetica,Arial,sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("HelveticaNeue-BoldItalic", "Helvetica,Arial,sans-serif", FontWeight.Bold, FontStyle.Italic)]
        [TestCase("HelveticaNeue-Light", "Helvetica,Arial,sans-serif", FontWeight.Light, FontStyle.Normal)]
        [TestCase("HelveticaNeueLTPro-BdCn", "Helvetica,Arial,sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("HelveticaNeueLTStd-BdCn", "Helvetica,Arial,sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("HelveticaNeueLTStd-Cn", "Helvetica,Arial,sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("HelveticaNeueLTStd-LtCn", "Helvetica,Arial,sans-serif", FontWeight.Light, FontStyle.Normal)]
        [TestCase("HelveticaNeueLTStd-MdCn", "Helvetica,Arial,sans-serif", FontWeight.Medium, FontStyle.Normal)]
        [TestCase("Helvetica-Oblique", "Helvetica,Arial,sans-serif", FontWeight.Regular, FontStyle.Oblique)]
        [TestCase("Impact", "Impact,fantasy", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Meiryo", "sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("MeiryoUI", "sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("MeiryoUI-Bold", "sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("MS-Gothic", "'MS Gothic',sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("MS-PGothic", "'MS Gothic',sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Myriad-Bold", "sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("MyriadPro-Black", "sans-serif", FontWeight.Black, FontStyle.Normal)]
        [TestCase("MyriadPro-BlackIt", "sans-serif", FontWeight.Black, FontStyle.Italic)]
        [TestCase("MyriadPro-Bold", "sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("MyriadPro-BoldCond", "sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("MyriadPro-BoldSemiCn", "sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("MyriadPro-Cond", "sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("MyriadPro-It", "sans-serif", FontWeight.Regular, FontStyle.Italic)]
        [TestCase("MyriadPro-Regular", "sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("MyriadPro-Semibold", "sans-serif", FontWeight.SemiBold, FontStyle.Normal)]
        [TestCase("MyriadPro-SemiboldCond", "sans-serif", FontWeight.SemiBold, FontStyle.Normal)]
        [TestCase("Myriad-Roman", "serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("PalatinoLinotype-Bold", "'Palatino Linotype',Palatino,serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("PalatinoLinotype-BoldItalic", "'Palatino Linotype',Palatino,serif", FontWeight.Bold, FontStyle.Italic)]
        [TestCase("PalatinoLinotype-Roman", "'Palatino Linotype',Palatino,serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("SegoeUI", "'Segoe UI',sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("SegoeUI-Bold", "'Segoe UI',sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("SourceSansPro-BlackIt", "sans-serif", FontWeight.Black, FontStyle.Italic)]
        [TestCase("SourceSansPro-BoldIt", "sans-serif", FontWeight.Bold, FontStyle.Italic)]
        [TestCase("SourceSansPro-ExtraLight", "sans-serif", FontWeight.ExtraLight, FontStyle.Normal)]
        [TestCase("SourceSansPro-ExtraLightIt", "sans-serif", FontWeight.ExtraLight, FontStyle.Italic)]
        [TestCase("SourceSansPro-It", "sans-serif", FontWeight.Regular, FontStyle.Italic)]
        [TestCase("SourceSansPro-LightIt", "sans-serif", FontWeight.Light, FontStyle.Italic)]
        [TestCase("SourceSansPro-Regular", "sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("SourceSansPro-SemiboldIt", "sans-serif", FontWeight.SemiBold, FontStyle.Italic)]
        [TestCase("Tahoma", "Tahoma,sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Tahoma-Bold", "Tahoma,sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Times New Roman", "'Times New Roman',serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Times New Roman,Bold", "'Times New Roman',serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Times New Roman,Italic", "'Times New Roman',serif", FontWeight.Regular, FontStyle.Italic)]
        [TestCase("Times-Bold", "'Times New Roman',serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Times-BoldItalic", "'Times New Roman',serif", FontWeight.Bold, FontStyle.Italic)]
        [TestCase("Times-Italic", "'Times New Roman',serif", FontWeight.Regular, FontStyle.Italic)]
        [TestCase("TimesNewRoman", "'Times New Roman',serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("TimesNewRoman,Bold", "'Times New Roman',serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("TimesNewRomanPS-BoldItalicMT", "'Times New Roman',serif", FontWeight.Bold, FontStyle.Italic)]
        [TestCase("TimesNewRomanPS-BoldMT", "'Times New Roman',serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("TimesNewRomanPS-ItalicMT", "'Times New Roman',serif", FontWeight.Regular, FontStyle.Italic)]
        [TestCase("TimesNewRomanPSMT", "'Times New Roman',serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Times-Roman", "'Times New Roman',serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Verdana", "Verdana,sans-serif", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Verdana-Bold", "Verdana,sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("Wingdings", "Wingdings", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("Wingdings-Regular", "Wingdings", FontWeight.Regular, FontStyle.Normal)]
        [TestCase("YuGothic-Bold", "'Yu Gothic',sans-serif", FontWeight.Bold, FontStyle.Normal)]
        [TestCase("YuGothic-Regular", "'Yu Gothic',sans-serif", FontWeight.Regular, FontStyle.Normal)]
        public void Resolve(string pdfFontName, string fontFamily, FontWeight fontWeight, FontStyle fontStyle)
        {
            var font = (LocalFont)new StandardFontResolver().ResolveFont(pdfFontName, default);
            var actualFontWeight = font.FontWeight == FontWeight.Default ? FontWeight.Regular : font.FontWeight;

            Assert.AreEqual(fontFamily, font.FontFamily);
            Assert.AreEqual(fontWeight, actualFontWeight);
            Assert.AreEqual(fontStyle, font.FontStyle);
        }
    }
}
