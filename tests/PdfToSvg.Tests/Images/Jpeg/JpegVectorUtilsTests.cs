// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

#if NET7_0_OR_GREATER

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    public class JpegVectorUtilsTests
    {
        [Test]
        public void ConvertToVector128Single()
        {
            Vector128<short> vector = Vector128.Create(0, 1, 2, 3, 4, 5, 6, 7);

            var (lower, upper) = JpegVectorUtils.ConvertToVector128Single(vector);

            Assert.AreEqual(Vector128.Create(0f, 1f, 2f, 3f), lower);
            Assert.AreEqual(Vector128.Create(4f, 5f, 6f, 7f), upper);
        }

        [Test]
        public void ConvertToVector256Single()
        {
            Vector256<short> vector = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);

            var (lower, upper) = JpegVectorUtils.ConvertToVector256Single(vector);

            Assert.AreEqual(Vector256.Create(0f, 1f, 02f, 03f, 04f, 05f, 06f, 07f), lower);
            Assert.AreEqual(Vector256.Create(8f, 9f, 10f, 11f, 12f, 13f, 14f, 15f), upper);
        }

        [Test]
        public void ConvertToVector128Int16_Sse2()
        {
            if (!Sse2.IsSupported)
            {
                Assert.Inconclusive("Sse2 not supported");
            }

            var actual = JpegVectorUtils.ConvertToVector128Int16_Sse2(
                Vector128.Create(0f, 0.4f, 0.5f, 0.6f),
                Vector128.Create(1.5f, 5f, 6f, 7f));

            Assert.AreEqual(Vector128.Create(0, 0, 0, 1, 2, 5, 6, 7), actual);
        }

        [Test]
        public void ConvertToVector256Int16_Avx2()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Inconclusive("AVX2 not supported");
            }

            var actual = JpegVectorUtils.ConvertToVector256Int16_Avx2(
                Vector256.Create(0f, 0.4f, 0.5f, 0.6f, 1.4f, 1.5f, 1.6f, 07f),
                Vector256.Create(8f, 9f, 10f, 11f, 12f, 13f, 14f, 15f));

            Assert.AreEqual(Vector256.Create(0, 0, 0, 1, 1, 2, 2, 7, 8, 9, 10, 11, 12, 13, 14, 15), actual);
        }

        [Test]
        public void Widen_Vector128_Int16()
        {
            Vector128<short> vector = Vector128.Create(0, 1, 2, 3, 4, 5, 6, 7);

            var actual = JpegVectorUtils.Widen(vector);

            Assert.AreEqual(Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7), actual);
        }

        [Test]
        public void Widen_Vector256_Int16()
        {
            Vector256<short> vector = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);

            var (lower, upper) = JpegVectorUtils.Widen(vector);

            Assert.AreEqual(Vector256.Create(0, 1, 02, 03, 04, 05, 06, 07), lower);
            Assert.AreEqual(Vector256.Create(8, 9, 10, 11, 12, 13, 14, 15), upper);
        }

        [Test]
        public void NarrowWithSaturation_Avx2_Int32()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Inconclusive("AVX2 not supported");
            }
            else
            {
                var actual = JpegVectorUtils.NarrowWithSaturation_Avx2(
                    Vector256.Create(0, 1, 02, 03, 04, 05, 06, 07),
                    Vector256.Create(8, 9, 10, 11, 12, 13, 14, 15));

                Assert.AreEqual(Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15), actual);
            }
        }

        [Test]
        public void ClampNative128_Scalar()
        {
            var input = Vector128.Create(-0.001f, 0.001f, 249.999f, 255.1f);

            var output = JpegVectorUtils.ClampNative(input, Vector128.Create(0f), Vector128.Create(255f));

            Assert.AreEqual(Vector128.Create(0, 0.001f, 249.999f, 255f), output);
        }

        [Test]
        public void ClampNative128_Infinity()
        {
            var input = Vector128.Create(float.NegativeInfinity, 0.001f, 249.999f, float.PositiveInfinity);

            var output = JpegVectorUtils.ClampNative(input, Vector128.Create(0f), Vector128.Create(255f));

            Assert.AreEqual(Vector128.Create(0, 0.001f, 249.999f, 255f), output);
        }

        [Test]
        public void ClampNative256()
        {
            var input = Vector256.Create(float.NegativeInfinity, -0.001f, 0.001f, 1f, 2f, 249.999f, 255f, float.PositiveInfinity);

            var output = JpegVectorUtils.ClampNative(input, Vector256.Create(0f), Vector256.Create(255f));

            Assert.AreEqual(Vector256.Create(0, 0, 0.001f, 1f, 2f, 249.999f, 255f, 255f), output);
        }
    }
}

#endif
