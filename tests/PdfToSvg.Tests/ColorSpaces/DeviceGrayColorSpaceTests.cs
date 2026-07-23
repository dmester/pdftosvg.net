// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;

namespace PdfToSvg.Tests.ColorSpaces
{
    public class DeviceGrayColorSpaceTests
    {
        private readonly DeviceGrayColorSpace colorSpace = new();

        [Test]
        public void ToRgb()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                0.51f
            };

            var inputOffset = 2;
            colorSpace.ToRgb(input, ref inputOffset, out var red, out var green, out var blue);

            Assert.AreEqual(3, inputOffset);
            Assert.AreEqual(0.51f, red, delta: 0.001f);
            Assert.AreEqual(0.51f, green, delta: 0.001f);
            Assert.AreEqual(0.51f, blue, delta: 0.001f);
        }

        [Test]
        public void ToRgb8_Float()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                -1f, 0f, 0.5098f, 1f, 2f
            };
            var actualOutput = new float[17];
            ArrayUtils.Fill(actualOutput, 127f);

            colorSpace.ToRgb8(input, inputOffset: 2, actualOutput, rgbBufferOffset: 1, sampleCount: 5);

            Assert.That(
                actualOutput,
                Is.EqualTo(new float[] { 127, 0, 0, 0, 0, 0, 0, 130, 130, 130, 255, 255, 255, 255, 255, 255, 127 })
                    .Within(0.001f));
        }

        [Test]
        public void ToRgb8_Byte()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                -1f, 0f, 0.51f, 1f, 2f
            };
            var actualOutput = new byte[17];
            ArrayUtils.Fill(actualOutput, (byte)127);

            colorSpace.ToRgb8(input, inputOffset: 2, actualOutput, rgbBufferOffset: 1, sampleCount: 5);

            Assert.AreEqual(
                new byte[] { 127, 0, 0, 0, 0, 0, 0, 130, 130, 130, 255, 255, 255, 255, 255, 255, 127 },
                actualOutput);
        }

        [Test]
        public void ToRgba8_Byte()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                -1f, 0f, 0.51f, 1f, 2f
            };
            var actualOutput = new byte[22];
            ArrayUtils.Fill(actualOutput, (byte)127);

            colorSpace.ToRgba8(input, inputOffset: 2, actualOutput, rgbaBufferOffset: 1, sampleCount: 5);

            Assert.AreEqual(
                new byte[] { 127, 0, 0, 0, 0, 0, 0, 0, 0, 130, 130, 130, 0, 255, 255, 255, 0, 255, 255, 255, 0, 127 },
                actualOutput);
        }

    }
}
