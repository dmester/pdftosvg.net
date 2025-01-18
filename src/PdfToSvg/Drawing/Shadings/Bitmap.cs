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

        private static void PopulateIntersections(List<int> intersections, Point[] points, int y, int width)
        {
            var intersectionLineY = y + 0.5;

            intersections.Clear();

            Point p1, p2;

            for (var i = 0; i < points.Length; i++)
            {
                if (i == 0)
                {
                    p1 = points[points.Length - 1];
                    p2 = points[0];
                }
                else
                {
                    p1 = points[i - 1];
                    p2 = points[i];
                }

                if (p1.Y == p2.Y)
                {
                    continue;
                }

                if (p1.Y < p2.Y)
                {
                    if (p1.Y > intersectionLineY || p2.Y <= intersectionLineY)
                    {
                        continue;
                    }
                }
                else
                {
                    if (p2.Y > intersectionLineY || p1.Y <= intersectionLineY)
                    {
                        continue;
                    }
                }

                var dx = (p2.X - p1.X) * (p1.Y - intersectionLineY) / (p1.Y - p2.Y);

                var intersectionX = p1.X + dx;
                var intIntersectionX = (int)(intersectionX + 0.5);

                if (intIntersectionX < width)
                {
                    if (intIntersectionX < 0)
                    {
                        intIntersectionX = 0;
                    }

                    intersections.Add(intIntersectionX);
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
            if (points.Length < 2)
            {
                return;
            }

            var dminy = points[0].Y;
            var dmaxy = dminy;

            for (var i = 1; i < points.Length; i++)
            {
                var pointY = points[i].Y;

                if (dminy > pointY)
                {
                    dminy = pointY;
                }
                else if (dmaxy < pointY)
                {
                    dmaxy = pointY;
                }
            }

            var miny = Math.Max(0, (int)(dminy + 0.5));
            var maxy = Math.Min(height, (int)(dmaxy + 0.5));

            var intersections = new List<int>();

            ConvertToRgb24(color, out var red, out var green, out var blue);

            if (red == AlphaRed && green == AlphaGreen)
            {
                usedBlueComponents[blue] = true;
            }

            for (var y = miny; y < maxy; y++)
            {
                var rowOffset = y * width * BytesPerSample;

                PopulateIntersections(intersections, points, y, width);

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
