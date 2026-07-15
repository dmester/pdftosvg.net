// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.ImageModel;
using PdfToSvg.Imaging.Jpx.Transforms;
using System;

namespace PdfToSvg.Tests.Images.Jpx.Transforms
{
    public class JpxMctTests
    {
        private static JpxComponent Component(int precision, int separationX = 1, int separationY = 1)
        {
            return new JpxComponent
            {
                Ssizi = precision - 1,
                XRsizi = separationX,
                YRsizi = separationY,
            };
        }

        [Test]
        public void AreComponentsCompatible_EqualProperties()
        {
            var components = new[] { Component(8), Component(8), Component(8) };

            Assert.AreEqual(true, JpxMct.AreComponentsCompatible(components));
        }

        [Test]
        public void AreComponentsCompatible_DifferentSeparation()
        {
            var components = new[] { Component(8), Component(8, separationX: 2), Component(8) };

            Assert.AreEqual(false, JpxMct.AreComponentsCompatible(components));
        }

        [Test]
        public void AreComponentsCompatible_DifferentPrecision()
        {
            var components = new[] { Component(8), Component(12), Component(8) };

            Assert.AreEqual(false, JpxMct.AreComponentsCompatible(components));
        }

        [Test]
        public void AreComponentsCompatible_TooFewComponents()
        {
            var components = new[] { Component(8), Component(8) };

            Assert.AreEqual(false, JpxMct.AreComponentsCompatible(components));
        }

        // Forward RCT, ITU-T T.800 (06/2019) Section G.2.1 Equations G-3 to G-5.
        private static void ForwardRct(int i0, int i1, int i2, out int y0, out int y1, out int y2)
        {
            y0 = (int)Math.Floor((i0 + 2.0 * i1 + i2) / 4);
            y1 = i2 - i1;
            y2 = i0 - i1;
        }

        // Forward ICT, ITU-T T.800 (06/2019) Section G.3.1 Equations G-9 to G-11 with the
        // Table G.1 coefficients. Cb and Cr are expressed through Y so that the weights match the
        // luminance weights exactly.
        private static void ForwardIct(double i0, double i1, double i2, out double y0, out double y1, out double y2)
        {
            y0 = 0.299 * i0 + 0.587 * i1 + 0.114 * i2;
            y1 = (i2 - y0) / 1.772;
            y2 = (i0 - y0) / 1.402;
        }

        [Test]
        public void InverseArray_RctRecoversRandomIntegersExactly()
        {
            // The RCT is exactly reversible (Section G.2). Forward transformed random integers
            // must be recovered bit-exactly by the inverse.
            const int Count = 1003;
            var random = new Random(1);

            var expected0 = new float[Count];
            var expected1 = new float[Count];
            var expected2 = new float[Count];
            var planes = new[] { new float[Count], new float[Count], new float[Count] };

            for (var i = 0; i < Count; i++)
            {
                // 12-bit centred transform input range
                var i0 = random.Next(-2048, 2048);
                var i1 = random.Next(-2048, 2048);
                var i2 = random.Next(-2048, 2048);

                expected0[i] = i0;
                expected1[i] = i1;
                expected2[i] = i2;

                ForwardRct(i0, i1, i2, out var y0, out var y1, out var y2);
                planes[0][i] = y0;
                planes[1][i] = y1;
                planes[2][i] = y2;
            }

            JpxMct.InverseArray(planes, Count, reversible: true);

            Assert.AreEqual(expected0, planes[0]);
            Assert.AreEqual(expected1, planes[1]);
            Assert.AreEqual(expected2, planes[2]);
        }

