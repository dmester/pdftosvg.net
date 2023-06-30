// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace PdfToSvg.Drawing.Shadings
{
    internal abstract class Shading
    {
        public PdfDictionary Definition { get; }

        public ColorSpace ColorSpace { get; }

        public Rectangle? BBox { get; }

        public Shading(PdfDictionary definition, CancellationToken cancellationToken)
        {
            Definition = definition;

            if (definition.TryGetArray<double>(Names.BBox, out var bbox) && bbox.Length == 4)
            {
                BBox = new Rectangle(bbox[0], bbox[1], bbox[2], bbox[3]);
            }

            var colorSpaceDefinition = definition.GetValueOrDefault(Names.ColorSpace);
            ColorSpace = ColorSpaceParser.Parse(colorSpaceDefinition, null, cancellationToken);
        }

        public static Shading? Create(PdfDictionary definition, CancellationToken cancellationToken)
        {
            var shadingType = (ShadingType)definition.GetValueOrDefault(Names.ShadingType, 0);

            switch (shadingType)
            {
                case ShadingType.Axial:
                    return new AxialShading(definition, cancellationToken);

                case ShadingType.Radial:
                    return new RadialShading(definition, cancellationToken);

                default:
                    Log.WriteLine("Unsupported shading type " + shadingType + ".");
                    return null;
            }
        }

        public abstract XElement? GetShadingElement(Matrix transform, bool inPattern);
    }
}
