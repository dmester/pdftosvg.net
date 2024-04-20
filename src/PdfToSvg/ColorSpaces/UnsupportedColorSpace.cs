// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal class UnsupportedColorSpace : ColorSpace, IEquatable<UnsupportedColorSpace?>
    {
        private readonly PdfName name;
        private readonly ColorSpace substituteColorSpace = new DeviceRgbColorSpace();

        public UnsupportedColorSpace(PdfName name)
        {
            this.name = name;
        }

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            substituteColorSpace.ToRgb(input, ref inputOffset, out red, out green, out blue);
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent) => substituteColorSpace.GetDefaultDecodeArray(bitsPerComponent);

        public override int ComponentsPerSample => substituteColorSpace.ComponentsPerSample;

        public override float[] DefaultColor => substituteColorSpace.DefaultColor;

        public string Name => name.Value;

        public override int GetHashCode() => name.GetHashCode();
        public bool Equals(UnsupportedColorSpace? other) => name == other?.name;
        public override bool Equals(object? obj) => Equals(obj as UnsupportedColorSpace);

        public override string ToString() => "Unsupported color space: " + name;

    }
}