        [Test]
        [TestCase(0)]
        [TestCase(3)]
        [TestCase(4)] // Breakpoint between one and two Vector128<float>
        [TestCase(5)]
        [TestCase(7)]
        [TestCase(8)] // Breakpoint between one and two Vector256<float>
        [TestCase(9)]
        [TestCase(1003)]
        public void InverseArray_IctRoundTripWithinTolerance(int count)
        {
            // The ICT is irreversible: the Table G.2 inverse coefficients are rounded to five
            // decimals, so forward + inverse only reproduces the input approximately.
            const float Tolerance = 0.01f;
            var random = new Random(2);

            var expected = new float[3][] { new float[count], new float[count], new float[count] };
            var planes = new[] { new float[count], new float[count], new float[count] };

            for (var i = 0; i < count; i++)
            {
                // 8-bit centred transform input range
                var i0 = random.Next(-128, 128);
                var i1 = random.Next(-128, 128);
                var i2 = random.Next(-128, 128);

                expected[0][i] = i0;
                expected[1][i] = i1;
                expected[2][i] = i2;

                ForwardIct(i0, i1, i2, out var y0, out var y1, out var y2);
                planes[0][i] = (float)y0;
                planes[1][i] = (float)y1;
                planes[2][i] = (float)y2;
            }

            JpxMct.InverseArray(planes, count, reversible: false);

            for (var component = 0; component < 3; component++)
            {
                for (var i = 0; i < count; i++)
                {
                    Assert.AreEqual(expected[component][i], planes[component][i], Tolerance,
                        "Component " + component + " sample " + i);
                }
            }
        }

        [Test]
        public void InverseArray_RctMatchesPerSampleMethod()
        {
            AssertMatchesPerSampleMethod(reversible: true);
        }

        [Test]
        public void InverseArray_IctMatchesPerSampleMethod()
        {
            AssertMatchesPerSampleMethod(reversible: false);
        }

        // The batch transform must produce results identical to the per-sample methods, in both
        // the vectorized part and the scalar tail (the odd count ensures there is a tail).
        private static void AssertMatchesPerSampleMethod(bool reversible)
        {
            const int Count = 37;
            var random = new Random(3);

            var planes = new[] { new float[Count], new float[Count], new float[Count] };
            var expected0 = new float[Count];
            var expected1 = new float[Count];
            var expected2 = new float[Count];

            for (var i = 0; i < Count; i++)
            {
                planes[0][i] = (float)(random.NextDouble() * 2000 - 1000);
                planes[1][i] = (float)(random.NextDouble() * 2000 - 1000);
                planes[2][i] = (float)(random.NextDouble() * 2000 - 1000);

                if (reversible)
                {
                    JpxMct.InverseRct(planes[0][i], planes[1][i], planes[2][i],
                        out expected0[i], out expected1[i], out expected2[i]);
                }
                else
                {
                    JpxMct.InverseIct(planes[0][i], planes[1][i], planes[2][i],
                        out expected0[i], out expected1[i], out expected2[i]);
                }
            }

            JpxMct.InverseArray(planes, Count, reversible);

            Assert.AreEqual(expected0, planes[0]);
            Assert.AreEqual(expected1, planes[1]);
            Assert.AreEqual(expected2, planes[2]);
        }

        [Test]
        public void InverseArray_TransformsOnlyCountSamples()
        {
            var planes = new[]
            {
                new float[] { 10, 20, 99 },
                new float[] { 4, 8, 88 },
                new float[] { -4, -8, 77 },
            };

            JpxMct.InverseArray(planes, 2, reversible: true);

            Assert.AreEqual(99f, planes[0][2]);
            Assert.AreEqual(88f, planes[1][2]);
            Assert.AreEqual(77f, planes[2][2]);
        }

        [Test]
        public void InverseArray_TooFewPlanesThrows()
        {
            var planes = new[] { new float[4], new float[4] };

            Assert.Throws<JpxException>(() => JpxMct.InverseArray(planes, 4, reversible: true));
        }

        [Test]
        public void InverseArray_TooShortPlaneThrows()
        {
            var planes = new[] { new float[4], new float[2], new float[4] };

            Assert.Throws<JpxException>(() => JpxMct.InverseArray(planes, 4, reversible: false));
        }
    }
}
