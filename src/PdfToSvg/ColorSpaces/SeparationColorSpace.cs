// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Functions;
using PdfToSvg.Imaging;

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

        public override void ToRgb8(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            var output = new float[tintTransform.OutputCount];

            for (var i = 0; i < sampleCount; i++)
            {
                tintTransform.Evaluate(input, inputOffset, output, 0);
                AlternateSpace.ToRgb(output, out var red, out var green, out var blue);

                rgbBuffer[rgbBufferOffset + 0] = ToRgb8Component(red);
                rgbBuffer[rgbBufferOffset + 1] = ToRgb8Component(green);
                rgbBuffer[rgbBufferOffset + 2] = ToRgb8Component(blue);

                rgbBufferOffset += 3;
                inputOffset++;
            }
        }

        public override void ToRgb8(float[] input, int inputOffset, float[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            var output = new float[tintTransform.OutputCount];

            for (var i = 0; i < sampleCount; i++)
            {
                tintTransform.Evaluate(input, inputOffset, output, 0);
                AlternateSpace.ToRgb(output, out var red, out var green, out var blue);

                rgbBuffer[rgbBufferOffset + 0] = MathUtils.Clamp(red * 255f, 0f, 255f);
                rgbBuffer[rgbBufferOffset + 1] = MathUtils.Clamp(green * 255f, 0f, 255f);
                rgbBuffer[rgbBufferOffset + 2] = MathUtils.Clamp(blue * 255f, 0f, 255f);

                rgbBufferOffset += 3;
                inputOffset++;
            }
        }

        public override void ToRgba8(float[] input, int inputOffset, byte[] rgbaBuffer, int rgbaBufferOffset, int sampleCount)
        {
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
                inputOffset++;
            }
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, 1f });
        }

        public override ColorConversionContext CreateConversionContext(
            int bitsPerComponent,
            DecodeArray decodeArray)
        {
            return ColorConversionContext.Create(this, bitsPerComponent, decodeArray);
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
