// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                input[i] = (short)random.Next(-128, 128);
            }

            ReferenceDct(input, dcted);
            ReferenceIdct(dcted, output);

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(input[i], output[i], 5d, "Index {0}", i);
            }
        }

        [Test]
        public void FastDct()
        {
            var data = new short[64];
            var refdct = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                data[i] = (short)random.Next(-128, 128);
            }

            ReferenceDct(data, refdct);

            JpegDct.Forward(data);

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(refdct[i], data[i], 5d, "Index {0}", i);
            }
        }

        [Test]
        public void FastIdct()
        {
            var data = new short[64];
            var refidct = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                data[i] = (short)random.Next(-128, 128);
            }

            ReferenceIdct(data, refidct);

            JpegDct.Inverse(data);

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(refidct[i], data[i], 5d, "Index {0}", i);
            }
        }

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

                output[o] = (short)(sum / 4 + 128);
            }
        }
    }
}


