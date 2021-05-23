using PdfToSvg.DocumentModel;
using PdfToSvg.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class TextState
    {
        // PDF spec 1.7, Table 104, page 251

        public Matrix TextMatrix = Matrix.Identity;
        public Matrix LineMatrix = Matrix.Identity;
        public double Leading;
        public InternalFont Font = InternalFont.Fallback;
        public double FontSize;
        public double CharSpacing;
        public double WordSpacing;
        public TextRenderingMode RenderingMode = TextRenderingMode.Fill;
        public double Rise;
        public double Scaling = 100;

        public TextState Clone()
        {
            return (TextState)MemberwiseClone();
        }
    }
}
