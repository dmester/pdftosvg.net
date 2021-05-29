using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class TextSpan
    {
        public string Value;
        public double Width;
        public double SpaceBefore;
        public TextStyle Style;

        public TextSpan(double spaceBefore, TextStyle style, string value, double width)
        {
            SpaceBefore = spaceBefore;
            Style = style;
            Value = value;
            Width = width;
        }
    }
}
