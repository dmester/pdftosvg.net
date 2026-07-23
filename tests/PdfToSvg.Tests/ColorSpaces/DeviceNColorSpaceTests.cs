// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.Functions;

namespace PdfToSvg.Tests.ColorSpaces
{
    public class DeviceNColorSpaceTests
    {
        private readonly DeviceNColorSpace colorSpace = new(
            componentsPerSample: 2,
            new DeviceRgbColorSpace(),
            new TintTransform());

        [Test]
        public void ToRgb()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                0.2f, 0.4f
            };

            var inputOffset = 2;
            colorSpace.ToRgb(input, ref inputOffset, out var red, out var green, out var blue);

            Assert.AreEqual(4, inputOffset);
            Assert.AreEqual(0.2f, red, delta: 0.001f);
            Assert.AreEqual(0.4f, green, delta: 0.001f);
            Assert.AreEqual(0.3f, blue, delta: 0.001f);
        }

        [Test]
        public void ToRgb8_Float()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                -1f, 2f,
                0f, 0f,
                0.2f, 0.4f,
                1f, 1f,
                2f, -1f,
            };
            var actualOutput = new float[17];
            ArrayUtils.Fill(actualOutput, 127f);

            colorSpace.ToRgb8(input, inputOffset: 2, actualOutput, rgbBufferOffset: 1, sampleCount: 5);

            Assert.That(
                actualOutput,
                Is.EqualTo(new float[]
                {
                    127,
                    0, 255, 127.5f,
                    0, 0, 0,
                    51, 102, 76.5f,
                    255, 255, 255,
                    255, 0, 127.5f,
                    127,
                }).Within(0.001f));
        }

        [Test]
        public void ToRgb8_Byte()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                -1f, 2f,
                0f, 0f,
                0.2f, 0.4f,
                1f, 1f,
                2f, -1f,
            };
            var actualOutput = new byte[17];
            ArrayUtils.Fill(actualOutput, (byte)127);

            colorSpace.ToRgb8(input, inputOffset: 2, actualOutput, rgbBufferOffset: 1, sampleCount: 5);

            Assert.AreEqual(
                new byte[]
                {
                    127,
                    0, 255, 128,
                    0, 0, 0,
                    51, 102, 77,
                    255, 255, 255,
                    255, 0, 128,
                    127,
                },
                actualOutput);
        }

        [Test]
        public void ToRgba8_Byte()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                -1f, 2f,
                0f, 0f,
                0.2f, 0.4f,
                1f, 1f,
                2f, -1f,
            };
            var actualOutput = new byte[22];
            ArrayUtils.Fill(actualOutput, (byte)127);

            colorSpace.ToRgba8(input, inputOffset: 2, actualOutput, rgbaBufferOffset: 1, sampleCount: 5);

            Assert.AreEqual(
                new byte[]
                {
                    127,
                    0, 255, 128, 0,
                    0, 0, 0, 0,
                    51, 102, 77, 0,
                    255, 255, 255, 0,
                    255, 0, 128, 0,
                    127,
                },
                actualOutput);
        }

        private sealed class TintTransform : Function
        {
            public override int InputCount => 2;

            public override int OutputCount => 3;

            public override void Evaluate(float[] input, int inputOffset, float[] output, int outputOffset)
            {
                var first = input[inputOffset + 0];
                var second = input[inputOffset + 1];

                output[outputOffset + 0] = first;
                output[outputOffset + 1] = second;
                output[outputOffset + 2] = (first + second) * 0.5f;
            }
        }
    }
}
