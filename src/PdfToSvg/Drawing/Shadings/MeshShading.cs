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

        private const int MinSizePx = 100;
        private const int MaxSizePx = 600;
        private const int ResolutionPxPerPt = 2;

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

        private const int ApproximationResolution = 80;
        private const float TimeMultiplier = 1.0f / (ApproximationResolution - 1);

        private static readonly float[] b0 = new float[ApproximationResolution];
        private static readonly float[] b1 = new float[ApproximationResolution];
        private static readonly float[] b2 = new float[ApproximationResolution];
        private static readonly float[] b3 = new float[ApproximationResolution];

        static MeshShading()
        {
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

        private static Point GetXY(Point[] points, int u, int v)
        {
            var x =
                points[0].X * b0[u] * b0[v] +
                points[1].X * b0[u] * b1[v] +
                points[2].X * b0[u] * b2[v] +
                points[3].X * b0[u] * b3[v] +

                points[11].X * b1[u] * b0[v] +
                points[12].X * b1[u] * b1[v] +
                points[13].X * b1[u] * b2[v] +
                points[4].X * b1[u] * b3[v] +

                points[10].X * b2[u] * b0[v] +
                points[15].X * b2[u] * b1[v] +
                points[14].X * b2[u] * b2[v] +
                points[5].X * b2[u] * b3[v] +

                points[9].X * b3[u] * b0[v] +
                points[8].X * b3[u] * b1[v] +
                points[7].X * b3[u] * b2[v] +
                points[6].X * b3[u] * b3[v];

            var y =
                points[0].Y * b0[u] * b0[v] +
                points[1].Y * b0[u] * b1[v] +
                points[2].Y * b0[u] * b2[v] +
                points[3].Y * b0[u] * b3[v] +

                points[11].Y * b1[u] * b0[v] +
                points[12].Y * b1[u] * b1[v] +
                points[13].Y * b1[u] * b2[v] +
                points[4].Y * b1[u] * b3[v] +

                points[10].Y * b2[u] * b0[v] +
                points[15].Y * b2[u] * b1[v] +
                points[14].Y * b2[u] * b2[v] +
                points[5].Y * b2[u] * b3[v] +

                points[9].Y * b3[u] * b0[v] +
                points[8].Y * b3[u] * b1[v] +
                points[7].Y * b3[u] * b2[v] +
                points[6].Y * b3[u] * b3[v];

            return new Point(x, y);
        }

        private byte[] Render(int width, int height, Matrix transform)
        {
            var bitmap = new Bitmap(width, height);

            foreach (var patch in patches)
            {
                var colorU0 = new float[componentsPerSample];
                var colorU1 = new float[componentsPerSample];
                var colorUV = new float[componentsPerSample];

                for (var u = 1; u < ApproximationResolution; u++) // x-axis
                {
                    Interpolate(patch.Colors[1], patch.Colors[2], colorU0, (u - 1) * TimeMultiplier);
                    Interpolate(patch.Colors[0], patch.Colors[3], colorU1, u * TimeMultiplier);

                    for (var v = 1; v < ApproximationResolution; v++) // y-axis
                    {
                        Interpolate(colorU1, colorU0, colorUV, v * TimeMultiplier);

                        var p0 = GetXY(patch.Coordinates, u - 1, v - 1);
                        var p1 = GetXY(patch.Coordinates, u - 1, v);
                        var p2 = GetXY(patch.Coordinates, u, v);
                        var p3 = GetXY(patch.Coordinates, u, v - 1);

                        var color = GetPolygonColor(colorUV);

                        bitmap.FillPolygon(new[]
                        {
                            transform * p0,
                            transform * p1,
                            transform * p2,
                            transform * p3
                        }, color);
                    }
                }
            }

            using (var pngStream = new MemoryStream())
            {
                var pngWriter = new PngEncoder(pngStream);

                pngWriter.WriteSignature();
                pngWriter.WriteImageHeader(width, height, PngColorType.TruecolourWithAlpha, bitDepth: 8);

                using (var pngDataStream = pngWriter.GetImageDataStream())
                {
                    pngDataStream.Write(bitmap.GetPngData());
                }

                pngWriter.WriteImageEnd();

                return pngStream.ToArray();
            }
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

        private IEnumerable<Point> GetOutline()
        {
            foreach (var patch in patches)
            {
                for (var u = 0; u < ApproximationResolution; u++) // x-axis
                {
                    yield return GetXY(patch.Coordinates, u, 0);
                }

                for (var v = 1; v < ApproximationResolution; v++) // y-axis
                {
                    yield return GetXY(patch.Coordinates, ApproximationResolution - 1, v);
                }

                for (var u = ApproximationResolution - 2; u >= 0; u--) // x-axis
                {
                    yield return GetXY(patch.Coordinates, u, ApproximationResolution - 1);
                }

                for (var v = ApproximationResolution - 2; v >= 0; v--) // y-axis
                {
                    yield return GetXY(patch.Coordinates, 0, v);
                }
            }
        }

        public override XElement? GetShadingElement(Matrix transform, Rectangle clipRectangle, bool inPattern)
        {
            var imageBBox = GetOutline().Select(p => transform * p).GetBoundingRectangle();

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

            var patternEl = new XElement(ns + "pattern",
                new XAttribute("width", SvgConversion.FormatCoordinate(clipRectangle.X2)),
                new XAttribute("height", SvgConversion.FormatCoordinate(clipRectangle.Y2)),
                new XAttribute("patternUnits", "userSpaceOnUse"),

                clipEl,
                backgroundEl,

                new XElement(ns + "image",
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
