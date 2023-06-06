// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcittFaxEncoder
{
    internal class PdfEncoder
    {
        private const int BytesPerLine = 64;
        private const string CrLf = "\r\n";

        private static string AsciiHexEncode(byte[] bytes)
        {
            var result = new StringBuilder(bytes.Length * 3);

            for (var i = 0; i < bytes.Length; i++)
            {
                if ((i % BytesPerLine) == 0)
                {
                    if (i > 0)
                    {
                        result.AppendLine();
                    }

                    result.Append("   ");
                }

                result.Append(bytes[i].ToString("x2"));
            }

            result.Append('>');

            return result.ToString();
        }

        private static string? Format(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        private static string FormatDictionary(string indentation, Dictionary<string, object> dict)
        {
            return string.Concat(dict.Select(x => "\r\n" + indentation + x.Key + " " + Format(x.Value)));
        }

        public static string CreateTestFile(byte[] image,
            int realWidth, int realHeight,
            Dictionary<string, object> imageDict, Dictionary<string, object> decodeParms)
        {
            var hexEncodedImage = AsciiHexEncode(image);

            imageDict["/Type"] = "/XObject";
            imageDict["/Subtype"] = "/Image";
            imageDict["/Width"] = realWidth;
            imageDict["/Height"] = realHeight;
            imageDict["/BitsPerComponent"] = 1;
            imageDict["/ColorSpace"] = "/DeviceGray";
            imageDict["/Filter"] = "[ /ASCIIHexDecode /CCITTFaxDecode ]";
            imageDict["/DecodeParms"] = "[ null <<" + FormatDictionary("      ", decodeParms) + "\r\n   >> ]";
            imageDict["/Length"] = hexEncodedImage.Length.ToString(CultureInfo.InvariantCulture);

            var encodedImageDict = FormatDictionary("   ", imageDict);

            var pageWidth = 594;
            var pageHeight = 841;
            var topPageUnits = 100;
            var widthPageUnits = 280;

            var heightPageUnits = widthPageUnits * realHeight / realWidth;
            var xPageUnits = (pageWidth - widthPageUnits) / 2;
            var yPageUnits = pageHeight - topPageUnits - heightPageUnits;

            var cm = string.Format(
                CultureInfo.InvariantCulture, "{0,3} 0 0 {1,3} {2,3} {3,3} cm",
                widthPageUnits, heightPageUnits, xPageUnits, yPageUnits);

            var pdf =
                "%PDF-1.7" + CrLf +
                CrLf +
                "xref" + CrLf +
                "0 6" + CrLf +
                "0000000000 65535 f" + CrLf +
                "0000000182 00000 n" + CrLf +
                "0000000233 00000 n" + CrLf +
                "0000000297 00000 n" + CrLf +
                "0000000476 00000 n" + CrLf +
                "0000000571 00000 n" + CrLf +
                "trailer" + CrLf +
                "<< /Root 1 0 R  /Size 6 >>" + CrLf +
                CrLf +

                "1 0 obj << /Type /Catalog  /Pages 2 0 R >> endobj" + CrLf +
                "2 0 obj << /Type /Pages  /Kids [ 3 0 R ]  /Count 1 >> endobj" + CrLf +
                CrLf +

                "3 0 obj" + CrLf +
                "<<" + CrLf +
                "   /Type /Page" + CrLf +
                "   /Parent 2 0 R" + CrLf +
                "   /Resources <<" + CrLf +
                "      /XObject << /Im1 5 0 R >>" + CrLf +
                "   >>" + CrLf +
                "   /MediaBox [ 0 0 594.96 841.92 ]" + CrLf +
                "   /Contents [ 4 0 R ]" + CrLf +
                ">>" + CrLf +
                "endobj" + CrLf +
                CrLf +

                "4 0 obj" + CrLf +
                "<< /Length 37 >>" + CrLf +
                "stream" + CrLf +
                "   " + cm + CrLf +
                "   /Im1 Do" + CrLf +
                "endstream" + CrLf +
                "endobj" + CrLf +
                CrLf +

                "5 0 obj" + CrLf +
                "<<" + encodedImageDict + CrLf +
                ">>" + CrLf +
                "stream" + CrLf +
                hexEncodedImage + CrLf +
                "endstream" + CrLf +
                "endobj" + CrLf +
                CrLf +

                "startxref" + CrLf +
                "12" + CrLf +
                "%%EOF";

            return pdf;
        }
    }
}
