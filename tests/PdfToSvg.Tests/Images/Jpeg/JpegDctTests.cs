// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using NUnit.Framework.Internal;
using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;


#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal class JpegDctTests
    {
        [Test]
        public void TestReferenceDct()
        {
            var input = new short[64];
            var dcted = new short[64];
            var output = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                input[i] = (short)random.Next(0, 255);
            }

            ReferenceDct(input, dcted);
            ReferenceIdct(dcted, output);

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(input[i], output[i], 5d, "Index {0}", i);
            }
        }

        [Test]
        public void ForwardScalar()
        {
            var data = new short[64];
            var refdct = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                data[i] = (short)random.Next(-128, 128);
            }

            ReferenceDct(data, refdct);

            JpegDct.ForwardScalar(data);
            JpegBlockUtils.TransposeScalar(data);

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(refdct[i], data[i], 5d, "Index {0}", i);
            }
        }

        [Test]
        public void InverseScalar()
        {
            var data = new short[64];
            var refidct = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                data[i] = (short)random.Next(-128, 128);
            }

            ReferenceIdct(data, refidct);

            JpegBlockUtils.TransposeScalar(data);
            JpegDct.InverseScalar(data);

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(refidct[i], data[i], 5d, "Index {0}", i);
            }
        }

#if NET7_0_OR_GREATER

        [Test]
        public void ForwardSse()
        {
            if (!Sse2.IsSupported)
            {
                Assert.Inconclusive("SSE2 not supported");
            }

            var data = new short[64];
            var refdct = new short[64];
            var actual = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                data[i] = (short)random.Next(-128, 128);
            }

            // Reference
            ReferenceDct(data, refdct);

            // SSE2
            {
                ref var pSrc = ref Unsafe.As<short, Vector128<short>>(ref data[0]);
                var (row0_lo, row0_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 0));
                var (row1_lo, row1_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 1));
                var (row2_lo, row2_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 2));
                var (row3_lo, row3_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 3));
                var (row4_lo, row4_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 4));
                var (row5_lo, row5_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 5));
                var (row6_lo, row6_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 6));
                var (row7_lo, row7_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 7));

                JpegDct.ForwardSse(
                    ref row0_lo, ref row0_hi,
                    ref row1_lo, ref row1_hi,
                    ref row2_lo, ref row2_hi,
                    ref row3_lo, ref row3_hi,
                    ref row4_lo, ref row4_hi,
                    ref row5_lo, ref row5_hi,
                    ref row6_lo, ref row6_hi,
                    ref row7_lo, ref row7_hi);
                JpegBlockUtils.TransposeSse(
                    ref row0_lo, ref row0_hi,
                    ref row1_lo, ref row1_hi,
                    ref row2_lo, ref row2_hi,
                    ref row3_lo, ref row3_hi,
                    ref row4_lo, ref row4_hi,
                    ref row5_lo, ref row5_hi,
                    ref row6_lo, ref row6_hi,
                    ref row7_lo, ref row7_hi);

                ref var pActual = ref Unsafe.As<short, Vector128<short>>(ref actual[0]);
                Unsafe.Add(ref pActual, 0) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row0_lo, row0_hi);
                Unsafe.Add(ref pActual, 1) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row1_lo, row1_hi);
                Unsafe.Add(ref pActual, 2) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row2_lo, row2_hi);
                Unsafe.Add(ref pActual, 3) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row3_lo, row3_hi);
                Unsafe.Add(ref pActual, 4) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row4_lo, row4_hi);
                Unsafe.Add(ref pActual, 5) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row5_lo, row5_hi);
                Unsafe.Add(ref pActual, 6) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row6_lo, row6_hi);
                Unsafe.Add(ref pActual, 7) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row7_lo, row7_hi);
            }

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(refdct[i], actual[i], 5d, "Index {0}", i);
            }
        }

        [Test]
        public void InverseSse()
        {
            if (!Sse2.IsSupported)
            {
                Assert.Inconclusive("SSE2 not supported");
            }

            var data = new short[64];
            var refidct = new short[64];
            var actual = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                data[i] = (short)random.Next(-128, 128);
            }

            // Reference
            ReferenceIdct(data, refidct);

            // SSE2
            {
                ref var pSrc = ref Unsafe.As<short, Vector128<short>>(ref data[0]);

                var (row0_lo, row0_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 0));
                var (row1_lo, row1_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 1));
                var (row2_lo, row2_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 2));
                var (row3_lo, row3_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 3));
                var (row4_lo, row4_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 4));
                var (row5_lo, row5_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 5));
                var (row6_lo, row6_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 6));
                var (row7_lo, row7_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pSrc, 7));

                JpegBlockUtils.TransposeSse(
                    ref row0_lo, ref row0_hi,
                    ref row1_lo, ref row1_hi,
                    ref row2_lo, ref row2_hi,
                    ref row3_lo, ref row3_hi,
                    ref row4_lo, ref row4_hi,
                    ref row5_lo, ref row5_hi,
                    ref row6_lo, ref row6_hi,
                    ref row7_lo, ref row7_hi);
                JpegDct.InverseSse(
                    ref row0_lo, ref row0_hi,
                    ref row1_lo, ref row1_hi,
                    ref row2_lo, ref row2_hi,
                    ref row3_lo, ref row3_hi,
                    ref row4_lo, ref row4_hi,
                    ref row5_lo, ref row5_hi,
                    ref row6_lo, ref row6_hi,
                    ref row7_lo, ref row7_hi);

                ref var pActual = ref Unsafe.As<short, Vector128<short>>(ref actual[0]);
                Unsafe.Add(ref pActual, 0) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row0_lo, row0_hi);
                Unsafe.Add(ref pActual, 1) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row1_lo, row1_hi);
                Unsafe.Add(ref pActual, 2) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row2_lo, row2_hi);
                Unsafe.Add(ref pActual, 3) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row3_lo, row3_hi);
                Unsafe.Add(ref pActual, 4) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row4_lo, row4_hi);
                Unsafe.Add(ref pActual, 5) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row5_lo, row5_hi);
                Unsafe.Add(ref pActual, 6) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row6_lo, row6_hi);
                Unsafe.Add(ref pActual, 7) = JpegVectorUtils.ConvertToVector128Int16_Sse2(row7_lo, row7_hi);
            }

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(refidct[i], actual[i], 5d, "Index {0}", i);
            }
        }

        [Test]
        public void ForwardAvx()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Inconclusive("AVX2 not supported");
            }

            var data = new short[64];
            var refdct = new short[64];
            var actual = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                data[i] = (short)random.Next(-128, 128);
            }

            // Reference
            ReferenceDct(data, refdct);

            // AVX2
            {
                ref var pSrc = ref Unsafe.As<short, Vector256<short>>(ref data[0]);
                var (fRow0, fRow1) = JpegVectorUtils.ConvertToVector256Single(Unsafe.Add(ref pSrc, 0));
                var (fRow2, fRow3) = JpegVectorUtils.ConvertToVector256Single(Unsafe.Add(ref pSrc, 1));
                var (fRow4, fRow5) = JpegVectorUtils.ConvertToVector256Single(Unsafe.Add(ref pSrc, 2));
                var (fRow6, fRow7) = JpegVectorUtils.ConvertToVector256Single(Unsafe.Add(ref pSrc, 3));

                JpegDct.ForwardAvx(ref fRow0, ref fRow1, ref fRow2, ref fRow3, ref fRow4, ref fRow5, ref fRow6, ref fRow7);
                JpegBlockUtils.TransposeAvx(ref fRow0, ref fRow1, ref fRow2, ref fRow3, ref fRow4, ref fRow5, ref fRow6, ref fRow7);

                ref var pActual = ref Unsafe.As<short, Vector256<short>>(ref actual[0]);
                Unsafe.Add(ref pActual, 0) = JpegVectorUtils.ConvertToVector256Int16_Avx2(fRow0, fRow1);
                Unsafe.Add(ref pActual, 1) = JpegVectorUtils.ConvertToVector256Int16_Avx2(fRow2, fRow3);
                Unsafe.Add(ref pActual, 2) = JpegVectorUtils.ConvertToVector256Int16_Avx2(fRow4, fRow5);
                Unsafe.Add(ref pActual, 3) = JpegVectorUtils.ConvertToVector256Int16_Avx2(fRow6, fRow7);
            }

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(refdct[i], actual[i], 5d, "Index {0}", i);
            }
        }

        [Test]
        public void InverseAvx()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Inconclusive("AVX2 not supported");
            }

            var data = new short[64];
            var refidct = new short[64];
            var actual = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                data[i] = (short)random.Next(-128, 128);
            }

            // Reference
            ReferenceIdct(data, refidct);

            // AVX2
            {
                ref var pSrc = ref Unsafe.As<short, Vector256<short>>(ref data[0]);
                var (fRow0, fRow1) = JpegVectorUtils.ConvertToVector256Single(Unsafe.Add(ref pSrc, 0));
                var (fRow2, fRow3) = JpegVectorUtils.ConvertToVector256Single(Unsafe.Add(ref pSrc, 1));
                var (fRow4, fRow5) = JpegVectorUtils.ConvertToVector256Single(Unsafe.Add(ref pSrc, 2));
                var (fRow6, fRow7) = JpegVectorUtils.ConvertToVector256Single(Unsafe.Add(ref pSrc, 3));

                JpegBlockUtils.TransposeAvx(ref fRow0, ref fRow1, ref fRow2, ref fRow3, ref fRow4, ref fRow5, ref fRow6, ref fRow7);
                JpegDct.InverseAvx(ref fRow0, ref fRow1, ref fRow2, ref fRow3, ref fRow4, ref fRow5, ref fRow6, ref fRow7);

                ref var pActual = ref Unsafe.As<short, Vector256<short>>(ref actual[0]);
                Unsafe.Add(ref pActual, 0) = JpegVectorUtils.ConvertToVector256Int16_Avx2(fRow0, fRow1);
                Unsafe.Add(ref pActual, 1) = JpegVectorUtils.ConvertToVector256Int16_Avx2(fRow2, fRow3);
                Unsafe.Add(ref pActual, 2) = JpegVectorUtils.ConvertToVector256Int16_Avx2(fRow4, fRow5);
                Unsafe.Add(ref pActual, 3) = JpegVectorUtils.ConvertToVector256Int16_Avx2(fRow6, fRow7);
            }

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(refidct[i], actual[i], 5d, "Index {0}", i);
            }
        }

