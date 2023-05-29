// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegDct
    {
        // This class is a major performance bottleneck. There are more efficient DCT algorithms that can be
        // implemented.

        private const double OneSqrt2 = 0.70710678118654752440084436210485;

        private readonly static short[] idct;
        private readonly static short[] dct;

        static JpegDct()
        {
            idct = new short[64 * 64];
            dct = new short[64 * 64];

            // A.3.3 FDCT and IDCT (informative)

            for (var o = 0; o < 64; o++)
            {
                for (var i = 0; i < 64; i++)
                {
                    var u = i % 8;
                    var v = i / 8;

                    var x = o % 8;
                    var y = o / 8;

                    var Cu = u == 0 ? OneSqrt2 : 1;
                    var Cv = v == 0 ? OneSqrt2 : 1;

                    idct[(o << 6) | i] =
                        (short)(
                            short.MaxValue *
                            Cu * Cv *
                            Math.Cos((2 * x + 1) * u * Math.PI / 16) *
                            Math.Cos((2 * y + 1) * v * Math.PI / 16)
                        );
                }
            }

            for (var o = 0; o < 64; o++)
            {
                for (var i = 0; i < 64; i++)
                {
                    var u = i % 8;
                    var v = i / 8;

                    var x = o % 8;
                    var y = o / 8;

                    dct[(o << 6) | i] =
                        (short)(
                            short.MaxValue *
                            Math.Cos((2 * x + 1) * u * Math.PI / 16) *
                            Math.Cos((2 * y + 1) * v * Math.PI / 16)
                        );
                }
            }
        }

        public static void Inverse(short[] input, short[] output)
        {
            for (var o = 0; o < 64; o++)
            {
                var sum = 0;

                for (var i = 0; i < 64; i++)
                {
                    sum += idct[(o << 6) | i] * input[i];
                }

                output[o] = (short)((sum >> 17) + 128);
            }
        }

        public static void Forward(short[] input, short[] output)
        {
            for (var o = 0; o < 64; o++)
            {
                var u = o % 8;
                var v = o / 8;

                var Cu = u == 0 ? OneSqrt2 : 1;
                var Cv = v == 0 ? OneSqrt2 : 1;

                var sum = 0;

                for (var i = 0; i < 64; i++)
                {
                    sum += dct[(i << 6) | o] * (input[i] - 128);
                }

                output[o] = (short)((sum >> 17) * Cu * Cv);
            }
        }
    }
}
