// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Functions;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal class DeviceNColorSpace : ColorSpace
    {
        // Implementation limitations:
        //
        // DeviceN has a lot of currently unsupported features. No PDF reader except Acrobat seems to have implemented
        // many of the special cases.
        //
        // NChannel is not supported, but should be backward compatible with DeviceN.
        //
        // The `name` parameter does not affect generated colors.

        private readonly Function tintTransform;

        public DeviceNColorSpace(int componentsPerSample, ColorSpace alternateSpace, Function tintTransform)
        {
            this.ComponentsPerSample = componentsPerSample;
            this.AlternateSpace = alternateSpace;
            this.tintTransform = tintTransform;
        }

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            var components = new double[ComponentsPerSample];
            for (var i = 0; i < components.Length; i++)
            {
                components[i] = input[inputOffset++];
            }

            var output = tintTransform.Evaluate(components);
            var floatOutput = new float[output.Length];

            for (var i = 0; i < output.Length; i++)
            {
                floatOutput[i] = (float)output[i];
            }

            AlternateSpace.ToRgb(floatOutput, out red, out green, out blue);
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            // PDF spec 1.7, Table 90
            var decodeArray = new float[ComponentsPerSample * 2];

            for (var i = 0; i < decodeArray.Length; i += 2)
            {
                decodeArray[i + 0] = 0f;
                decodeArray[i + 1] = 1f;
            }

            return new DecodeArray(bitsPerComponent, decodeArray);
        }

        public override int ComponentsPerSample { get; }

        public ColorSpace AlternateSpace { get; }

        public override float[] DefaultColor => new float[ComponentsPerSample];

        public override int GetHashCode() =>
            895666034 ^
            ComponentsPerSample ^
            AlternateSpace.GetHashCode() ^
            tintTransform.GetHashCode();

        public override bool Equals(object obj) =>
            obj is DeviceNColorSpace colorSpace &&
            colorSpace.ComponentsPerSample == ComponentsPerSample &&
            colorSpace.AlternateSpace.Equals(AlternateSpace) &&
            ReferenceEquals(colorSpace.tintTransform, tintTransform);

        public override string ToString() => "DeviceN N=" + ComponentsPerSample + " " + AlternateSpace;
    }
}
