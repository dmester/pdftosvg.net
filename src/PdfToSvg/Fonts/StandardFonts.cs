// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts
{
    internal static class StandardFonts
    {
        public static PdfName TimesRoman { get; } = new PdfName("Times-Roman");
        public static PdfName TimesBold { get; } = new PdfName("Times-Bold");
        public static PdfName TimesItalic { get; } = new PdfName("Times-Italic");
        public static PdfName TimesBoldItalic { get; } = new PdfName("Times-BoldItalic");

        public static PdfName Helvetica { get; } = new PdfName("Helvetica");
        public static PdfName HelveticaBold { get; } = new PdfName("Helvetica-Bold");
        public static PdfName HelveticaOblique { get; } = new PdfName("Helvetica-Oblique");
        public static PdfName HelveticaBoldOblique { get; } = new PdfName("Helvetica-BoldOblique");

        public static PdfName Courier { get; } = new PdfName("Courier");
        public static PdfName CourierBold { get; } = new PdfName("Courier-Bold");
        public static PdfName CourierOblique { get; } = new PdfName("Courier-Oblique");
        public static PdfName CourierBoldOblique { get; } = new PdfName("Courier-BoldOblique");

        public static PdfName Symbol { get; } = new PdfName("Symbol");
        public static PdfName ZapfDingbats { get; } = new PdfName("ZapfDingbats");

        public static PdfName TranslateAlternativeNames(PdfName name)
        {
            // PDF spec 1.3, Table H3, page 795

            switch (name.Value)
            {
                case "TimesNewRoman":
                case "TimesNewRomanPS":
                case "TimesNewRomanPSMT":
                    return TimesRoman;

                case "TimesNewRoman-Bold":
                case "TimesNewRoman,Bold":
                case "TimesNewRomanPS-Bold":
                case "TimesNewRomanPS-BoldMT":
                    return TimesBold;

                case "TimesNewRoman-Italic":
                case "TimesNewRoman,Italic":
                case "TimesNewRomanPS-Italic":
                case "TimesNewRomanPS-ItalicMT":
                    return TimesItalic;

                case "TimesNewRoman-BoldItalic":
                case "TimesNewRoman,BoldItalic":
                case "TimesNewRomanPS-BoldItalic":
                case "TimesNewRomanPS-BoldItalicMT":
                    return TimesBoldItalic;

                case "Arial":
                case "ArialMT":
                    return Helvetica;

                case "Helvetica,Bold":
                case "Arial-Bold":
                case "Arial,Bold":
                case "Arial-BoldMT":
                    return HelveticaBold;

                case "Helvetica-Italic":
                case "Helvetica,Italic":
                case "Arial-Italic":
                case "Arial,Italic":
                case "Arial-ItalicMT":
                    return HelveticaOblique;

                case "Helvetica-BoldItalic":
                case "Helvetica,BoldItalic":
                case "Arial-BoldItalic":
                case "Arial,BoldItalic":
                case "Arial-BoldItalicMT":
                    return HelveticaBoldOblique;

                case "CourierNew":
                case "CourierNewPSMT":
                    return Courier;

                case "Courier,Bold":
                case "CourierNew-Bold":
                case "CourierNew,Bold":
                case "CourierNewPS-BoldMT":
                    return CourierBold;

                case "Courier,Italic":
                case "CourierNew-Italic":
                case "CourierNew,Italic":
                case "CourierNewPS-ItalicMT":
                    return CourierOblique;

                case "Courier,BoldItalic":
                case "CourierNew-BoldItalic":
                case "CourierNew,BoldItalic":
                case "CourierNewPS-BoldItalicMT":
                    return CourierBoldOblique;

                default:
                    return name;
            }
        }
    }
}
