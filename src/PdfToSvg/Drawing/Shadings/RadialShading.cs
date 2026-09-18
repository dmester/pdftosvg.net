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
    internal class RadialShading : NativeSvgShading
    {
        public double X0 { get; }
        public double Y0 { get; }
        public double R0 { get; }
        public double X1 { get; }
        public double Y1 { get; }
        public double R1 { get; }

        public RadialShading(PdfDictionary definition, CancellationToken cancellationToken) : base(definition, cancellationToken)
        {
            if (definition.TryGetArray<double>(Names.Coords, out var coords) &&
                coords.Length >= 6)
            {
                X0 = coords[0];
                Y0 = coords[1];
                R0 = coords[2];
                X1 = coords[3];
                Y1 = coords[4];
                R1 = coords[5];
            }
        }

        public override SvgElement? GetShadingElement(Matrix transform, Rectangle clipRectangle, bool inPattern)
        {
            var svgConversion = new SvgConversion();
            var radialGradient = new SvgElement("radialGradient");

            radialGradient.AddAttribute("fx", svgConversion.FormatCoordinate(X0), isRaw: true);
            radialGradient.AddAttribute("fy", svgConversion.FormatCoordinate(Y0), isRaw: true);
            radialGradient.AddAttribute("fr", svgConversion.FormatCoordinate(R0), isRaw: true);

            radialGradient.AddAttribute("cx", svgConversion.FormatCoordinate(X1), isRaw: true);
            radialGradient.AddAttribute("cy", svgConversion.FormatCoordinate(Y1), isRaw: true);
            radialGradient.AddAttribute("r", svgConversion.FormatCoordinate(R1), isRaw: true);

            if (!transform.IsIdentity)
            {
                radialGradient.AddAttribute("gradientTransform", svgConversion.Matrix(transform), isRaw: true);
            }

            radialGradient.AddAttribute("gradientUnits", "userSpaceOnUse", isRaw: true);

            AddStopElements(svgConversion, radialGradient, inPattern);

            return radialGradient;
        }
    }
}
