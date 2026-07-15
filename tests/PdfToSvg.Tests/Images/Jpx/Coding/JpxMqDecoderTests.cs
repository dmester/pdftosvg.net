// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.Coding;
using System;

namespace PdfToSvg.Tests.Images.Jpx.Coding
{
    public class JpxMqDecoderTests
    {
        [Test]
        public void Decode_MixedDecisionSequence()
        {
            // 32 decisions.
            var data = new byte[] { 0x01, 0xCC, 0x93, 0x72, 0xFF };
            var expected = Bits("01101001111000100011011110101010");

            Assert.AreEqual(expected, DecodeAll(data, expected.Length));
        }

        [Test]
        public void Decode_AllZeroDecisions()
        {
            // A long run of the more-probable symbol; exercises repeated renormalization and the
            // 0xFF stream-extension path in ByteIn.
            var data = new byte[] { 0x9F, 0xFF };
            var expected = Bits("000000000000000000000000");

            Assert.AreEqual(expected, DecodeAll(data, expected.Length));
        }

        [Test]
        public void Decode_AllOneDecisions()
        {
            // A run of the less-probable symbol (MPS starts at 0). The codeword contains a 0xFF
            // byte, exercising the ByteIn stuffing branch.
            var data = new byte[] { 0xFF, 0x7F, 0x01 };
            var expected = Bits("11111111111111111111");

            Assert.AreEqual(expected, DecodeAll(data, expected.Length));
        }

        [Test]
        public void Decode_AlternatingDecisions()
        {
            // 40 alternating decisions.
            var data = new byte[] { 0x15, 0x80, 0x40, 0x00, 0x00, 0x7F, 0x00 };
            var expected = Bits("0101010101010101010101010101010101010101");

            Assert.AreEqual(expected, DecodeAll(data, expected.Length));
        }

        [Test]
        public void Decode_TerminatedStreamExtendsWithFf()
        {
            var data = new byte[] { 0x9F, 0xFF };
            var decoder = new JpxMqDecoder(new ArraySegment<byte>(data));

            var entry = new JpxMqContextEntry();

            for (var i = 0; i < 24; i++)
            {
                Assert.AreEqual(0, decoder.DecodeBit(ref entry));
            }

            // Reading far past the end stays in bounds (extension bytes are synthesized).
            Assert.DoesNotThrow(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    decoder.DecodeBit(ref entry);
                }
            });
        }

        private static int[] DecodeAll(byte[] data, int decisionCount)
        {
            var decoder = new JpxMqDecoder(new ArraySegment<byte>(data));

            var entry = new JpxMqContextEntry();
            var decisions = new int[decisionCount];

            for (var i = 0; i < decisionCount; i++)
            {
                decisions[i] = decoder.DecodeBit(ref entry);
            }

            return decisions;
        }

        private static int[] Bits(string bits)
        {
            var result = new int[bits.Length];
            for (var i = 0; i < bits.Length; i++)
            {
                result[i] = bits[i] - '0';
            }
            return result;
        }
    }
}
