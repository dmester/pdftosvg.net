// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Drawing.Shadings
{
    internal class Patch
    {
        public readonly Point[] Coordinates;
        public readonly float[][] Colors;

        public Patch(Point[] coordinates, float[][] colors)
        {
            switch (coordinates.Length)
            {
                case 3:
                    if (colors.Length != 3)
                    {
                        throw new ArgumentOutOfRangeException(nameof(colors));
                    }

                    Coordinates = new[]
                    {
                        coordinates[0], ControlPoint(coordinates[0], coordinates[1]),
                        ControlPoint(coordinates[1], coordinates[0]), coordinates[1], ControlPoint(coordinates[1], coordinates[2]),
                        ControlPoint(coordinates[2], coordinates[1]), coordinates[2], ControlPoint(coordinates[2], coordinates[0]),
                        ControlPoint(coordinates[0], coordinates[2]), coordinates[0], coordinates[0], coordinates[0],
                        default, default, default, default,
                    };
                    Colors = new[]
                    {
                        colors[0],
                        colors[1],
                        colors[2],
                        colors[0],
                    };

                    CalculateMiddleCoords();
                    break;

                case 4:
                    if (colors.Length != 4)
                    {
                        throw new ArgumentOutOfRangeException(nameof(colors));
                    }

                    Coordinates = new[]
                    {
                        coordinates[0], ControlPoint(coordinates[0], coordinates[1]),
                        ControlPoint(coordinates[1], coordinates[0]), coordinates[1], ControlPoint(coordinates[1], coordinates[2]),
                        ControlPoint(coordinates[2], coordinates[1]), coordinates[2], ControlPoint(coordinates[2], coordinates[3]),
                        ControlPoint(coordinates[3], coordinates[2]), coordinates[3], ControlPoint(coordinates[3], coordinates[0]),
                        ControlPoint(coordinates[0], coordinates[3]),
                        default, default, default, default,
                    };
                    Colors = (float[][])colors.Clone();

                    CalculateMiddleCoords();
                    break;

                case 12:
                    if (colors.Length != 4)
                    {
                        throw new ArgumentOutOfRangeException(nameof(colors));
                    }

                    Coordinates = new Point[16];
                    Array.Copy(coordinates, Coordinates, 12);
                    Colors = (float[][])colors.Clone();

                    CalculateMiddleCoords();
                    break;

                case 16:
                    if (colors.Length != 4)
                    {
                        throw new ArgumentOutOfRangeException(nameof(colors));
                    }

                    Coordinates = (Point[])coordinates.Clone();
                    Colors = (float[][])colors.Clone();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(coordinates));
            }
        }

        private static Point ControlPoint(Point from, Point to)
        {
            const double FromWeight = 0.66;
            return new Point(from.X * FromWeight + to.X * (1 - FromWeight), from.Y * FromWeight + to.Y * (1 - FromWeight));
        }

        private void CalculateMiddleCoords()
        {
            // p11
            Coordinates[12] = new Point(
                (1.0 / 9) * (-4 * Coordinates[0].X + 6 * (Coordinates[1].X + Coordinates[11].X) - 2 * (Coordinates[3].X + Coordinates[9].X) + 3 * (Coordinates[8].X + Coordinates[4].X) - 1 * Coordinates[6].X),
                (1.0 / 9) * (-4 * Coordinates[0].Y + 6 * (Coordinates[1].Y + Coordinates[11].Y) - 2 * (Coordinates[3].Y + Coordinates[9].Y) + 3 * (Coordinates[8].Y + Coordinates[4].Y) - 1 * Coordinates[6].Y)
                );

            // p12
            Coordinates[13] = new Point(
                (1.0 / 9) * (-4 * Coordinates[3].X + 6 * (Coordinates[2].X + Coordinates[4].X) - 2 * (Coordinates[0].X + Coordinates[6].X) + 3 * (Coordinates[7].X + Coordinates[11].X) - 1 * Coordinates[9].X),
                (1.0 / 9) * (-4 * Coordinates[3].Y + 6 * (Coordinates[2].Y + Coordinates[4].Y) - 2 * (Coordinates[0].Y + Coordinates[6].Y) + 3 * (Coordinates[7].Y + Coordinates[11].Y) - 1 * Coordinates[9].Y)
                );

            // p22
            Coordinates[14] = new Point(
                (1.0 / 9) * (-4 * Coordinates[6].X + 6 * (Coordinates[7].X + Coordinates[5].X) - 2 * (Coordinates[9].X + Coordinates[3].X) + 3 * (Coordinates[2].X + Coordinates[10].X) - 1 * Coordinates[0].X),
                (1.0 / 9) * (-4 * Coordinates[6].Y + 6 * (Coordinates[7].Y + Coordinates[5].Y) - 2 * (Coordinates[9].Y + Coordinates[3].Y) + 3 * (Coordinates[2].Y + Coordinates[10].Y) - 1 * Coordinates[0].Y)
                );

            // p21
            Coordinates[15] = new Point(
                (1.0 / 9) * (-4 * Coordinates[9].X + 6 * (Coordinates[8].X + Coordinates[10].X) - 2 * (Coordinates[6].X + Coordinates[0].X) + 3 * (Coordinates[1].X + Coordinates[5].X) - 1 * Coordinates[3].X),
                (1.0 / 9) * (-4 * Coordinates[9].Y + 6 * (Coordinates[8].Y + Coordinates[10].Y) - 2 * (Coordinates[6].Y + Coordinates[0].Y) + 3 * (Coordinates[1].Y + Coordinates[5].Y) - 1 * Coordinates[3].Y));
        }
    }
}
