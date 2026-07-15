// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif

namespace PdfToSvg.Tests.Images.Jpeg
{
    public class JpegBlockUtilsTests
    {
        [Test]
        public void TransposeScalar()
        {
            var source = new float[64]
            {
                00, 01, 02, 03, 04, 05, 06, 07,
                08, 09, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23,
                24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 35, 36, 37, 38, 39,
                40, 41, 42, 43, 44, 45, 46, 47,
                48, 49, 50, 51, 52, 53, 54, 55,
                56, 57, 58, 59, 60, 61, 62, 63,
            };

            var expected = new float[64]
            {
                00, 08, 16, 24, 32, 40, 48, 56,
                01, 09, 17, 25, 33, 41, 49, 57,
                02, 10, 18, 26, 34, 42, 50, 58,
                03, 11, 19, 27, 35, 43, 51, 59,
                04, 12, 20, 28, 36, 44, 52, 60,
                05, 13, 21, 29, 37, 45, 53, 61,
                06, 14, 22, 30, 38, 46, 54, 62,
                07, 15, 23, 31, 39, 47, 55, 63,
            };

            var actual = (float[])source.Clone();
            JpegBlockUtils.TransposeScalar(actual);

            Assert.AreEqual(expected, actual);
        }

#if NET7_0_OR_GREATER
        [Test]
        public void TransposeAvx()
        {
            if (!Avx.IsSupported)
            {
                Assert.Inconclusive("AVX2 not supported");
            }

            var source = new float[64]
            {
                00, 01, 02, 03, 04, 05, 06, 07,
                08, 09, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23,
                24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 35, 36, 37, 38, 39,
                40, 41, 42, 43, 44, 45, 46, 47,
                48, 49, 50, 51, 52, 53, 54, 55,
                56, 57, 58, 59, 60, 61, 62, 63,
            };

            var expected = new float[64]
            {
                00, 08, 16, 24, 32, 40, 48, 56,
                01, 09, 17, 25, 33, 41, 49, 57,
                02, 10, 18, 26, 34, 42, 50, 58,
                03, 11, 19, 27, 35, 43, 51, 59,
                04, 12, 20, 28, 36, 44, 52, 60,
                05, 13, 21, 29, 37, 45, 53, 61,
                06, 14, 22, 30, 38, 46, 54, 62,
                07, 15, 23, 31, 39, 47, 55, 63,
            };

            var actual = (float[])source.Clone();
            JpegBlockUtils.TransposeAvx(actual);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Transpose128()
        {
            var source = new float[64]
            {
                00, 01, 02, 03, 04, 05, 06, 07,
                08, 09, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23,
                24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 35, 36, 37, 38, 39,
                40, 41, 42, 43, 44, 45, 46, 47,
                48, 49, 50, 51, 52, 53, 54, 55,
                56, 57, 58, 59, 60, 61, 62, 63,
            };

            var expected = new float[64]
            {
                00, 08, 16, 24, 32, 40, 48, 56,
                01, 09, 17, 25, 33, 41, 49, 57,
                02, 10, 18, 26, 34, 42, 50, 58,
                03, 11, 19, 27, 35, 43, 51, 59,
                04, 12, 20, 28, 36, 44, 52, 60,
                05, 13, 21, 29, 37, 45, 53, 61,
                06, 14, 22, 30, 38, 46, 54, 62,
                07, 15, 23, 31, 39, 47, 55, 63,
            };

            var actual = (float[])source.Clone();
            JpegBlockUtils.Transpose128(actual);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase(0)]
        [TestCase(3)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(24)]
        [TestCase(32)]
        [TestCase(40)]
        [TestCase(48)]
        [TestCase(63)]
        public void IsSolidBlock256Unsafe_NotSolid(int diffIndex)
        {
            var data = new float[64];
            Array.Fill(data, 42f);
            data[diffIndex] = 43;

            Assert.IsFalse(JpegBlockUtils.IsSolidBlock256Unsafe(ref data[0]));
        }

        [Test]
        public void IsSolidBlock256Unsafe_Solid()
        {
            var data = new float[64];
            Array.Fill(data, 42f);

            Assert.IsTrue(JpegBlockUtils.IsSolidBlock256Unsafe(ref data[0]));
        }

        [Test]
        [TestCase(0)]
        [TestCase(3)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(24)]
        [TestCase(32)]
        [TestCase(40)]
        [TestCase(48)]
        [TestCase(63)]
        public void IsSolidBlock128Unsafe_NotSolid(int diffIndex)
        {
            var data = new float[64];
            Array.Fill(data, 42f);
            data[diffIndex] = 43;

            Assert.IsFalse(JpegBlockUtils.IsSolidBlock128Unsafe(ref data[0]));
        }

        [Test]
        public void IsSolidBlock128Unsafe_Solid()
        {
            var data = new float[64];
            Array.Fill(data, 42f);

            Assert.IsTrue(JpegBlockUtils.IsSolidBlock128Unsafe(ref data[0]));
        }
#endif
    }
}
