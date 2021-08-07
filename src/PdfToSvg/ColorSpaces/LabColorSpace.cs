// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg.ColorSpaces
{
    internal class LabColorSpace : ColorSpace
    {
        private readonly Matrix3x3 transform;
        private readonly float amin, amax, bmin, bmax;

        public LabColorSpace(Matrix1x3? whitePoint, double[]? range)
        {
            if (range != null && range.Length > 3)
            {
                amin = (float)range[0];
                amax = (float)range[1];
                bmin = (float)range[2];
                bmax = (float)range[3];
            }
            else
            {
                amin = -100;
                amax = 100;
                bmin = -100;
                bmax = 100;
            }

            var inputWhite = new Matrix1x3(1, 1, 1);

            if (whitePoint == null)
            {
                transform =
                    ChromaticAdaption.XyzScalingTransform(inputWhite, ColorConversion.ReferenceWhiteD65);
            }
            else
            {
                transform =
                    ChromaticAdaption.BradfordTransform(whitePoint.Value, ColorConversion.ReferenceWhiteD65) *
                    ChromaticAdaption.XyzScalingTransform(inputWhite, whitePoint.Value);
            }

            transform = ColorConversion.CieXyzD65ToLinearRgb * transform;
        }

        public override int ComponentsPerSample => 3;

        public override float[] DefaultColor => new[] { 0f, 0f, 0f };

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            // PDF spec 1.7, Table 90
            return new DecodeArray(bitsPerComponent, new[] { 0f, 100f, amin, amax, bmin, bmax });
        }

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            // PDF spec 1.7, Page 157
            var Lx = input[inputOffset++];
            var ax = MathUtils.Clamp(input[inputOffset++], amin, amax);
            var bx = MathUtils.Clamp(input[inputOffset++], bmin, bmax);

            var M = (Lx + 16) / 116f;
            var L = M + ax / 500f;
            var N = M - bx / 200f;

            Matrix1x3 xyz;

            xyz.M11 = g(L);
            xyz.M21 = g(M);
            xyz.M31 = g(N);

            var rgb = transform * xyz;

            red = ColorConversion.LinearRgbToSRgb(rgb.M11);
            green = ColorConversion.LinearRgbToSRgb(rgb.M21);
            blue = ColorConversion.LinearRgbToSRgb(rgb.M31);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static float g(float x)
        {
            return x >= 6f / 29f
                ? x * x * x
                : (108f / 841f) * (x - (4f / 29f));
        }
    }
}
