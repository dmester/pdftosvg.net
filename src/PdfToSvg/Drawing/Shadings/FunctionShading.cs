// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing.Paths;
using PdfToSvg.Functions;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using PdfToSvg.Svg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Drawing.Shadings
{
    internal class FunctionShading : Shading
    {
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
                var input = new float[2];
                var colorFloatComponents = new float[ColorSpace.ComponentsPerSample];

                // The Sub filter is likely a good candidate for most shadings
                row[0] = (byte)PngFilter.Sub;

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        input[0] = (float)(minX + x * xMultiplier);
                        input[1] = (float)(minY + y * yMultiplier);
                        function.Evaluate(input, 0, colorFloatComponents, 0);

                        ColorSpace.ToRgb8(colorFloatComponents, 0, row, 1 + x * OutputComponentsPerSample, sampleCount: 1);
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

        public override SvgElement? GetShadingElement(Matrix transform, Rectangle clipRectangle, bool inPattern)
        {
            // This implementation could be shared with MeshShading, but wasn't since it is much easier in this case to
            // apply the transform on the rendered image itself, instead as in MeshShading, on the polygons inside the
            // rendered image.

            var svgConversion = new SvgConversion();

            SvgElement? backgroundEl = null;
            SvgElement? clipEl = null;
            string? clipAttributeValue = null;

            if (BBox != null)
            {
                var bboxCoords = transform * BBox.Value;

                var bboxPath = new PathData();
                bboxPath.Polygon(bboxCoords);
                var bboxPathData = svgConversion.PathData(bboxPath);

                // Background is not applicable when using the sh operator according to the spec
                if (Background != null && inPattern)
                {
                    backgroundEl = new SvgElement("path");
                    backgroundEl.AddAttribute("fill", svgConversion.FormatColor(Background.Value), isRaw: true);
                    backgroundEl.AddAttribute("d", bboxPathData, isRaw: true);
                }

                var clipId = StableID.Generate("cl", "MeshClip", bboxPathData);

                var clipPathEl = new SvgElement("path");
                clipPathEl.AddAttribute("d", bboxPathData, isRaw: true);

                clipEl = new SvgElement("clipPath");
                clipEl.AddAttribute("id", clipId, isRaw: true);
                clipEl.AddChild(clipPathEl);

                clipAttributeValue = "url(#" + clipId + ")";
            }
            // Background is not applicable when using the sh operator according to the spec
            else if (Background != null && inPattern)
            {
                backgroundEl = new SvgElement("rect");
                backgroundEl.AddAttribute("fill", svgConversion.FormatColor(Background.Value), isRaw: true);
                backgroundEl.AddAttribute("width", svgConversion.FormatCoordinate(clipRectangle.X2), isRaw: true);
                backgroundEl.AddAttribute("height", svgConversion.FormatCoordinate(clipRectangle.Y2), isRaw: true);
            }

            var imageWidth = (int)MathUtils.Clamp(maxX - minX, 1, MaxSize);
            var imageHeight = (int)MathUtils.Clamp(maxY - minY, 1, MaxSize);

            var imageUrl = RenderDataUrl(imageWidth, imageHeight);

            var imageEl = new SvgElement("image");
            imageEl.AddAttribute("transform", svgConversion.Matrix(matrix * transform), isRaw: true);
            imageEl.AddAttribute("x", svgConversion.FormatCoordinate(minX), isRaw: true);
            imageEl.AddAttribute("y", svgConversion.FormatCoordinate(minY), isRaw: true);
            imageEl.AddAttribute("width", svgConversion.FormatCoordinate(maxX - minX), isRaw: true);
            imageEl.AddAttribute("height", svgConversion.FormatCoordinate(maxY - minY), isRaw: true);
            imageEl.AddAttribute("preserveAspectRatio", "none", isRaw: true);
            imageEl.AddAttribute("href", imageUrl, isRaw: true);

            var imageWrapperEl = new SvgElement("g");
            if (clipAttributeValue != null)
            {
                imageWrapperEl.AddAttribute("clip-path", clipAttributeValue, isRaw: true);
            }
            imageWrapperEl.AddChild(imageEl);

            var patternEl = new SvgElement("pattern");
            patternEl.AddAttribute("width", svgConversion.FormatCoordinate(clipRectangle.X2), isRaw: true);
            patternEl.AddAttribute("height", svgConversion.FormatCoordinate(clipRectangle.Y2), isRaw: true);
            patternEl.AddAttribute("patternUnits", "userSpaceOnUse", isRaw: true);

            if (clipEl != null)
            {
                patternEl.AddChild(clipEl);
            }

            if (backgroundEl != null)
            {
                patternEl.AddChild(backgroundEl);
            }

            patternEl.AddChild(imageWrapperEl);

            return patternEl;
        }
    }
}
