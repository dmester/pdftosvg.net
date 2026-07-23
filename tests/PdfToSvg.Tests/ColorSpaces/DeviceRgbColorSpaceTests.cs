// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.ColorSpaces
{
    public class DeviceRgbColorSpaceTests
    {
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(33)]
        [TestCase(63)]
        [TestCase(64)]
        [TestCase(65)]
        public void ToRgb8Float(int samples)
        {
            const int InputOffset = 1;
            const int OutputOffset = 2;
            const int OutputTail = 1; // To ensure nothing is overwritten after the output

            var input = new float[InputOffset + samples * 3];
            var expected = new float[OutputOffset + samples * 3 + OutputTail];
            var actual = new float[OutputOffset + samples * 3 + OutputTail];
            ArrayUtils.Fill(expected, 90000f);
            ArrayUtils.Fill(actual, 90000f);

            var i = 0;
            if (samples > 0)
            {
                // Note:
                // The outcome of Infinity and NaN are platform specific, so they are not tested here

                input[InputOffset + 0] = float.MinValue;
                input[InputOffset + 1] = float.MaxValue;

                expected[OutputOffset + 0] = 0;
                expected[OutputOffset + 1] = 255;

                i += 2;
            }

            for (; InputOffset + i < input.Length; i++)
            {
                input[InputOffset + i] = i * (1f / 255);
                expected[OutputOffset + i] = input[InputOffset + i] * 255f;
            }

            new DeviceRgbColorSpace().ToRgb8(input, InputOffset, actual, OutputOffset, samples);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(33)]
        [TestCase(63)]
        [TestCase(64)]
        [TestCase(65)]
        public void ToRgb8Byte(int samples)
        {
            const int InputOffset = 1;
            const int OutputOffset = 2;
            const int OutputTail = 1; // To ensure nothing is overwritten after the output

            var input = new float[InputOffset + samples * 3];
            var expected = new byte[OutputOffset + samples * 3 + OutputTail];
            var actual = new byte[OutputOffset + samples * 3 + OutputTail];
            ArrayUtils.Fill(expected, (byte)127);
            ArrayUtils.Fill(actual, (byte)127);

            var i = 0;
            if (samples > 0)
            {
                // Note:
                // The outcome of Infinity and NaN are platform specific, so they are not tested here

                input[InputOffset + 0] = -1f;
                input[InputOffset + 1] = 2f;

                expected[OutputOffset + 0] = 0;
                expected[OutputOffset + 1] = 255;

                i += 2;
            }

            for (; InputOffset + i < input.Length; i++)
            {
                input[InputOffset + i] = i * (1f / 255);
                expected[OutputOffset + i] = (byte)i;
            }

            new DeviceRgbColorSpace().ToRgb8(input, InputOffset, actual, OutputOffset, samples);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase(float.NaN, 1)]
        [TestCase(float.NaN, 64)]
        [TestCase(float.NegativeInfinity, 1)]
        [TestCase(float.NegativeInfinity, 64)]
        [TestCase(float.PositiveInfinity, 1)]
        [TestCase(float.PositiveInfinity, 64)]
        public void ToRgb8Byte_Special(float inputValue, int samples)
        {
            var input = new float[samples * 3];
            var expected = new byte[samples * 3];

            input[0] = inputValue;
            input[1] = 42f / 255; // To ensure conversion is successful

            var actual = new byte[samples * 3];

            new DeviceRgbColorSpace().ToRgb8(input, 0, actual, 0, samples);

            // Special values are platform specific
            Assert.IsTrue(actual[0] == 0f || actual[0] == 255f);
            Assert.IsTrue(actual[1] == 42f);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(33)]
        [TestCase(63)]
        [TestCase(64)]
        [TestCase(65)]
        public void ToRgba8(int samples)
        {
            const int InputOffset = 1;
            const int OutputOffset = 2;
            const int OutputTail = 1; // To ensure nothing is overwritten after the output

            var input = new float[InputOffset + samples * 3];
            var expected = new byte[OutputOffset + samples * 4 + OutputTail];
            var actual = new byte[OutputOffset + samples * 4 + OutputTail];
            ArrayUtils.Fill(expected, (byte)127);
            ArrayUtils.Fill(actual, (byte)127);

            var i = 0;
            if (samples > 3)
            {
                // Note:
                // The outcome of Infinity and NaN are platform specific, so they are not tested

                input[InputOffset + 0] = -1f;
                input[InputOffset + 1] = 2f;
                input[InputOffset + 2] = -1;

                input[InputOffset + 3] = 1.4f * (1f / 255);
                input[InputOffset + 4] = 1.5f * (1f / 255);
                input[InputOffset + 5] = 1.6f * (1f / 255);

                input[InputOffset + 6] = 2.4f * (1f / 255);
                input[InputOffset + 7] = 2.5f * (1f / 255);
                input[InputOffset + 8] = 2.6f * (1f / 255);

                expected[OutputOffset + 0] = 0;
                expected[OutputOffset + 1] = 255;
                expected[OutputOffset + 2] = 0;
                expected[OutputOffset + 3] = 0;

                expected[OutputOffset + 4] = 1;
                expected[OutputOffset + 5] = 2;
                expected[OutputOffset + 6] = 2;
                expected[OutputOffset + 7] = 0;

                expected[OutputOffset + 8] = 2;
                expected[OutputOffset + 9] = 3;
                expected[OutputOffset + 10] = 3;
                expected[OutputOffset + 11] = 0;

                i += 3;
            }

            for (; i < samples; i++)
            {
                input[InputOffset + i * 3 + 0] = (i * 3 + 0) * (1f / 255);
                input[InputOffset + i * 3 + 1] = (i * 3 + 1) * (1f / 255);
                input[InputOffset + i * 3 + 2] = (i * 3 + 2) * (1f / 255);

                expected[OutputOffset + i * 4 + 0] = (byte)(i * 3 + 0);
                expected[OutputOffset + i * 4 + 1] = (byte)(i * 3 + 1);
                expected[OutputOffset + i * 4 + 2] = (byte)(i * 3 + 2);
                expected[OutputOffset + i * 4 + 3] = 0;
            }

            new DeviceRgbColorSpace().ToRgba8(input, InputOffset, actual, OutputOffset, samples);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase(float.NaN, 1)]
        [TestCase(float.NaN, 64)]
        [TestCase(float.NegativeInfinity, 1)]
        [TestCase(float.NegativeInfinity, 64)]
        [TestCase(float.PositiveInfinity, 1)]
        [TestCase(float.PositiveInfinity, 64)]
        public void ToRgba8_Special(float inputValue, int samples)
        {
            var input = new float[samples * 3];
            var expected = new byte[samples * 4];

            input[0] = inputValue;
            input[1] = 42f / 255; // To ensure conversion is successful

            var actual = new byte[samples * 4];

            new DeviceRgbColorSpace().ToRgba8(input, 0, actual, 0, samples);

            // Special values are platform specific
            Assert.IsTrue(actual[0] == 0f || actual[0] == 255f);
            Assert.IsTrue(actual[1] == 42f);
        }
    }
}
