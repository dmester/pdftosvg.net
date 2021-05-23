using PdfToSvg.ColorSpaces;
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
        // PDF spec 1.7, Table 104, page 251

        public TextState TextState { get; set; } = new TextState();

        public Matrix Transform { get; set; } = Matrix.Identity;

        public double Flatness { get; set; }

        public int Intent { get; set; }

        public ClipPath ClipPath { get; set; }

        public int[] DashArray { get; set; }

        public int DashPhase { get; set; }

        public double MiterLimit { get; set; }

        public int LineJoin { get; set; }

        public int LineCap { get; set; }

        public double LineWidth { get; set; }

        public ColorSpace FillColorSpace { get; set; } = new DeviceRgbColorSpace();
        public ColorSpace StrokeColorSpace { get; set; } = new DeviceRgbColorSpace();

        public RgbColor FillColor { get; set; } = RgbColor.Black;
        public RgbColor StrokeColor { get; set; } = RgbColor.Black;

        public GraphicsState Clone()
        {
            var clone = (GraphicsState)MemberwiseClone();
            clone.TextState = TextState.Clone();
            return clone;
        }
    }
}
