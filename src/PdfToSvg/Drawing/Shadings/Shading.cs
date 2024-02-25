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

        public RgbColor? Background { get; }

        public Shading(PdfDictionary definition, CancellationToken cancellationToken)
        {
            Definition = definition;

            // ISO 32000-2 Table 77 Common entries for all shadings

            // BBox
            if (definition.TryGetArray<double>(Names.BBox, out var bbox) && bbox.Length == 4)
            {
                BBox = new Rectangle(bbox[0], bbox[1], bbox[2], bbox[3]);
            }

            // Color space
            var colorSpaceDefinition = definition.GetValueOrDefault(Names.ColorSpace);
            ColorSpace = ColorSpaceParser.Parse(colorSpaceDefinition, null, cancellationToken);

            // Background
            var background = definition.GetArrayOrNull<float>(Names.Background);
            if (background != null && background.Length > 0)
            {
                Background = new RgbColor(ColorSpace, background);
            }
        }

        public static Shading? Create(PdfDictionary definition, CancellationToken cancellationToken)
        {
            var shadingType = (ShadingType)definition.GetValueOrDefault(Names.ShadingType, 0);

            switch (shadingType)
            {
                case ShadingType.Function:
                    return new FunctionShading(definition, cancellationToken);

                case ShadingType.Axial:
                    return new AxialShading(definition, cancellationToken);

                case ShadingType.Radial:
                    return new RadialShading(definition, cancellationToken);

                case ShadingType.FreeFormGouraud:
                    return new FreeFormGouraudShading(definition, cancellationToken);

                case ShadingType.LatticeFormGouraud:
                    return new LatticeFormGouraudShading(definition, cancellationToken);

                case ShadingType.CoonsPatchMesh:
                    return new CoonsPatchMeshShading(definition, cancellationToken);

                case ShadingType.TensorProductPatchMesh:
                    return new TensorProductPatchMeshShading(definition, cancellationToken);

                default:
                    Log.WriteLine("Unsupported shading type " + shadingType + ".");
                    return null;
            }
        }

        public abstract XElement? GetShadingElement(Matrix transform, Rectangle clipRectangle, bool inPattern);
    }
}
