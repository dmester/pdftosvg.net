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
    internal class AxialShading : NativeSvgShading
    {
        private static readonly XNamespace ns = "http://www.w3.org/2000/svg";

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

        public override XElement? GetShadingElement(Matrix transform, Rectangle clipRectangle, bool inPattern)
        {
            var linearGradient = new XElement(ns + "linearGradient");

            linearGradient.SetAttributeValue("x1", SvgConversion.FormatCoordinate(X1));
            linearGradient.SetAttributeValue("y1", SvgConversion.FormatCoordinate(Y1));
            linearGradient.SetAttributeValue("x2", SvgConversion.FormatCoordinate(X2));
            linearGradient.SetAttributeValue("y2", SvgConversion.FormatCoordinate(Y2));

            if (!transform.IsIdentity)
            {
                linearGradient.SetAttributeValue("gradientTransform", SvgConversion.Matrix(transform));
            }

            linearGradient.SetAttributeValue("gradientUnits", "userSpaceOnUse");

            AddStopElements(linearGradient, inPattern);

            return linearGradient;
        }
    }
}
