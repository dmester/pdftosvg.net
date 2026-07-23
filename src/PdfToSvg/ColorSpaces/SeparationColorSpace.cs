// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Functions;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal class SeparationColorSpace : ColorSpace
    {
        // Implementation limitation:
        // The `name` parameter of the colorspace does not affect generated colors.

        private readonly Function tintTransform;

        public SeparationColorSpace(ColorSpace alternateSpace, Function tintTransform)
        {
            this.AlternateSpace = alternateSpace;
            this.tintTransform = tintTransform;
        }

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            var output = new float[tintTransform.OutputCount];

            tintTransform.Evaluate(input, inputOffset, output, 0);
            AlternateSpace.ToRgb(output, out red, out green, out blue);

            inputOffset++;
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, 1f });
        }

        public override int ComponentsPerSample => 1;

        public ColorSpace AlternateSpace { get; }

        public override float[] DefaultColor => new[] { 0f };

        public override string ToString() => "Separation " + AlternateSpace;

        public override int GetHashCode() =>
            1388104043 ^
            AlternateSpace.GetHashCode() ^
            tintTransform.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is SeparationColorSpace colorSpace &&
            colorSpace.AlternateSpace.Equals(AlternateSpace) &&
            colorSpace.tintTransform.Equals(tintTransform);
    }
}
