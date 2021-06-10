// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Functions;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal class SeparationColorSpace : ColorSpace
    {
        private readonly Function tintTransform;

        public SeparationColorSpace(ColorSpace alternateSpace, Function tintTransform)
        {
            this.AlternateSpace = alternateSpace;
            this.tintTransform = tintTransform;
        }

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            var tint = input[inputOffset++];

            var output = tintTransform.Evaluate(tint);
            var floatOutput = new float[output.Length];

            for (var i = 0; i < output.Length; i++)
            {
                floatOutput[i] = (float)output[i];
            }

            AlternateSpace.ToRgb(floatOutput, out red, out green, out blue);
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, 1f });
        }

        public override int ComponentsPerSample => 1;

        public ColorSpace AlternateSpace { get; }

        public override float[] DefaultColor => new[] { 0f };

        public override string ToString() => "Separation " + AlternateSpace;
    }
}
