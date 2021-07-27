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
    // Source for the conversion formulas and constants:
    // https://www.color.org/chardata/rgb/sRGB.pdf

    internal static class ColorConversion
    {
        private const float RelativeReferenceWhiteD65X = 0.3127f;
        private const float RelativeReferenceWhiteD65Y = 0.3290f;
        private const float RelativeReferenceWhiteD65Z = 0.3583f;

        private const float ReferenceWhiteD65X = RelativeReferenceWhiteD65X / RelativeReferenceWhiteD65Y;
        private const float ReferenceWhiteD65Y = RelativeReferenceWhiteD65Y / RelativeReferenceWhiteD65Y;
        private const float ReferenceWhiteD65Z = RelativeReferenceWhiteD65Z / RelativeReferenceWhiteD65Y;

        public static Matrix1x3 ReferenceWhiteD65 => new
        (
            ReferenceWhiteD65X,
            ReferenceWhiteD65Y,
            ReferenceWhiteD65Z
        );

        public static Matrix3x3 CieXyzD65ToLinearRgb => new
        (
            +3.2406255f, -1.5372080f, -0.4986286f,
            -0.9689307f, +1.8757561f, +0.0415175f,
            +0.0557101f, -0.2040211f, +1.0569959f
        );

#if HAVE_AGGRESSIVE_INLINING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static float LinearRgbToSRgb(float v)
        {
            return v <= 0.0031308f
                ? 12.92f * v
                : (float)(1.055 * Math.Pow(v, 1 / 2.4) - 0.055);
        }
    }
}
