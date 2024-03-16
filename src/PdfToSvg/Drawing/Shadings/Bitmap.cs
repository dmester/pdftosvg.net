// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Png;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToSvg.Drawing.Shadings
{
    internal class Bitmap
    {
        private const int BytesPerSample = 4;
        private const int RedOffset = 0;
        private const int GreenOffset = 1;
        private const int BlueOffset = 2;
        private const int AlphaOffset = 3;

        private readonly int width;
        private readonly int height;
        private readonly byte[] buffer;

        private const int AlphaRed = 0;
        private const int AlphaGreen = 0;
        private bool[] usedBlueComponents = new bool[256];

        public Bitmap(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));

            this.width = width;
            this.height = height;

            buffer = new byte[height * width * BytesPerSample];
        }

        private struct Edge
        {
            public readonly double X1;
            public readonly double X2;
            public readonly double Y1;
            public readonly double Y2;

            public Edge(double x1, double x2, double y1, double y2)
            {
                X1 = x1;
                X2 = x2;
                Y1 = y1;
                Y2 = y2;
            }

            public double Intersection(double y)
            {
                var dx = (X2 - X1) * (Y1 - y) / (Y1 - Y2);
                return X1 + dx;
            }
        }

        private List<Edge> GetPolygonEdges(Point[] points)
        {
            var edges = new List<Edge>(points.Length);

            if (points.Length > 2)
            {
                for (var i = 1; i < points.Length; i++)
                {
                    var from = points[i - 1];
                    var to = points[i];
                    if (from.Y != to.Y)
                    {
                        edges.Add(new Edge(from.X, to.X, from.Y, to.Y));
                    }
                }

                if (points[0].Y != points[points.Length - 1].Y)
                {
                    var from = points[points.Length - 1];
                    var to = points[0];
                    edges.Add(new Edge(from.X, to.X, from.Y, to.Y));
                }
            }

            return edges;
        }

        private static void PopulateIntersections(List<int> intersections, List<Edge> edges, int y, int width)
        {
            var intersectionLineY = y + 0.5;

            intersections.Clear();

            for (var i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];

                if (edge.Y1 < edge.Y2)
                {
                    if (edge.Y1 > intersectionLineY || edge.Y2 <= intersectionLineY)
                    {
                        continue;
                    }
                }
                else
                {
                    if (edge.Y2 > intersectionLineY || edge.Y1 <= intersectionLineY)
                    {
                        continue;
                    }
                }

                var intersectionX = edge.Intersection(intersectionLineY);
                var intIntersectionX = (int)(intersectionX + 0.5);

                if (intIntersectionX < width)
                {
                    intersections.Add(Math.Max(0, intIntersectionX));
                }
            }

            intersections.Sort();
        }

        private static void ConvertToRgb24(RgbColor color, out byte red, out byte green, out byte blue)
        {
            red = (byte)(color.Red * 255);
            green = (byte)(color.Green * 255);
            blue = (byte)(color.Blue * 255);
        }

        public void FillPolygon(Point[] points, RgbColor color)
        {
            var bbox = points.GetBoundingRectangle();

            var miny = Math.Max(0, (int)(bbox.Y1 + 0.5));
            var maxy = Math.Min(height, (int)(bbox.Y2 + 0.5));

            var edges = GetPolygonEdges(points);
            var intersections = new List<int>();

            ConvertToRgb24(color, out var red, out var green, out var blue);

            if (red == AlphaRed && green == AlphaGreen)
            {
                usedBlueComponents[blue] = true;
            }

            for (var y = miny; y < maxy; y++)
            {
                var rowOffset = y * width * BytesPerSample;

                PopulateIntersections(intersections, edges, y, width);

                for (var i = 0; i < intersections.Count; i += 2)
                {
                    var from = intersections[i];
                    var to = i + 1 < intersections.Count ? intersections[i + 1] : width;
                    var pixelOffset = rowOffset + from * BytesPerSample;

                    for (var x = from; x < to; x++)
                    {
                        buffer[pixelOffset + RedOffset] = red;
                        buffer[pixelOffset + GreenOffset] = green;
                        buffer[pixelOffset + BlueOffset] = blue;
                        buffer[pixelOffset + AlphaOffset] = 255;
                        pixelOffset += BytesPerSample;
                    }
                }
            }
        }

        public byte[] ToPng(PngFilter filter)
        {
            var alphaBlue = -1;
            for (var i = 0; i < usedBlueComponents.Length; i++)
            {
                if (usedBlueComponents[i] == false)
                {
                    alphaBlue = i;
                    break;
                }
            }

            if (alphaBlue < 0)
            {
                return PngEncoder.TruecolourWithAlpha(buffer, width, height, filter);
            }
            else
            {
                return PngEncoder.Truecolour(buffer, width, height, filter, AlphaRed, AlphaGreen, alphaBlue);
            }
        }
    }
}
