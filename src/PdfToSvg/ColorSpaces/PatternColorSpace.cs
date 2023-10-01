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
    internal class PatternColorSpace : ColorSpace
    {
        public PatternColorSpace(ColorSpace alternateSpace)
        {
            AlternateSpace = alternateSpace;
        }

        public override int ComponentsPerSample => 1;

        public override float[] DefaultColor => new float[] { 0 };

        public ColorSpace AlternateSpace { get; }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new float[] { 0, 1 });
        }

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            inputOffset++;
            red = 0;
            green = 0;
            blue = 0;
        }

        public override string ToString() => "Pattern";
    }
}
