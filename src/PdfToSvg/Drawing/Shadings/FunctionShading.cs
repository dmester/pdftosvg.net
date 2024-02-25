// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing.Paths;
using PdfToSvg.Functions;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace PdfToSvg.Drawing.Shadings
{
    internal class FunctionShading : Shading
    {
        private static readonly XNamespace ns = "http://www.w3.org/2000/svg";
        private const int MaxSize = 200;
        private const int OutputComponentsPerSample = 3;

        private readonly double minX;
        private readonly double maxX;
        private readonly double minY;
        private readonly double maxY;

        private readonly Matrix matrix;
        private readonly Function function;

        public FunctionShading(PdfDictionary definition, CancellationToken cancellationToken) : base(definition, cancellationToken)
        {
            if (definition.TryGetArray<double>(Names.Domain, out var domain) && domain.Length >= 4)
            {
                minX = domain[0];
                maxX = domain[1];
                minY = domain[2];
                maxY = domain[3];
            }
            else
            {
                minX = 0;
                maxX = 1;
                minY = 0;
                maxY = 1;
            }

            matrix = definition.GetValueOrDefault(Names.Matrix, Matrix.Identity);

            var functionDefinition = definition.GetDictionaryOrNull(Names.Function);
            function = Function.Parse(functionDefinition, cancellationToken);
        }

        private byte[] Render(int width, int height)
        {
            using var pngStream = new MemoryStream();
            var pngWriter = new PngEncoder(pngStream);

            pngWriter.WriteSignature();
            pngWriter.WriteImageHeader(width, height, PngColorType.Truecolour, bitDepth: 8);

            var xMultiplier = (maxX - minX) / width;
            var yMultiplier = (maxY - minY) / height;

            using (var pngDataStream = pngWriter.GetImageDataStream())
            {
                var row = new byte[1 + width * OutputComponentsPerSample];
                var colorFloatComponents = new float[ColorSpace.ComponentsPerSample];

                // The Sub filter is likely a good candidate for most shadings
                row[0] = (byte)PngFilter.Sub;

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var colorDoubleComponents = function.Evaluate(
                            minX + x * xMultiplier,
                            minY + y * yMultiplier);

                        for (var i = 0; i < colorDoubleComponents.Length && i < colorFloatComponents.Length; i++)
                        {
                            colorFloatComponents[i] = (float)colorDoubleComponents[i];
                        }

                        ColorSpace.ToRgb8(colorFloatComponents, 0, row, 1 + x * OutputComponentsPerSample, count: 1);
                    }

                    for (var i = row.Length - OutputComponentsPerSample; i > 1; i -= OutputComponentsPerSample)
                    {
                        for (var j = 0; j < OutputComponentsPerSample; j++)
                        {
                            row[i + j] = (byte)(row[i + j] - row[i + j - OutputComponentsPerSample]);
                        }
                    }

                    pngDataStream.Write(row);
                }
            }

            pngWriter.WriteImageEnd();

            return pngStream.ToArray();
        }

        private string RenderDataUrl(int width, int height)
        {
            return "data:image/png;base64," + Convert.ToBase64String(Render(width, height));
        }

        public override XElement? GetShadingElement(Matrix transform, Rectangle clipRectangle, bool inPattern)
        {
            // This implementation could be shared with MeshShading, but wasn't since it is much easier in this case to
            // apply the transform on the rendered image itself, instead as in MeshShading, on the polygons inside the
            // rendered image.

            XElement? backgroundEl = null;
            XElement? clipEl = null;
            XAttribute? clipAttribute = null;

            if (BBox != null)
            {
                var bboxCoords = transform * BBox.Value;

                var bboxPath = new PathData();
                bboxPath.Polygon(bboxCoords);
                var bboxPathData = SvgConversion.PathData(bboxPath);

                // Background is not applicable when using the sh operator according to the spec
                if (Background != null && inPattern)
                {
                    backgroundEl = new XElement(ns + "path",
                        new XAttribute("fill", SvgConversion.FormatColor(Background.Value)),
                        new XAttribute("d", bboxPathData)
                        );
                }

                var clipId = StableID.Generate("cl", "MeshClip", bboxPathData);

                clipEl = new XElement(ns + "clipPath",
                    new XAttribute("id", clipId),
                    new XElement(ns + "path",
                        new XAttribute("d", bboxPathData)
                        ));

                clipAttribute = new XAttribute("clip-path", "url(#" + clipId + ")");
            }
            // Background is not applicable when using the sh operator according to the spec
            else if (Background != null && inPattern)
            {
                backgroundEl = new XElement(ns + "rect",
                    new XAttribute("fill", SvgConversion.FormatColor(Background.Value)),
                    new XAttribute("width", SvgConversion.FormatCoordinate(clipRectangle.X2)),
                    new XAttribute("height", SvgConversion.FormatCoordinate(clipRectangle.Y2))
                    );
            }

            var imageWidth = (int)MathUtils.Clamp(maxX - minX, 1, MaxSize);
            var imageHeight = (int)MathUtils.Clamp(maxY - minY, 1, MaxSize);

            var imageUrl = RenderDataUrl(imageWidth, imageHeight);

            var patternEl = new XElement(ns + "pattern",
                new XAttribute("width", SvgConversion.FormatCoordinate(clipRectangle.X2)),
                new XAttribute("height", SvgConversion.FormatCoordinate(clipRectangle.Y2)),
                new XAttribute("patternUnits", "userSpaceOnUse"),

                clipEl,
                backgroundEl,

                new XElement(ns + "g",
                    clipAttribute,
                    new XElement(ns + "image",
                        new XAttribute("transform", SvgConversion.Matrix(matrix * transform)),
                        new XAttribute("x", SvgConversion.FormatCoordinate(minX)),
                        new XAttribute("y", SvgConversion.FormatCoordinate(minY)),
                        new XAttribute("width", SvgConversion.FormatCoordinate(maxX - minX)),
                        new XAttribute("height", SvgConversion.FormatCoordinate(maxY - minY)),
                        new XAttribute("preserveAspectRatio", "none"),
                        new XAttribute("href", imageUrl)
                        )
                    ));

            return patternEl;
        }
    }
}
