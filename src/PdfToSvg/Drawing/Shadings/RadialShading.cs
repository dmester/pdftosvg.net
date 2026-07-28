// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace PdfToSvg.Drawing.Shadings
{
    internal class RadialShading : NativeSvgShading
    {
        private static readonly XNamespace ns = "http://www.w3.org/2000/svg";

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

        public override XElement? GetShadingElement(Matrix transform, Rectangle clipRectangle, bool inPattern)
        {
            var svgConversion = new SvgConversion();
            var radialGradient = new XElement(ns + "radialGradient");

            radialGradient.SetAttributeValue("fx", svgConversion.FormatCoordinate(X0));
            radialGradient.SetAttributeValue("fy", svgConversion.FormatCoordinate(Y0));
            radialGradient.SetAttributeValue("fr", svgConversion.FormatCoordinate(R0));

            radialGradient.SetAttributeValue("cx", svgConversion.FormatCoordinate(X1));
            radialGradient.SetAttributeValue("cy", svgConversion.FormatCoordinate(Y1));
            radialGradient.SetAttributeValue("r", svgConversion.FormatCoordinate(R1));

            if (!transform.IsIdentity)
            {
                radialGradient.SetAttributeValue("gradientTransform", svgConversion.Matrix(transform));
            }

            radialGradient.SetAttributeValue("gradientUnits", "userSpaceOnUse");

            AddStopElements(svgConversion, radialGradient, inPattern);

            return radialGradient;
        }
    }
}
