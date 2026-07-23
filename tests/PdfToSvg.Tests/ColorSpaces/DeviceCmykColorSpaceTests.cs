// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;

namespace PdfToSvg.Tests.ColorSpaces
{
    public class DeviceCmykColorSpaceTests
    {
        private readonly DeviceCmykColorSpace colorSpace = new();

        [Test]
        public void ToRgb()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                0f, 0f, 0f, 1f
            };

            var inputOffset = 2;
            colorSpace.ToRgb(input, ref inputOffset, out var red, out var green, out var blue);

            Assert.AreEqual(6, inputOffset);
            Assert.AreEqual(0.0498f, red, delta: 0.001f);
            Assert.AreEqual(0.0884f, green, delta: 0.001f);
            Assert.AreEqual(0.1081f, blue, delta: 0.001f);
        }

        [Test]
        public void ToRgb8_Float()
        {
            const int InputOffset = 1;
            const int OutputOffset = 2;
            const int OutputTail = 1; // To ensure nothing is overwritten after the output

            for (var samples = 0; samples < 68; samples++)
            {
                var input = CreateCmykInput(InputOffset, samples);
                var expected = new float[OutputOffset + samples * 3 + OutputTail];
                var actual = new float[OutputOffset + samples * 3 + OutputTail];
                ArrayUtils.Fill(expected, 90000f);
                ArrayUtils.Fill(actual, 90000f);

                for (var i = 0; i < samples; i++)
                {
                    var sampleOffset = InputOffset + i * 4;
                    colorSpace.ToRgb(input, ref sampleOffset, out var red, out var green, out var blue);

                    expected[OutputOffset + i * 3 + 0] = MathUtils.Clamp(red * 255f, 0f, 255f);
                    expected[OutputOffset + i * 3 + 1] = MathUtils.Clamp(green * 255f, 0f, 255f);
                    expected[OutputOffset + i * 3 + 2] = MathUtils.Clamp(blue * 255f, 0f, 255f);
                }

                colorSpace.ToRgb8(input, InputOffset, actual, OutputOffset, samples);

                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void ToRgb8_Byte()
        {
            const int InputOffset = 1;
            const int OutputOffset = 2;
            const int OutputTail = 1; // To ensure nothing is overwritten after the output

            for (var samples = 0; samples < 68; samples++)
            {
                var input = CreateCmykInput(InputOffset, samples);
                var expected = new byte[OutputOffset + samples * 3 + OutputTail];
                var actual = new byte[OutputOffset + samples * 3 + OutputTail];
                ArrayUtils.Fill(expected, (byte)127);
                ArrayUtils.Fill(actual, (byte)127);

                for (var i = 0; i < samples; i++)
                {
                    var sampleOffset = InputOffset + i * 4;
                    colorSpace.ToRgb(input, ref sampleOffset, out var red, out var green, out var blue);

                    expected[OutputOffset + i * 3 + 0] = ToRgb8Component(red);
                    expected[OutputOffset + i * 3 + 1] = ToRgb8Component(green);
                    expected[OutputOffset + i * 3 + 2] = ToRgb8Component(blue);
                }

                colorSpace.ToRgb8(input, InputOffset, actual, OutputOffset, samples);

                Assert.AreEqual(expected, actual, "Samples: " + samples);
            }
        }

        [Test]
        public void ToRgba8_Byte()
        {
            const int InputOffset = 1;
            const int OutputOffset = 2;
            const int OutputTail = 1; // To ensure nothing is overwritten after the output

            for (var samples = 0; samples < 68; samples++)
            {
                var input = CreateCmykInput(InputOffset, samples);
                var expected = new byte[OutputOffset + samples * 4 + OutputTail];
                var actual = new byte[OutputOffset + samples * 4 + OutputTail];
                ArrayUtils.Fill(expected, (byte)127);
                ArrayUtils.Fill(actual, (byte)127);

                for (var i = 0; i < samples; i++)
                {
                    var sampleOffset = InputOffset + i * 4;
                    colorSpace.ToRgb(input, ref sampleOffset, out var red, out var green, out var blue);

                    expected[OutputOffset + i * 4 + 0] = ToRgb8Component(red);
                    expected[OutputOffset + i * 4 + 1] = ToRgb8Component(green);
                    expected[OutputOffset + i * 4 + 2] = ToRgb8Component(blue);
                    expected[OutputOffset + i * 4 + 3] = 0;
                }

                colorSpace.ToRgba8(input, InputOffset, actual, OutputOffset, samples);

                Assert.AreEqual(expected, actual, "Samples: " + samples);
            }
        }

        private static float[] CreateCmykInput(int inputOffset, int samples)
        {
            var input = new float[inputOffset + samples * 4];

            for (var i = 0; i < samples * 4; i++)
            {
                // Note:
                // The outcome of Infinity and NaN are platform specific, so they are not tested here.
                // The pattern includes values slightly outside the range [0, 1] to also exercise clamping.
                input[inputOffset + i] = (i % 37) * (1f / 32) - 0.08f;
            }

            return input;
        }

        private static byte ToRgb8Component(float value)
        {
            var intValue = unchecked((int)(value * 255f + 0.5f));

            return (byte)(
                intValue < 0 ? 0 :
                intValue > 255 ? 255 :
                intValue);
        }
    }
}
