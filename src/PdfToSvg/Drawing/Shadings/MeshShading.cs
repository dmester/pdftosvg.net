// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing.Paths;
using PdfToSvg.Functions;
using PdfToSvg.Imaging;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace PdfToSvg.Drawing.Shadings
{
    internal abstract class MeshShading : Shading
    {
        private static readonly XNamespace ns = "http://www.w3.org/2000/svg";

        private const int MinSizePx = 20;
        private const int MaxSizePx = 600;
        private const int ResolutionPxPerPt = 2;

        // The patch is divided into cells filled with a solid color.
        // The number of cells needs to be high enough to not cause too much banding, but also low enough to not cause performance issues.
        private const double OptimalCellSize = 2.5;
        private const int MinApproximationResolution = 4;
        private const int MaxApproximationResolution = 80;

        protected readonly List<Patch> patches = new();

        protected readonly Function function;
        protected readonly int componentsPerSample;

        protected readonly int bitsPerComponent;
        protected readonly int bitsPerCoordinate;
        protected readonly int bitsPerFlag;

        protected readonly DecodeArray colorDecode;
        protected readonly DecodeRange xDecode;
        protected readonly DecodeRange yDecode;

        protected readonly VariableBitReader reader;

        private class Approximation
        {
            private readonly float[] b0;
            private readonly float[] b1;
            private readonly float[] b2;
            private readonly float[] b3;

            public readonly int Resolution;
            public readonly float TimeMultiplier;

            public Approximation(int resolution)
            {
                Resolution = resolution;
                TimeMultiplier = 1.0f / (resolution - 1);

                b0 = new float[resolution];
                b1 = new float[resolution];
                b2 = new float[resolution];
                b3 = new float[resolution];

                for (var t = 0; t < b0.Length; t++)
                {
                    var ft = t * TimeMultiplier;
                    var ift = 1 - ft;

                    b0[t] = ift * ift * ift;
                    b1[t] = 3 * ft * ift * ift;
                    b2[t] = 3 * ft * ft * ift;
                    b3[t] = ft * ft * ft;
                }
            }

            public Point GetXY(Point[] points, int u, int v)
            {
                var b00 = b0[u] * b0[v];
                var b01 = b0[u] * b1[v];
                var b02 = b0[u] * b2[v];
                var b03 = b0[u] * b3[v];

                var b10 = b1[u] * b0[v];
                var b11 = b1[u] * b1[v];
                var b12 = b1[u] * b2[v];
                var b13 = b1[u] * b3[v];

                var b20 = b2[u] * b0[v];
                var b21 = b2[u] * b1[v];
                var b22 = b2[u] * b2[v];
                var b23 = b2[u] * b3[v];

                var b30 = b3[u] * b0[v];
                var b31 = b3[u] * b1[v];
                var b32 = b3[u] * b2[v];
                var b33 = b3[u] * b3[v];

                var x =
                    points[0].X * b00 +
                    points[1].X * b01 +
                    points[2].X * b02 +
                    points[3].X * b03 +

                    points[11].X * b10 +
                    points[12].X * b11 +
                    points[13].X * b12 +
                    points[4].X * b13 +

                    points[10].X * b20 +
                    points[15].X * b21 +
                    points[14].X * b22 +
                    points[5].X * b23 +

                    points[9].X * b30 +
                    points[8].X * b31 +
                    points[7].X * b32 +
                    points[6].X * b33;

                var y =
                    points[0].Y * b00 +
                    points[1].Y * b01 +
                    points[2].Y * b02 +
                    points[3].Y * b03 +

                    points[11].Y * b10 +
                    points[12].Y * b11 +
                    points[13].Y * b12 +
                    points[4].Y * b13 +

                    points[10].Y * b20 +
                    points[15].Y * b21 +
                    points[14].Y * b22 +
                    points[5].Y * b23 +

                    points[9].Y * b30 +
                    points[8].Y * b31 +
                    points[7].Y * b32 +
                    points[6].Y * b33;

                return new Point(x, y);
            }
        }

        public MeshShading(PdfDictionary definition, CancellationToken cancellationToken) : base(definition, cancellationToken)
        {
            // Function
            var functionDefinition = definition.GetDictionaryOrNull(Names.Function);
            function = Function.Parse(functionDefinition, cancellationToken);

            componentsPerSample = function is IdentityFunction ? ColorSpace.ComponentsPerSample : 1;
            bitsPerComponent = definition.GetValueOrDefault(Names.BitsPerComponent, 8);
            bitsPerCoordinate = definition.GetValueOrDefault(Names.BitsPerCoordinate, 8);
            bitsPerFlag = definition.GetValueOrDefault(Names.BitsPerFlag, 8);

            // Data stream
            var data = ArrayUtils.Empty<byte>();

            if (definition.Stream != null)
            {
                using (var stream = definition.Stream.OpenDecoded(cancellationToken))
                {
                    using var memoryStream = new MemoryStream();

                    stream.CopyTo(memoryStream, cancellationToken);
                    data = memoryStream.ToArray();
                }
            }

            reader = new VariableBitReader(data, 0, data.Length);

            // Decode
            if (definition.TryGetArray<double>(Names.Decode, out var decodeArray) &&
                decodeArray.Length >= 4 + componentsPerSample)
            {
                xDecode = new DecodeRange((float)decodeArray[0], (float)decodeArray[1], bitsPerCoordinate);
                yDecode = new DecodeRange((float)decodeArray[2], (float)decodeArray[3], bitsPerCoordinate);

                var colorDecodeArray = new double[componentsPerSample * 2];
                Array.Copy(decodeArray, 4, colorDecodeArray, 0, colorDecodeArray.Length);
                colorDecode = new DecodeArray(bitsPerComponent, colorDecodeArray);
            }
            else
            {
                colorDecode = new DecodeArray(8, new double[] { 0, 1 });
            }
        }

        protected bool TryReadCoordinate(out Point result)
        {
            var x = (double)reader.ReadLongBits(bitsPerCoordinate);
            var y = (double)reader.ReadLongBits(bitsPerCoordinate);

            if (x >= 0 && y >= 0)
            {
                result = new Point(xDecode.Decode(x), yDecode.Decode(y));
                return true;
            }

            result = default;
            return false;
        }

        protected bool TryReadColor(out float[] components)
        {
            components = new float[componentsPerSample];

            for (var i = 0; i < componentsPerSample; i++)
            {
                var component = reader.ReadBits(bitsPerComponent);
                if (component < 0)
                {
                    return false;
                }

                components[i] = component;
            }

            colorDecode.Decode(components);

            return true;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static float Interpolate(float from, float to, float t)
        {
            return from + (to - from) * t;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void Interpolate(float[] color1, float[] color2, float[] output, float t)
        {
            for (var i = 0; i < output.Length; i++)
            {
                output[i] = Interpolate(color1[i], color2[i], t);
            }
        }

        private byte[] Render(int width, int height, Matrix transform)
        {
            var bitmap = new Bitmap(width, height);

            if (patches.Count > 0)
            {
                var roughMaxPatchSize = patches
                    .Select(patch =>
                    {
                        var bbox = patch.Coordinates
                            .Select(p => transform * p)
                            .Take(12) // Skip middle control points
                            .GetBoundingRectangle();

                        return Math.Max(bbox.Width, bbox.Height);
                    })
                    .Max();

                var approximationResolution = MathUtils.Clamp((int)(0.5 + roughMaxPatchSize / OptimalCellSize), MinApproximationResolution, MaxApproximationResolution);
                var approximation = new Approximation(approximationResolution);

                var colorU0 = new float[componentsPerSample];
                var colorU1 = new float[componentsPerSample];
                var colorUV = new float[componentsPerSample];

                var cellCornerPoints = new Point[approximation.Resolution * approximation.Resolution];

                var dp0 = -approximation.Resolution - 1;
                var dp1 = -approximation.Resolution;
                var dp2 = 0;
                var dp3 = -1;

                foreach (var patch in patches)
                {
                    var cornerPointIndex = 0;

                    // Calculate corners
                    for (var u = 0; u < approximation.Resolution; u++) // x-axis
                    {
                        for (var v = 0; v < approximation.Resolution; v++) // y-axis
                        {
                            cellCornerPoints[cornerPointIndex++] = approximation.GetXY(patch.Coordinates, u, v);
                        }
                    }

                    // Transform points
                    for (var i = 0; i < cellCornerPoints.Length; i++)
                    {
                        cellCornerPoints[i] = transform * cellCornerPoints[i];
                    }

                    // Render polygons
                    cornerPointIndex = approximation.Resolution; // Skip first column

                    for (var u = 1; u < approximation.Resolution; u++) // x-axis
                    {
                        Interpolate(patch.Colors[1], patch.Colors[2], colorU0, (u - 1) * approximation.TimeMultiplier);
                        Interpolate(patch.Colors[0], patch.Colors[3], colorU1, u * approximation.TimeMultiplier);

                        cornerPointIndex++;

                        for (var v = 1; v < approximation.Resolution; v++) // y-axis
                        {
                            Interpolate(colorU1, colorU0, colorUV, v * approximation.TimeMultiplier);

                            var color = GetPolygonColor(colorUV);

                            bitmap.FillPolygon([
                                cellCornerPoints[cornerPointIndex + dp0],
                                cellCornerPoints[cornerPointIndex + dp1],
                                cellCornerPoints[cornerPointIndex + dp2],
                                cellCornerPoints[cornerPointIndex + dp3],
                            ], color);

                            cornerPointIndex++;
                        }
                    }
                }
            }

            // Sub and Up seems to perform best on shadings
            return bitmap.ToPng(PngFilter.Up);
        }

        private string RenderDataUrl(int width, int height, Matrix transform)
        {
            var data = Render(width, height, transform);
            return "data:image/png;base64," + Convert.ToBase64String(data);
        }

        private RgbColor GetPolygonColor(float[] components)
        {
            if (!(function is IdentityFunction))
            {
                var doubleComponents = function.Evaluate(components[0]);

                components = new float[doubleComponents.Length];
                for (var i = 0; i < doubleComponents.Length; i++)
                {
                    components[i] = (float)doubleComponents[i];
                }
            }

            return new RgbColor(ColorSpace, components);
        }

        private Rectangle GetBoundingBox(Matrix transform)
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            static void VisitCurve1D(double p0, double p1, double p2, double p3, ref double min, ref double max)
            {
                // If none of the control points is outside the current min/max range, there is no
                // point in computing the actual bounds of the Bézier curve
                if (p0 < min ||
                    p1 < min ||
                    p2 < min ||
                    p3 < min ||

                    p0 > max ||
                    p1 > max ||
                    p2 > max ||
                    p3 > max)
                {
                    Bezier.GetCubicBounds(p0, p1, p2, p3, out var curveMin, out var curveMax);

                    if (min > curveMin)
                    {
                        min = curveMin;
                    }

                    if (max < curveMax)
                    {
                        max = curveMax;
                    }
                }
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            void VisitCurve2D(Point p0, Point p1, Point p2, Point p3)
            {
                VisitCurve1D(p0.X, p1.X, p2.X, p3.X, ref minX, ref maxX);
                VisitCurve1D(p0.Y, p1.Y, p2.Y, p3.Y, ref minY, ref maxY);
            }

            foreach (var patch in patches)
            {
                // According to Wikipedia, affine transformations can be applied on the control points instead of the computed curve coordinates.
                // https://en.wikipedia.org/wiki/B%C3%A9zier_curve#Computer_graphics

                var transformed0 = transform * patch.Coordinates[0];
                var transformed3 = transform * patch.Coordinates[3];
                var transformed6 = transform * patch.Coordinates[6];
                var transformed9 = transform * patch.Coordinates[9];

                VisitCurve2D(
                    transformed0,
                    transform * patch.Coordinates[1],
                    transform * patch.Coordinates[2],
                    transformed3
                    );

                VisitCurve2D(
                    transformed3,
                    transform * patch.Coordinates[4],
                    transform * patch.Coordinates[5],
                    transformed6
                    );

                VisitCurve2D(
                    transformed6,
                    transform * patch.Coordinates[7],
                    transform * patch.Coordinates[8],
                    transformed9
                    );

                VisitCurve2D(
                    transformed9,
                    transform * patch.Coordinates[10],
                    transform * patch.Coordinates[11],
                    transformed0
                    );
            }

            return new Rectangle(minX, minY, maxX, maxY);
        }

        public override XElement? GetShadingElement(Matrix transform, Rectangle clipRectangle, bool inPattern)
        {
            if (clipRectangle.X2 <= 0 || clipRectangle.Y2 <= 0)
            {
                return null;
            }

            var imageBBox = GetBoundingBox(transform);

            XElement? backgroundEl = null;
            XElement? clipEl = null;
            XAttribute? clipAttribute = null;

            if (BBox != null)
            {
                var bboxCoords = transform * BBox.Value;
                var transformedBbox = bboxCoords.GetBoundingRectangle();

                imageBBox = Rectangle.Intersection(imageBBox, transformedBbox);

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

                var isSkewed = transform.B != 0 || transform.C != 0;
                if (isSkewed)
                {
                    var clipId = StableID.Generate("cl", "MeshClip", bboxPathData);

                    clipEl = new XElement(ns + "clipPath",
                        new XAttribute("id", clipId),
                        new XElement(ns + "path",
                            new XAttribute("d", bboxPathData)
                            ));

                    clipAttribute = new XAttribute("clip-path", "url(#" + clipId + ")");
                }
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

            // It is better to round the measures inwards, to prevent a transparent ghost line, blurring the mesh edge
            // when the image is resized.
            var naturalWidth = (int)imageBBox.X2 - (int)Math.Ceiling(imageBBox.X1);
            var naturalHeight = (int)imageBBox.Y2 - (int)Math.Ceiling(imageBBox.Y1);

            var imageWidth = MathUtils.Clamp(naturalWidth * ResolutionPxPerPt, MinSizePx, MaxSizePx);
            var imageHeight = MathUtils.Clamp(naturalHeight * ResolutionPxPerPt, MinSizePx, MaxSizePx);

            var imageTransform =
                Matrix.Translate(-Math.Ceiling(imageBBox.X1), -Math.Ceiling(imageBBox.Y1)) *
                Matrix.Scale((double)imageWidth / naturalWidth, (double)imageHeight / naturalHeight);

            var imageUrl = RenderDataUrl(imageWidth, imageHeight, transform * imageTransform);

            var patternEl = New.XElement(ns + "pattern",
                new XAttribute("width", SvgConversion.FormatCoordinate(clipRectangle.X2)),
                new XAttribute("height", SvgConversion.FormatCoordinate(clipRectangle.Y2)),
                new XAttribute("patternUnits", "userSpaceOnUse"),

                clipEl,
                backgroundEl,

                New.XElement(ns + "image",
                    clipAttribute,
                    new XAttribute("x", SvgConversion.FormatCoordinate(imageBBox.X1)),
                    new XAttribute("y", SvgConversion.FormatCoordinate(imageBBox.Y1)),
                    new XAttribute("width", SvgConversion.FormatCoordinate(imageBBox.Width)),
                    new XAttribute("height", SvgConversion.FormatCoordinate(imageBBox.Height)),
                    new XAttribute("preserveAspectRatio", "none"),
                    new XAttribute("href", imageUrl)
                    ));

            return patternEl;
        }
    }
}
