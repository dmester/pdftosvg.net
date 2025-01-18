// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg.Drawing
{
    internal static class Bezier
    {
        /// <summary>
        /// Computes the value along a cubic Bézier curve in one dimension.
        /// </summary>
        /// <param name="p0">Start point</param>
        /// <param name="p1">Control point 1</param>
        /// <param name="p2">Control point 2</param>
        /// <param name="p3">End point</param>
        /// <param name="t">Position along the curve [0, 1]</param>
        public static double ComputeCubic(double p0, double p1, double p2, double p3, double t)
        {
            return ComputeCubicCore(p0, p1, p2, p3, t);
        }

        /// <summary>
        /// Computes the value along a cubic Bézier curve in two dimensions.
        /// </summary>
        /// <param name="p0">Start point</param>
        /// <param name="p1">Control point 1</param>
        /// <param name="p2">Control point 2</param>
        /// <param name="p3">End point</param>
        /// <param name="t">Position along the curve [0, 1]</param>
        public static Point ComputeCubic(Point p0, Point p1, Point p2, Point p3, double t)
        {
            return new Point(
                ComputeCubicCore(p0.X, p1.X, p2.X, p3.X, t),
                ComputeCubicCore(p0.Y, p1.Y, p2.Y, p3.Y, t)
                );
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static double ComputeCubicCore(double p0, double p1, double p2, double p3, double t)
        {
            var complementT = 1 - t;
            var complementT2 = complementT * complementT;
            var t2 = t * t;

            return
                complementT * complementT2 * p0 +
                3 * complementT2 * t * p1 +
                3 * complementT * t2 * p2 +
                t2 * t * p3;
        }

        /// <summary>
        /// Gets the minimum and maximum value for a cubic Bézier curve in one dimension.
        /// </summary>
        /// <param name="p0">Start point</param>
        /// <param name="p1">Control point 1</param>
        /// <param name="p2">Control point 2</param>
        /// <param name="p3">End point</param>
        /// <param name="min">Computed minimum value</param>
        /// <param name="max">Computed maximum value</param>
        public static void GetCubicBounds(double p0, double p1, double p2, double p3, out double min, out double max)
        {
            // Algorithm:
            //
            //   Stack Exchange / Pikalek
            //   https://gamedev.stackexchange.com/a/164816
            //
            //   Inigo Quilez
            //   https://iquilezles.org/articles/bezierbbox/
            //
            // Math:
            //
            // f(t) =
            //
            //    (1 - t)^3 * p0 +
            //    3 * (1 - t)^2 * t * p1 +
            //    3 * (1 - t) * t^2 * p2 +
            //    t^3 * p3 =
            //    
            //    (1 - 3*t + 3*t^2 - t^3) * p0 +
            //    (3*t - 6*t^2 + 3*t^3) * p1 +
            //    (3*t^2 - 3*t^3) * p2 +
            //    (t^3) * p3 =
            //
            //    (-p0 + 3*p1 - 3*p2 + p3) * t^3 +
            //    (3*p0 - 6*p1 + 3*p2) * t^2 +
            //    (-3*p0 + 3*p1) * t +
            //    (p0) +
            //    
            // f'(t) =
            //    3 * (-p0 + 3*p1 - 3*p2 + p3) * t^2 +
            //    2 * (3*p0 - 6*p1 + 3*p2) * t +
            //    (-3*p0 + 3*p1)
            //
            // Solving t where derivative is 0
            //
            // 0 = 
            //    3 * (-p0 + 3*p1 - 3*p2 + p3) * t^2 +
            //    2 * (3*p0 - 6*p1 + 3*p2) * t +
            //    (-3*p0 + 3*p1)
            //
            // Dividing by 3 => 
            //
            // 0 = 
            //    (-p0 + 3*p1 - 3*p2 + p3) * t^2 +
            //    2 * (p0 - 2*p1 + p2) * t +
            //    (-p0 + p1) 
            //
            // Quadratic formula:
            //    if
            //    at^2 +  bt + c = 0   =>   t = (-b ± sqrt(b^2 - 4ac)) / 2a
            //
            //    then
            //    at^2 + 2bt + c = 0   =>   t = (-2b ± sqrt(4b^2 - 4ac)) / 2a = (-b ± sqrt(b^2 - ac)) / a
            //
            //    if
            //    a = 0 
            //
            //    then
            //    0 * t^2 + 2bt + c = 0   =>   t = -c / 2b
            //
            // With:
            //    a = -p0 + 3*p1 - 3*p2 + p3
            //    b = p0 - 2*p1 + p2
            //    c = -p0 + p1
            //

            if (p0 < p3)
            {
                min = p0;
                max = p3;
            }
            else
            {
                min = p3;
                max = p0;
            }

            var a = 3 * p1 - p0 - 3 * p2 + p3;
            var b = p0 - 2 * p1 + p2;
            var c = p1 - p0;

            if (a == 0)
            {
                if (b != 0)
                {
                    var t = -c / (2 * b);
                    if (t > 0 && t < 1)
                    {
                        var val = ComputeCubicCore(p0, p1, p2, p3, t);

                        if (val < min)
                        {
                            min = val;
                        }
                        else if (val > max)
                        {
                            max = val;
                        }
                    }
                }
            }
            else
            {
                var sqrtValue = b * b - a * c;
                if (sqrtValue >= 0)
                {
                    var sqrt = Math.Sqrt(sqrtValue);

                    var ta = (-b + sqrt) / a;
                    if (ta > 0 && ta < 1)
                    {
                        var val = ComputeCubicCore(p0, p1, p2, p3, ta);

                        if (val < min)
                        {
                            min = val;
                        }
                        else if (val > max)
                        {
                            max = val;
                        }
                    }

                    var tb = (-b - sqrt) / a;
                    if (tb > 0 && tb < 1)
                    {
                        var val = ComputeCubicCore(p0, p1, p2, p3, tb);

                        if (val < min)
                        {
                            min = val;
                        }
                        else if (val > max)
                        {
                            max = val;
                        }
                    }
                }
            }
        }

    }
}
