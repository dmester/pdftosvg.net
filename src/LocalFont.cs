using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    public class LocalFont : Font
    {
        public LocalFont(string fontFamily, string fontWeight, string fontStyle)
        {
            FontFamily = fontFamily;
            FontWeight = fontWeight;
            FontStyle = fontStyle;
        }

        public override string FontFamily { get; }

        public string FontWeight { get; }

        public string FontStyle { get; }
    }
}
