using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class TextBuilder
    {
        public List<TextParagraph> paragraphs = new List<TextParagraph>();
        private TextStyle? textStyle;
        private double pendingSpace;
        private TextParagraph? currentParagraph;

        private double normalizedFontSize;

        // This represents the line matrix
        private double scale;
        private double translateX;
        private double translateY;
        private Matrix remainingTransform = Matrix.Identity;

        const double ScalingMultiplier = 1.0 / 100;

        public TextBuilder()
        {
            Clear();
        }

        public void InvalidateStyle()
        {
            textStyle = null;
        }

        public void Clear()
        {
            scale = double.NaN;
            translateX = double.NaN;
            translateY = double.NaN;
            remainingTransform = Matrix.Identity;
            pendingSpace = 0;
            currentParagraph = null;
            paragraphs.Clear();
            textStyle = null;
        }

        public void UpdateLineMatrix(GraphicsState graphicsState)
        {
            var transform = graphicsState.TextState.TextMatrix * graphicsState.Transform;

            var previousScale = scale;
            var previousTranslateX = translateX;
            var previousTranslateY = translateY;
            var previousTransform = remainingTransform;

            // The origin in pdfs is in the bottom-left corner. We have a root transform which flips the entire page 
            // vertically, to get the origin in the upper-left corner. To avoid flipped text, we need to flip
            // each text vertically as well.
            transform = Matrix.Scale(1, -1, transform);

            transform.DecomposeScale(out scale, out transform);
            
            normalizedFontSize = graphicsState.TextState.FontSize * scale;

            // Force font size to be positive
            if (normalizedFontSize < 0)
            {
                normalizedFontSize = -normalizedFontSize;
                scale = -scale;
                transform = Matrix.Scale(-1, -1, transform);
            }

            transform.DecomposeTranslate(out translateX, out translateY, out remainingTransform);

            if (scale == previousScale &&
                translateY == previousTranslateY &&
                remainingTransform == previousTransform &&

                // Don't overdo merging of adjacent text spans to avoid issues e.g. in tabular views
                Math.Abs(translateX - previousTranslateX) < 10)
            {
                pendingSpace += translateX - previousTranslateX;
            }
            else
            {
                pendingSpace = 0;
                currentParagraph = null;
            }
        }

        public void AddSpan(GraphicsState graphicsState, PdfString text)
        {
            if (text.Length == 0)
            {
                return;
            }

            // TODO handle null font
            var decodedText = graphicsState.TextState.Font.Decode(text, out var width);

            width *= normalizedFontSize;

            var style = GetTextStyle(graphicsState);

            if (currentParagraph == null)
            {
                NewParagraph();
            }

            var totalWidth = width + text.Length * style.CharSpacingPx;

            var wordSpacing = graphicsState.TextState.WordSpacing;
            if (wordSpacing != 0)
            {
                // TODO not correct, scale is not horizontal
                var wordSpacingGlobalUnits = wordSpacing * scale; 
                var words = decodedText.Split(' ');

                // This is not accurate, but the width of each individual word is not important
                // TODO but maybe for clipping?
                var wordWidth = width / words.Length;

                if (!string.IsNullOrEmpty(words[0]))
                {
                    AddSpanNoSpacing(style, words[0], wordWidth);
                }

                for (var i = 1; i < words.Length; i++)
                {
                    pendingSpace = wordSpacingGlobalUnits;
                    AddSpanNoSpacing(style, " " + words[i], wordWidth);
                }

                totalWidth += (words.Length - 1) * wordSpacing;
            }
            else
            {
                AddSpanNoSpacing(style, decodedText, width);
            }

            totalWidth *= graphicsState.TextState.Scaling * ScalingMultiplier;

            Translate(graphicsState, totalWidth);
        }

        public void AddSpace(GraphicsState graphicsState, double widthGlyphSpaceUnits)
        {
            const double FontSizeMultiplier = 1.0 / 1000;
            const double WidthMultiplier = -FontSizeMultiplier * ScalingMultiplier;

            var widthTextSpace = widthGlyphSpaceUnits *
                graphicsState.TextState.FontSize * 
                graphicsState.TextState.Scaling * WidthMultiplier *
                scale;

            pendingSpace += widthTextSpace;
            Translate(graphicsState, widthTextSpace);
        }

        private void AddSpanNoSpacing(TextStyle style, string text, double width)
        {
            width *= style.Scaling * ScalingMultiplier;

            if (currentParagraph == null)
            {
                currentParagraph = NewParagraph();
            }

            // TODO Remove kerning
            if (pendingSpace == 0 && currentParagraph.Content.Count > 0)
            {
                var span = currentParagraph.Content.Last();
                if (span.Style == style)
                {
                    span.Value += text;
                    span.Width += width;
                    return;
                }
            }

            currentParagraph.Content.Add(new TextSpan(pendingSpace, style, text, width));
            pendingSpace = 0;
        }

        private TextParagraph NewParagraph()
        {
            currentParagraph = new TextParagraph
            {
                Matrix = remainingTransform,
                X = translateX,
                Y = translateY,
            };
            paragraphs.Add(currentParagraph);

            pendingSpace = 0;

            return currentParagraph;
        }

        private void Translate(GraphicsState graphicsState, double dx)
        {
            translateX += dx;
            graphicsState.TextState.TextMatrix = Matrix.Translate(dx / scale, 0, graphicsState.TextState.TextMatrix);
        }

        private TextStyle GetTextStyle(GraphicsState graphicsState)
        {
            if (textStyle == null)
            {
                textStyle = new TextStyle(graphicsState, normalizedFontSize);
            }

            return textStyle;
        }
    }
}
