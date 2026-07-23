// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Functions;
using PdfToSvg.Imaging;

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
            var output = new float[tintTransform.OutputCount];

            tintTransform.Evaluate(input, inputOffset, output, outputOffset: 0);
            AlternateSpace.ToRgb(output, out red, out green, out blue);

            inputOffset += ComponentsPerSample;
        }

        public override void ToRgb8(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            var componentsPerSample = ComponentsPerSample;
            var output = new float[tintTransform.OutputCount];

            for (var i = 0; i < sampleCount; i++)
            {
                tintTransform.Evaluate(input, inputOffset, output, 0);
                AlternateSpace.ToRgb(output, out var red, out var green, out var blue);

                rgbBuffer[rgbBufferOffset + 0] = ToRgb8Component(red);
                rgbBuffer[rgbBufferOffset + 1] = ToRgb8Component(green);
                rgbBuffer[rgbBufferOffset + 2] = ToRgb8Component(blue);

                rgbBufferOffset += 3;
                inputOffset += componentsPerSample;
            }
        }

        public override void ToRgb8(float[] input, int inputOffset, float[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            var componentsPerSample = ComponentsPerSample;
            var output = new float[tintTransform.OutputCount];

            for (var i = 0; i < sampleCount; i++)
            {
                tintTransform.Evaluate(input, inputOffset, output, 0);
                AlternateSpace.ToRgb(output, out var red, out var green, out var blue);

                rgbBuffer[rgbBufferOffset + 0] = MathUtils.Clamp(red * 255f, 0f, 255f);
                rgbBuffer[rgbBufferOffset + 1] = MathUtils.Clamp(green * 255f, 0f, 255f);
                rgbBuffer[rgbBufferOffset + 2] = MathUtils.Clamp(blue * 255f, 0f, 255f);

                rgbBufferOffset += 3;
                inputOffset += componentsPerSample;
            }
        }

        public override void ToRgba8(float[] input, int inputOffset, byte[] rgbaBuffer, int rgbaBufferOffset, int sampleCount)
        {
            var componentsPerSample = ComponentsPerSample;
            var output = new float[tintTransform.OutputCount];

            for (var i = 0; i < sampleCount; i++)
            {
                tintTransform.Evaluate(input, inputOffset, output, 0);
                AlternateSpace.ToRgb(output, out var red, out var green, out var blue);

                rgbaBuffer[rgbaBufferOffset + 0] = ToRgb8Component(red);
                rgbaBuffer[rgbaBufferOffset + 1] = ToRgb8Component(green);
                rgbaBuffer[rgbaBufferOffset + 2] = ToRgb8Component(blue);
                rgbaBuffer[rgbaBufferOffset + 3] = 0;

                rgbaBufferOffset += 4;
                inputOffset += componentsPerSample;
            }
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

        public override ColorConversionContext CreateConversionContext(
            int bitsPerComponent,
            DecodeArray decodeArray)
        {
            return ColorConversionContext.Create(this, bitsPerComponent, decodeArray);
        }

        public override int ComponentsPerSample { get; }

        public ColorSpace AlternateSpace { get; }

        public override float[] DefaultColor => new float[ComponentsPerSample];

        public override int GetHashCode() =>
            895666034 ^
            ComponentsPerSample ^
            AlternateSpace.GetHashCode() ^
            tintTransform.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is DeviceNColorSpace colorSpace &&
            colorSpace.ComponentsPerSample == ComponentsPerSample &&
            colorSpace.AlternateSpace.Equals(AlternateSpace) &&
            ReferenceEquals(colorSpace.tintTransform, tintTransform);

        public override string ToString() => "DeviceN N=" + ComponentsPerSample + " " + AlternateSpace;
    }
}
