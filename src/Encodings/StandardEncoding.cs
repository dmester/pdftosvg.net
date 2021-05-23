using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal class StandardEncoding : SingleByteEncoding
    {
        // Extracted from PDF spec 1.7 page 661-664.
        // Unicode char codes lookued up in Adobe Glyph List:
        // https://raw.githubusercontent.com/adobe-type-tools/agl-aglfn/master/glyphlist.txt

        private static readonly string chars =
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000" +
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000" +
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000" +
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000" +
            "\u0020\u0021\u0022\u0023\u0024\u0025\u0026\u2019" +
            "\u0028\u0029\u002a\u002b\u002c\u002d\u002e\u002f" +
            "\u0030\u0031\u0032\u0033\u0034\u0035\u0036\u0037" +
            "\u0038\u0039\u003a\u003b\u003c\u003d\u003e\u003f" +
            "\u0040\u0041\u0042\u0043\u0044\u0045\u0046\u0047" +
            "\u0048\u0049\u004a\u004b\u004c\u004d\u004e\u004f" +
            "\u0050\u0051\u0052\u0053\u0054\u0055\u0056\u0057" +
            "\u0058\u0059\u005a\u005b\u005c\u005d\u005e\u005f" +
            "\u2018\u0061\u0062\u0063\u0064\u0065\u0066\u0067" +
            "\u0068\u0069\u006a\u006b\u006c\u006d\u006e\u006f" +
            "\u0070\u0071\u0072\u0073\u0074\u0075\u0076\u0077" +
            "\u0078\u0079\u007a\u007b\u007c\u007d\u007e\u0000" +
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000" +
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000" +
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000" +
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000" +
            "\u0000\u00a1\u00a2\u00a3\u2044\u00a5\u0192\u00a7" +
            "\u00a4\u0027\u201c\u00ab\u2039\u203a\ufb01\ufb02" +
            "\u0000\u2013\u2020\u2021\u00b7\u0000\u00b6\u2022" +
            "\u201a\u201e\u201d\u00bb\u2026\u2030\u0000\u00bf" +
            "\u0000\u0060\u00b4\u02c6\u02dc\u00af\u02d8\u02d9" +
            "\u00a8\u0000\u02da\u00b8\u0000\u02dd\u02db\u02c7" +
            "\u2014\u0000\u0000\u0000\u0000\u0000\u0000\u0000" +
            "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000" +
            "\u0000\u00c6\u0000\u00aa\u0000\u0000\u0000\u0000" +
            "\u0141\u00d8\u0152\u00ba\u0000\u0000\u0000\u0000" +
            "\u0000\u00e6\u0000\u0000\u0000\u0131\u0000\u0000" +
            "\u0142\u00f8\u0153\u00df\u0000\u0000\u0000\u0000";

        public StandardEncoding() : base(chars) { }
    }
}
