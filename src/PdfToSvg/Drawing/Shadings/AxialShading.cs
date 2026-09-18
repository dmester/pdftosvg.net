// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Svg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Drawing.Shadings
{
    internal class AxialShading : NativeSvgShading
    {
        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; } = 1;
        public double Y2 { get; }

        public AxialShading(PdfDictionary definition, CancellationToken cancellationToken) : base(definition, cancellationToken)
        {
            if (definition.TryGetArray<double>(Names.Coords, out var coords) &&
                coords.Length >= 4)
            {
                X1 = coords[0];
                Y1 = coords[1];
                X2 = coords[2];
                Y2 = coords[3];
            }
        }

        public override SvgElement? GetShadingElement(Matrix transform, Rectangle clipRectangle, bool inPattern)
        {
            var svgConversion = new SvgConversion();
            var linearGradient = new SvgElement("linearGradient");

            linearGradient.AddAttribute("x1", svgConversion.FormatCoordinate(X1), isRaw: true);
            linearGradient.AddAttribute("y1", svgConversion.FormatCoordinate(Y1), isRaw: true);
            linearGradient.AddAttribute("x2", svgConversion.FormatCoordinate(X2), isRaw: true);
            linearGradient.AddAttribute("y2", svgConversion.FormatCoordinate(Y2), isRaw: true);

            if (!transform.IsIdentity)
            {
                linearGradient.AddAttribute("gradientTransform", svgConversion.Matrix(transform), isRaw: true);
            }

            linearGradient.AddAttribute("gradientUnits", "userSpaceOnUse", isRaw: true);

            AddStopElements(svgConversion, linearGradient, inPattern);

            return linearGradient;
        }
    }
}
