// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jbig2;
using PdfToSvg.Imaging.Jbig2.Coding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jbig2
{
    public class JbigArithmeticDecoderTests
    {
        // Test data from ITU-T_T_88__08_2018 section H.2

        private static byte[] EncodedTestData = new byte[]
        {
            0x84, 0xC7, 0x3B, 0xFC, 0xE1, 0xA1, 0x43, 0x04,
            0x02, 0x20, 0x00, 0x00, 0x41, 0x0D, 0xBB, 0x86,
            0xF4, 0x31, 0x7F, 0xFF, 0x88, 0xFF, 0x37, 0x47,
            0x1A, 0xDB, 0x6A, 0xDF, 0xFF, 0xAC,
        };

        private static byte[] DecodedTestData = new byte[]
        {
            0x00, 0x02, 0x00, 0x51, 0x00, 0x00, 0x00, 0xC0,
            0x03, 0x52, 0x87, 0x2A, 0xAA, 0xAA, 0xAA, 0xAA,
            0x82, 0xC0, 0x20, 0x00, 0xFC, 0xD7, 0x9E, 0xF6,
            0xBF, 0x7F, 0xED, 0x90, 0x4F, 0x46, 0xA3, 0xBF,
        };

        [Test]
        public void Decode()
        {
            var actualDecoded = new byte[DecodedTestData.Length];

            var decoder = new JbigArithmeticDecoder(EncodedTestData, 0, EncodedTestData.Length);
            var cx = new JbigArithmeticContext(1);

            for (var i = 0; i < actualDecoded.Length; i++)
            {
                var number = 0;

                for (var j = 0; j < 8; j++)
                {
                    number = (number << 1) | decoder.DecodeBit(cx);
                }

                actualDecoded[i] = (byte)number;
            }

            Assert.AreEqual(DecodedTestData, actualDecoded);
        }

        [TestCase(1)] // Without 0xac
        [TestCase(2)] // Without 0xffac
        public void Decode_NonTerminatedData(int missingBytes)
        {
            var actualDecoded = new byte[DecodedTestData.Length];

            var decoder = new JbigArithmeticDecoder(EncodedTestData, 0, EncodedTestData.Length - missingBytes);
            var cx = new JbigArithmeticContext(1);

            for (var i = 0; i < actualDecoded.Length; i++)
            {
                var number = 0;

                for (var j = 0; j < 8; j++)
                {
                    number = (number << 1) | decoder.DecodeBit(cx);
                }

                actualDecoded[i] = (byte)number;
            }

            Assert.AreEqual(DecodedTestData, actualDecoded);
        }

        [Test]
        public void ThrowsWhenReadingPastEnd()
        {
            var decoder = new JbigArithmeticDecoder(EncodedTestData, 0, EncodedTestData.Length);
            var cx = new JbigArithmeticContext(1);

            for (var i = 0; i < DecodedTestData.Length * 8; i++)
            {
                decoder.DecodeBit(cx);
            }

            Assert.Throws<EndOfStreamException>(() =>
            {
                // The exception is not entirely accurate, so lets read a couple of bits
                for (var i = 0; i < 1000; i++)
                {
                    decoder.DecodeBit(cx);
                }
            });
        }

        [Test]
        public void MalformedData()
        {
            var encoded = new byte[10];

            var random = new Random(0);

            for (var i = 0; i < 100; i++)
            {
                random.NextBytes(encoded);

                var decoder = new JbigArithmeticDecoder(encoded, 0, encoded.Length);
                var cx = new JbigArithmeticContext(1);

                Assert.Throws<EndOfStreamException>(() =>
                {
                    // The exception is not entirely accurate, so lets read a couple of bits
                    for (var i = 0; i < 10000; i++)
                    {
                        decoder.DecodeBit(cx);
                    }
                });
            }
        }
    }
}
