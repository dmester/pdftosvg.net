// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    // TODO merge with TextState?
    internal class GraphicsState
    {
        // Graphics state
        // PDF spec 1.7, Table 52, page 129
        public Matrix Transform = Matrix.Identity;

        public double Flatness;
        public int Intent;
        public ClipPath? ClipPath;

        public ColorSpace FillColorSpace = new DeviceRgbColorSpace();
        public ColorSpace StrokeColorSpace = new DeviceRgbColorSpace();

        public RgbColor FillColor = RgbColor.Black;
        public double FillAlpha = 1d;

        public RgbColor StrokeColor = RgbColor.Black;
        public double StrokeAlpha = 1d;

        public double StrokeWidth = 1d;
        public int StrokeLineJoin;
        public int StrokeLineCap;
        public int[]? StrokeDashArray;
        public int StrokeDashPhase;
        public double StrokeMiterLimit = 10d;

        // Text state
        // PDF spec 1.7, Table 104, page 251
        public Matrix TextMatrix = Matrix.Identity;
        public Matrix LineMatrix = Matrix.Identity;
        public double TextLeading;
        public InternalFont Font = InternalFont.Fallback;
        public double FontSize;
        public double TextCharSpacingPx;
        public double TextWordSpacingPx;
        public TextRenderingMode TextRenderingMode = TextRenderingMode.Fill;
        public double TextRisePx;
        public double TextScaling = 100;

        public GraphicsState Clone()
        {
            return (GraphicsState)MemberwiseClone();
        }
    }
}
