// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal class DeviceRgbColorSpace : ColorSpace, IEquatable<DeviceRgbColorSpace>
    {
        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            red = input[inputOffset++];
            green = input[inputOffset++];
            blue = input[inputOffset++];
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, 1f, 0f, 1f, 0f, 1f });
        }

        public override int ComponentsPerSample => 3;

        public override float[] DefaultColor => new[] { 0f, 0f, 0f };

        public override int GetHashCode() => 691308;
        public bool Equals(DeviceRgbColorSpace? other) => other != null;
        public override bool Equals(object? obj) => obj is DeviceRgbColorSpace;

        public override string ToString() => "RGB";
    }
}
