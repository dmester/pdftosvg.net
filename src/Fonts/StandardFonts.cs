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
    }
}
