// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

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

        private readonly double minSpaceEm;
        private readonly double minSpacePx;
        private GraphicsState? textStyle;
        private double pendingSpace;
        private TextParagraph? currentParagraph;

        private double normalizedFontSize;

        // This represents the line matrix
        private double scale;
        private double translateX;
        private double translateY;
        private Matrix remainingTransform = Matrix.Identity;

        private const double ScalingMultiplier = 1.0 / 100;

        public TextBuilder(double minSpaceEm, double minSpacePx)
        {
            this.minSpaceEm = minSpaceEm;
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
                pendingSpace += translateX - previousTranslateX;
            }
            else
            {
                pendingSpace = 0;
                currentParagraph = null;
            }

            if (normalizedFontSize != previousNormalizedFontSize)
            {
                InvalidateStyle();
            }
        }

        public void AddSpan(GraphicsState graphicsState, PdfString text)
        {
            if (text.Length == 0)
            {
                return;
            }

            var decodedText = graphicsState.Font.Decode(text, out var width);

            decodedText = SvgConversion.ReplaceInvalidChars(decodedText);

            width *= normalizedFontSize;

            var style = GetTextStyle(graphicsState);

            if (currentParagraph == null)
            {
                NewParagraph();
            }

            var totalWidth = width + text.Length * style.TextCharSpacingPx;

            var wordSpacing = graphicsState.TextWordSpacingPx;
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

                totalWidth += (words.Length - 1) * wordSpacingGlobalUnits;
            }
            else
            {
                AddSpanNoSpacing(style, decodedText, width);
            }

            totalWidth *= graphicsState.TextScaling * ScalingMultiplier;

            Translate(graphicsState, totalWidth);
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
            Translate(graphicsState, widthTextSpace);
        }

        private void AddSpanNoSpacing(GraphicsState style, string text, double width)
        {
            width *= style.TextScaling * ScalingMultiplier;

            if (currentParagraph == null)
            {
                currentParagraph = NewParagraph();
            }

            var absolutePendingSpace = Math.Abs(pendingSpace);

            var mergeWithPrevious =
                absolutePendingSpace < minSpacePx ||
                absolutePendingSpace < minSpaceEm * normalizedFontSize;

            if (mergeWithPrevious && currentParagraph.Content.Count > 0)
            {
                var span = currentParagraph.Content.Last();
                if (span.Style == style)
                {
                    span.Value += text;
                    span.Width += pendingSpace + width;
                    pendingSpace = 0;
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
