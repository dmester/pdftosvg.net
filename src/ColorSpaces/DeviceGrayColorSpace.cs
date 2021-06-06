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
    internal class DeviceGrayColorSpace : ColorSpace, IEquatable<DeviceGrayColorSpace>
    {
        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            var value = input[inputOffset++];
            red = value;
            green = value;
            blue = value;
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, 1f });
        }

        public override int ComponentsPerSample => 1;

        public override float[] DefaultColor => new[] { 0f };

        public override int GetHashCode() => 975312;
        public bool Equals(DeviceGrayColorSpace other) => other != null;
        public override bool Equals(object obj) => obj is DeviceGrayColorSpace;

        public override string ToString() => "Gray";
    }
}
