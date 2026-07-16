// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.ColorSpaces
{
    /// <summary>
    /// Used to indicate that no color space was specified.
    /// </summary>
    internal class NullColorSpace : ColorSpace
    {
        private readonly ColorSpace substituteColorSpace = new DeviceRgbColorSpace();

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            substituteColorSpace.ToRgb(input, ref inputOffset, out red, out green, out blue);
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent) => substituteColorSpace.GetDefaultDecodeArray(bitsPerComponent);

        public override int ComponentsPerSample => substituteColorSpace.ComponentsPerSample;

        public override float[] DefaultColor => substituteColorSpace.DefaultColor;

        public override int GetHashCode() => 227548204;
        public override bool Equals(object? obj) => obj is NullColorSpace;

        public override string ToString() => "Null color space";
    }
}