#endif

        private static void ReferenceDct(short[] input, short[] output)
        {
            for (var o = 0; o < 64; o++)
            {
                var u = o % 8;
                var v = o / 8;

                var Cu = u == 0 ? (1d / Math.Sqrt(2)) : 1;
                var Cv = v == 0 ? (1d / Math.Sqrt(2)) : 1;

                var sum = 0d;

                for (var i = 0; i < 64; i++)
                {
                    var x = i % 8;
                    var y = i / 8;

                    sum +=
                        Math.Cos((2 * x + 1) * u * Math.PI / 16) *
                        Math.Cos((2 * y + 1) * v * Math.PI / 16) *
                        (input[i] - 128);
                }

                output[o] = (short)(sum / 4 * Cu * Cv);
            }
        }

        private static void ReferenceIdct(short[] input, short[] output)
        {
            for (var o = 0; o < 64; o++)
            {
                var x = o % 8;
                var y = o / 8;

                var sum = 0d;

                for (var i = 0; i < 64; i++)
                {
                    var u = i % 8;
                    var v = i / 8;

                    var Cu = u == 0 ? (1d / Math.Sqrt(2)) : 1;
                    var Cv = v == 0 ? (1d / Math.Sqrt(2)) : 1;

                    sum +=
                        Cu * Cv *
                        Math.Cos((2 * x + 1) * u * Math.PI / 16) *
                        Math.Cos((2 * y + 1) * v * Math.PI / 16) *
                        input[i];
                }

                output[o] = (short)MathUtils.Clamp(sum / 4 + 128, 0d, 255d);
            }
        }
    }
}


