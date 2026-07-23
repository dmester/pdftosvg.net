// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;

namespace PdfToSvg.Tests.ColorSpaces
{
    public class IndexedColorSpaceTests
    {
        private readonly IndexedColorSpace colorSpace = new(
            new DeviceRgbColorSpace(),
            [
                1, 2, 3,
                4, 5, 6,
                253, 254, 255,
            ]);

        [Test]
        public void ToRgb()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                2, 1, 0,
            };

            var inputOffset = 2;
            colorSpace.ToRgb(input, ref inputOffset, out var red, out var green, out var blue);

            Assert.AreEqual(3, inputOffset);
            Assert.AreEqual(253f / 255, red, delta: 0.001f);
            Assert.AreEqual(254f / 255, green, delta: 0.001f);
            Assert.AreEqual(255f / 255, blue, delta: 0.001f);
        }

        [Test]
        public void ToRgb8_Float()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                2, 1, 0,
            };
            var actualOutput = new float[11];
            ArrayUtils.Fill(actualOutput, 127f);

            colorSpace.ToRgb8(input, inputOffset: 2, actualOutput, rgbBufferOffset: 1, sampleCount: 3);

            Assert.AreEqual(new float[] { 127, 253, 254, 255, 4, 5, 6, 1, 2, 3, 127 }, actualOutput);
        }

        [Test]
        public void ToRgb8_Byte()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                2, 1, 0,
            };
            var actualOutput = new byte[11];
            ArrayUtils.Fill(actualOutput, (byte)127);

            colorSpace.ToRgb8(input, inputOffset: 2, actualOutput, rgbBufferOffset: 1, sampleCount: 3);

            Assert.AreEqual(new byte[] { 127, 253, 254, 255, 4, 5, 6, 1, 2, 3, 127 }, actualOutput);
        }

        [Test]
        public void ToRgba8_Byte()
        {
            var input = new float[]
            {
                111, 111, // Ignore
                2, 1, 0,
            };
            var actualOutput = new byte[14];
            ArrayUtils.Fill(actualOutput, (byte)127);

            colorSpace.ToRgba8(input, inputOffset: 2, actualOutput, rgbaBufferOffset: 1, sampleCount: 3);

            Assert.AreEqual(new byte[] { 127, 253, 254, 255, 0, 4, 5, 6, 0, 1, 2, 3, 0, 127 }, actualOutput);
        }

    }
}
