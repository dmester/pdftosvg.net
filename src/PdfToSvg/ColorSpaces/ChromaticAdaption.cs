// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg.ColorSpaces
{
    internal static class ChromaticAdaption
    {
        // Constants from
        // http://www.brucelindbloom.com/index.html?Eqn_ChromAdapt.html

        private static readonly Matrix3x3 bradford = new
        (
            +0.8951f, +0.2664f, -0.1614f,
            -0.7502f, +1.7135f, +0.0367f,
            +0.0389f, -0.0685f, +1.0296f
        );

        private static readonly Matrix3x3 bradfordInverse = new
        (
            +0.9870f, -0.1471f, 0.1600f,
            +0.4323f, +0.5184f, 0.0493f,
            -0.0085f, +0.0400f, 0.9685f
        );

        public static Matrix3x3 BradfordTransform(Matrix1x3 srcWhitePoint, Matrix1x3 dstWhitePoint)
        {
            // Implementation based on
            // http://www.brucelindbloom.com/index.html?Eqn_ChromAdapt.html

            var convSrcWhite = bradford * srcWhitePoint;
            var convDstWhite = bradford * dstWhitePoint;

            var adt = XyzScalingTransform(convSrcWhite, convDstWhite);

            return bradfordInverse * adt * bradford;
        }

        public static Matrix3x3 XyzScalingTransform(Matrix1x3 srcWhitePoint, Matrix1x3 dstWhitePoint)
        {
            return new Matrix3x3
            (
                dstWhitePoint.M11 / srcWhitePoint.M11, 0, 0,
                0, dstWhitePoint.M21 / srcWhitePoint.M21, 0,
                0, 0, dstWhitePoint.M31 / srcWhitePoint.M31
            );
        }
    }
}
