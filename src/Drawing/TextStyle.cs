using PdfToSvg.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class TextStyle
    {
        public TextStyle(GraphicsState state, double fontSize)
        {
            Fill = state.FillColor;
            Stroke = state.StrokeColor;

            var textState = state.TextState;
            Font = textState.Font;
            FontSize = fontSize;
            CharSpacingPx = textState.CharSpacing;
            WordSpacingPx = textState.WordSpacing;
            RenderingMode = textState.RenderingMode;
            RisePx = textState.Rise;
            Scaling = textState.Scaling;
        }

        public RgbColor Fill { get; }
        public RgbColor Stroke { get; }
        public InternalFont Font { get; }
        public double FontSize { get; }
        public double CharSpacingPx { get; }
        public double WordSpacingPx { get; }
        public TextRenderingMode RenderingMode { get; }
        public double RisePx { get; }
        public double Scaling { get; }
    }
}
