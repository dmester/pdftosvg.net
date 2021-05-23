using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class TextParagraph
    {
        public List<TextSpan> Content = new List<TextSpan>();
        public Matrix Matrix;
        public double X;
        public double Y;
    }
}
