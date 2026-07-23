// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.ColorSpaces;
using PdfToSvg.Imaging;

namespace PdfToSvg.Tests.ColorSpaces
{
    public class ColorConversionContextTests
    {
        [Test]
        public void Create_UnsupportedConfigurationsDoNotCache()
        {
            var oneComponentColorSpace = new CountingColorSpace(1);
            var oneComponentDecode = CreateDecodeArray(bitsPerComponent: 9, componentsPerSample: 1);
            var oneComponentContext = ColorConversionContext.Create(
                oneComponentColorSpace, bitsPerComponent: 9, oneComponentDecode);
            var oneComponentInput = new[] { 128f, 128f };
            var oneComponentOutput = new byte[6];

            oneComponentContext.DecodeToRgb8(oneComponentInput, 0, oneComponentOutput, 0, 2);

            Assert.AreEqual(2, oneComponentColorSpace.ConversionCount);

            var fiveComponentColorSpace = new CountingColorSpace(5);
            var fiveComponentDecode = CreateDecodeArray(bitsPerComponent: 8, componentsPerSample: 5);
            var fiveComponentContext = ColorConversionContext.Create(
                fiveComponentColorSpace, bitsPerComponent: 8, fiveComponentDecode);
            var fiveComponentInput = new[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
            var fiveComponentOutput = new byte[6];

            fiveComponentContext.DecodeToRgb8(fiveComponentInput, 0, fiveComponentOutput, 0, 2);

            Assert.AreEqual(2, fiveComponentColorSpace.ConversionCount);
        }

        [Test]
        public void DecodeToRgb8_OneComponentCachesRawInput()
        {
            var colorSpace = new CountingColorSpace(1);
            var decodeArray = CreateDecodeArray(bitsPerComponent: 8, componentsPerSample: 1);
            var context = ColorConversionContext.Create(colorSpace, bitsPerComponent: 8, decodeArray);
            var input = new[] { -1f, 51f, 51f, 256f };
            var byteOutput = new byte[8];
            var floatOutput = new float[8];

            context.DecodeToRgb8(input, 1, byteOutput, 2, 2);
            context.DecodeToRgb8(input, 1, floatOutput, 2, 2);

            Assert.AreEqual(new byte[] { 0, 0, 51, 26, 204, 51, 26, 204 }, byteOutput);
            Assert.AreEqual(new float[] { 0, 0, 51, 26, 204, 51, 26, 204 }, floatOutput);
            Assert.AreEqual(1, colorSpace.ConversionCount);
        }

        [Test]
        public void DecodeToRgb8_OneComponentPreservesCustomDecodeArray()
        {
            var colorSpace = new CountingColorSpace(1);
            var decodeArray = new DecodeArray(bitsPerComponent: 1, new[] { 0.2f, 0.8f });
            var context = ColorConversionContext.Create(colorSpace, bitsPerComponent: 1, decodeArray);
            var input = new[] { 0f, 1f, 0f, 1f };
            var output = new byte[12];

            context.DecodeToRgb8(input, 0, output, 0, 4);

            Assert.AreEqual(
                new byte[] { 51, 26, 204, 204, 102, 51, 51, 26, 204, 204, 102, 51 },
                output);
            Assert.AreEqual(2, colorSpace.ConversionCount);
        }

        [Test]
        public void DecodeToRgba8_MultipleComponentsUsesInputOffsetInCacheKey()
        {
            var colorSpace = new CountingColorSpace(3);
            var decodeArray = CreateDecodeArray(bitsPerComponent: 8, componentsPerSample: 3);
            var context = ColorConversionContext.Create(colorSpace, bitsPerComponent: 8, decodeArray);
            var input = new[]
            {
                255f, 255f,
                51f, 102f, 153f,
                51f, 102f, 153f,
                153f, 102f, 51f,
            };
            var output = new byte[14];

            context.DecodeToRgba8(input, 2, output, 2, 3);

            Assert.AreEqual(
                new byte[] { 0, 0, 51, 102, 153, 0, 51, 102, 153, 0, 153, 102, 51, 0 },
                output);
            Assert.AreEqual(2, colorSpace.ConversionCount);
        }

        [Test]
        public void DecodeToRgb8_MultipleComponentsPreservesCustomDecodeArray()
        {
            var colorSpace = new CountingColorSpace(2);
            var decodeArray = new DecodeArray(bitsPerComponent: 1, new[] { 0.2f, 0.8f, 0.4f, 0.6f });
            var context = ColorConversionContext.Create(colorSpace, bitsPerComponent: 1, decodeArray);
            var input = new[] { 0f, 0f, 1f, 1f, 0f, 0f };
            var output = new byte[9];

            context.DecodeToRgb8(input, 0, output, 0, 3);

            Assert.AreEqual(new byte[] { 51, 102, 204, 204, 153, 51, 51, 102, 204 }, output);
            Assert.AreEqual(2, colorSpace.ConversionCount);
        }

        [Test]
        public void DecodeToRgb8_WithoutCacheDecodesInput()
        {
            var colorSpace = new CountingColorSpace(1);
            var decodeArray = new DecodeArray(bitsPerComponent: 8, new[] { 0.2f, 0.8f });
            var context = new ColorConversionContext(colorSpace, decodeArray);
            var input = new[] { 0f, 255f };
            var output = new byte[6];

            context.DecodeToRgb8(input, 0, output, 0, 2);

            Assert.AreEqual(new byte[] { 51, 26, 204, 204, 102, 51 }, output);
            Assert.AreEqual(new[] { 0.2f, 0.8f }, input);
        }

        private static DecodeArray CreateDecodeArray(int bitsPerComponent, int componentsPerSample)
        {
            var values = new float[componentsPerSample * 2];

            for (var i = 1; i < values.Length; i += 2)
            {
                values[i] = 1;
            }

            return new DecodeArray(bitsPerComponent, values);
        }

        private sealed class CountingColorSpace : ColorSpace
        {
            public CountingColorSpace(int componentsPerSample)
            {
                ComponentsPerSample = componentsPerSample;
            }

            public int ConversionCount { get; private set; }

            public override int ComponentsPerSample { get; }

            public override float[] DefaultColor => new float[ComponentsPerSample];

            public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
            {
                return CreateDecodeArray(bitsPerComponent, ComponentsPerSample);
            }

            public override void ToRgb(
                float[] input,
                ref int inputOffset,
                out float red,
                out float green,
                out float blue)
            {
                ConversionCount++;

                red = input[inputOffset];
                green = ComponentsPerSample > 1 ? input[inputOffset + 1] : input[inputOffset] * 0.5f;
                blue = ComponentsPerSample > 2 ? input[inputOffset + 2] : 1 - input[inputOffset];
                inputOffset += ComponentsPerSample;
            }
        }
    }
}
