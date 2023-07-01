// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Fonts;
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

        private readonly double collapseSpaceLocalFont;
        private readonly double collapseSpaceEmbeddedFont;
        private readonly double minSpacePx;

        private GraphicsState? textStyle;
        private double pendingSpace;
        private double aggregateOffset;
        private TextParagraph? currentParagraph;

        private double normalizedFontSize;

        // This represents the line matrix
        private double scale;
        private double translateX;
        private double translateY;
        private Matrix remainingTransform = Matrix.Identity;

        private const double ScalingMultiplier = 1.0 / 100;

        public TextBuilder(double collapseSpaceLocalFont, double collapseSpaceEmbeddedFont, double minSpacePx)
        {
            this.collapseSpaceLocalFont = collapseSpaceLocalFont;
            this.collapseSpaceEmbeddedFont = collapseSpaceEmbeddedFont;
            this.minSpacePx = minSpacePx;
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
            aggregateOffset = 0;
            currentParagraph = null;
            paragraphs.Clear();
            textStyle = null;
        }

        public void UpdateLineMatrix(GraphicsState graphicsState)
        {
            var transform = graphicsState.TextMatrix * graphicsState.Transform;

            var previousScale = scale;
            var previousTranslateX = translateX;
            var previousTranslateY = translateY;
            var previousTransform = remainingTransform;
            var previousNormalizedFontSize = normalizedFontSize;

            // The origin in pdfs is in the bottom-left corner. We have a root transform which flips the entire page 
            // vertically, to get the origin in the upper-left corner. To avoid flipped text, we need to flip
            // each text vertically as well.
            transform = Matrix.Scale(1, -1, transform);

            // Potential improvement:
            // This is not correct in case the horizontal and vertical scaling are not the same.
            transform.DecomposeScale(out scale, out transform);

            normalizedFontSize = graphicsState.FontSize * scale;

            // Force font size to be positive
            if (normalizedFontSize < 0)
            {
                normalizedFontSize = -normalizedFontSize;
                scale = -scale;
                transform = Matrix.Scale(-1, -1, transform);
            }

            // Force scaling to be positive
            if (graphicsState.TextScaling < 0)
            {
                transform = Matrix.Scale(-1, 1, transform);
            }

            transform.DecomposeTranslate(out translateX, out translateY, out remainingTransform);

            if (currentParagraph != null &&

                // SVG does not support negative dx placing the cursor before the <text> x position
                translateX >= currentParagraph.X &&

                scale == previousScale &&
                translateY == previousTranslateY &&
                remainingTransform == previousTransform &&

                // Don't overdo merging of adjacent text spans to avoid issues e.g. in tabular views
                Math.Abs(translateX - previousTranslateX) < 10)
            {
                var offset = translateX - previousTranslateX;
                pendingSpace += offset;
                aggregateOffset += offset;
            }
            else
            {
                pendingSpace = 0;
                aggregateOffset = 0;
                currentParagraph = null;
            }

            if (normalizedFontSize != previousNormalizedFontSize)
            {
                InvalidateStyle();
            }
        }

        public void AddSpan(GraphicsState graphicsState, PdfString text)
        {
            if (text.Length > 0)
            {
                if (graphicsState.Font.SubstituteFont is InlinedFont inlinedFont &&
                    inlinedFont.SourceFont is Type3Font type3Font)
                {
                    AddSpanType3(graphicsState, text, type3Font);
                }
                else
                {
                    AddTextSpan(graphicsState, text);
                }
            }
        }

        private void AddTextSpan(GraphicsState graphicsState, PdfString text)
        {
            if (currentParagraph == null)
            {
                NewParagraph();
            }

            var style = GetTextStyle(graphicsState);

            var textScaling = style.TextScaling * ScalingMultiplier;
            var wordSpacing = graphicsState.TextWordSpacingPx * textScaling * scale;
            var totalWidth = 0d;

            var words = graphicsState.Font.DecodeString(text, splitWords: wordSpacing != 0);

            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                var wordWidth = (word.Width * normalizedFontSize + word.Length * style.TextCharSpacingPx) * textScaling;

                if (i > 0)
                {
                    pendingSpace += wordSpacing;
                    aggregateOffset += wordSpacing;
                    totalWidth += wordSpacing;
                }

                if (!string.IsNullOrEmpty(word.Value))
                {
                    AddSpanNoSpacing(style, word.Value, wordWidth);
                }

                totalWidth += wordWidth;
            }

            Translate(graphicsState, totalWidth);
        }

        private void AddSpanType3(GraphicsState graphicsState, PdfString text, Type3Font type3)
        {
            currentParagraph = null;
            pendingSpace = 0;
            aggregateOffset = 0;

            var style = GetTextStyle(graphicsState);
            var wordSpacingGlobalUnits = style.TextWordSpacingPx * scale;

            var fontMatrix = type3.FontMatrix * Matrix.Scale(normalizedFontSize, -normalizedFontSize);

            if (style.TextScaling != 100)
            {
                fontMatrix = Matrix.Scale(style.TextScaling * ScalingMultiplier, 1, fontMatrix);
            }

            for (var i = 0; i < text.Length; i++)
            {
                var charCode = text[i];

                var charInfo = type3.GetChar(charCode);
                var width = charInfo.Width * normalizedFontSize;

                if (charInfo.GlyphDefinition != null &&
                    charInfo.GlyphDefinition.Length > 0)
                {
                    var paragraph = new TextParagraph
                    {
                        Matrix = fontMatrix * Matrix.Translate(translateX, translateY, remainingTransform),
                        X = 0,
                        Y = 0,
                        Type3Content = charInfo.GlyphDefinition,
                        Type3Style = style,
                    };
                    paragraphs.Add(paragraph);
                }

                var dx = width + style.TextCharSpacingPx;

                if (charCode == ' ')
                {
                    dx += wordSpacingGlobalUnits;
                }

                dx *= style.TextScaling * ScalingMultiplier;

                Translate(graphicsState, dx);
            }
        }

        public void AddSpace(GraphicsState graphicsState, double widthGlyphSpaceUnits)
        {
            const double FontSizeMultiplier = 1.0 / 1000;
            const double WidthMultiplier = -FontSizeMultiplier * ScalingMultiplier;

            var widthTextSpace = widthGlyphSpaceUnits *
                graphicsState.FontSize *
                graphicsState.TextScaling * WidthMultiplier *
                scale;

            pendingSpace += widthTextSpace;
            aggregateOffset += widthTextSpace;
            Translate(graphicsState, widthTextSpace);

            // Some pdfs use TJ operators where the substrings are rendered out of order by using negative offsets.
            //
            // Those cases could previously end up with <tspan>s being rendered to the left of the owning <text>.
            // SVG viewers seem to treat this scenario in different ways. Some places the owner <text> based on the
            // first <tspan> (e.g. Inkscape), while others (e.g. Chrome and Firefox) places the owner <text> based on
            // the leftmost <tspan>, even if it is not the first <tspan>.
            //
            // To prevent this, don't allow <tspan>s growing to the left of the parent <text>.
            //
            if (currentParagraph != null &&
                currentParagraph.X > translateX)
            {
                currentParagraph = null;
                pendingSpace = 0;
                aggregateOffset = 0;
            }
        }

        private void AddSpanNoSpacing(GraphicsState style, string text, double width)
        {
            if (currentParagraph == null)
            {
                currentParagraph = NewParagraph();
            }

            var svgText = SvgConversion.ReplaceInvalidChars(text);

            var isLocalFont = style.Font.SubstituteFont is LocalFont;
            var collapseSpaceEm = isLocalFont
                ? collapseSpaceLocalFont
                : collapseSpaceEmbeddedFont;

            var absolutePendingSpace = Math.Abs(pendingSpace);

            var mergeWithPrevious =
                // Too small offsets will be rounded to 0, so they should not cause an extra <tspan>
                absolutePendingSpace < minSpacePx ||

                // When the threshold is low, it is most important to produce an as accurate result as possible, so in
                // this case, we will look at the total aggregate offset from the real line matrix. If the threshold is
                // high, it is probably most important that the text runs are looking good, even if, e.g., the font has
                // not been exported.
                (
                    collapseSpaceEm < 0.1
                    ? Math.Abs(aggregateOffset) < collapseSpaceEm * normalizedFontSize
                    : absolutePendingSpace < collapseSpaceEm * normalizedFontSize
                );

            if (mergeWithPrevious && currentParagraph.Content.Count > 0)
            {
                var span = currentParagraph.Content.Last();
                if (span.Style == style)
                {
                    span.Value += svgText;
                    span.Width += pendingSpace + width;
                    pendingSpace = 0;
                    return;
                }
            }

            currentParagraph.Content.Add(new TextSpan(isLocalFont ? pendingSpace : aggregateOffset, style, svgText, width));
            pendingSpace = 0;
            aggregateOffset = 0;
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
            aggregateOffset = 0;

            return currentParagraph;
        }

        private void Translate(GraphicsState graphicsState, double dx)
        {
            translateX += dx;
            graphicsState.TextMatrix = Matrix.Translate(dx / scale, 0, graphicsState.TextMatrix);
        }

        private GraphicsState GetTextStyle(GraphicsState graphicsState)
        {
            if (textStyle == null)
            {
                textStyle = graphicsState.Clone();
                textStyle.FontSize = normalizedFontSize;
                textStyle.TextScaling = Math.Abs(graphicsState.TextScaling);
                textStyle.TextCharSpacingPx *= Math.Abs(scale);
            }

            return textStyle;
        }
    }
}
