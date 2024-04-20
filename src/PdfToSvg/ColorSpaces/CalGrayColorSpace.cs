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
    internal class CalGrayColorSpace : ColorSpace
    {
        private const double DefaultGamma = 1;
        private readonly double gamma;

        public CalGrayColorSpace(double? gamma)
        {
            if (gamma == null)
            {
                this.gamma = DefaultGamma;
            }
            else if (gamma > 0)
            {
                this.gamma = gamma.Value;
            }
            else
            {
                this.gamma = DefaultGamma;
                Log.WriteLine("Incorrect gamma value {0} passed to /CalGray color space. The value is ignored.", gamma);
            }
        }

        public override int ComponentsPerSample => 1;

        public override float[] DefaultColor => new float[] { 0 };

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            // PDF spec 1.7, Table 90
            return new DecodeArray(bitsPerComponent, new float[] { 0, 1 });
        }

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            var a = input[inputOffset++];

            if (gamma != DefaultGamma)
            {
                a = (float)Math.Pow(a, gamma);
            }

            a = ColorConversion.LinearRgbToSRgb(a);

            red = a;
            green = a;
            blue = a;
        }

        public override int GetHashCode() =>
           1182521745 ^
           gamma.GetHashCode();

        public override bool Equals(object obj) =>
            obj is CalGrayColorSpace colorSpace &&
            colorSpace.gamma == gamma;
    }
}
