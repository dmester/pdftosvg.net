// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.ColorSpaces
{
    internal class CalRgbColorSpace : ColorSpace
    {
        private const double DefaultGamma = 1;

        private readonly Matrix3x3 transform;
        private readonly double[]? gamma;

        public CalRgbColorSpace(Matrix1x3? whitePoint, double[]? gamma, Matrix3x3? matrix)
        {
            if (gamma != null &&
                gamma.Length >= ComponentsPerSample &&
                gamma.Take(ComponentsPerSample).Any(g => g != DefaultGamma))
            {
                this.gamma = gamma;
            }

            transform = Matrix3x3.Identity;

            if (matrix != null)
            {
                transform = matrix.Value * transform;
            }

            if (whitePoint != null)
            {
                transform = ChromaticAdaption.BradfordTransform(whitePoint.Value, ColorConversion.ReferenceWhiteD65) * transform;
            }

            transform = ColorConversion.CieXyzD65ToLinearRgb * transform;
        }

        public override int ComponentsPerSample => 3;

        public override float[] DefaultColor => new float[] { 0, 0, 0 };

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            // PDF spec 1.7, Table 90
            return new DecodeArray(bitsPerComponent, new float[] { 0, 1, 0, 1, 0, 1 });
        }

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            Matrix1x3 abc;

            abc.M11 = input[inputOffset++];
            abc.M21 = input[inputOffset++];
            abc.M31 = input[inputOffset++];

            if (gamma != null)
            {
                abc.M11 = (float)Math.Pow(abc.M11, gamma[0]);
                abc.M21 = (float)Math.Pow(abc.M21, gamma[1]);
                abc.M31 = (float)Math.Pow(abc.M31, gamma[2]);
            }

            var rgb = transform * abc;

            red = ColorConversion.LinearRgbToSRgb(rgb.M11);
            green = ColorConversion.LinearRgbToSRgb(rgb.M21);
            blue = ColorConversion.LinearRgbToSRgb(rgb.M31);
        }

        public override int GetHashCode() =>
           1721897427 ^
           transform.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is CalRgbColorSpace colorSpace &&
            (colorSpace.gamma ?? ArrayUtils.Empty<double>()).SequenceEqual(gamma ?? ArrayUtils.Empty<double>()) &&
            colorSpace.transform == transform;
    }
}
