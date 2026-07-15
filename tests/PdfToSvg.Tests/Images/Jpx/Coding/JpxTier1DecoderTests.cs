// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.Coding;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;

namespace PdfToSvg.Tests.Images.Jpx.Coding
{
    public class JpxTier1DecoderTests
    {
        [Test]
        public void Decode_NotIncluded_NoCoefficients()
        {
            var codeBlock = CreateCodeBlock(16, 16, magnitudeBitPlanes: 8);

            var coefficients = new JpxTier1Decoder().Decode(codeBlock, useMidpointReconstruction: false);

            Assert.IsNull(coefficients);
        }

        [Test]
        public void Decode_AllBitPlanesZero_NoCoefficients()
        {
            // A zero bit-plane count equal to the number of magnitude bit-planes leaves no coded
            // bit-planes; may appear in corrupt streams
            var codeBlock = CreateCodeBlock(16, 16, magnitudeBitPlanes: 8);
            Include(codeBlock, zeroBitPlanes: 8);
            AddSegment(codeBlock, codingPasses: 1, terminated: true, new byte[] { 0x00 });

            var coefficients = new JpxTier1Decoder().Decode(codeBlock, useMidpointReconstruction: false);

            Assert.IsNull(coefficients);
        }

        [Test]
        public void Decode_EmptyCodeBlock_NoCoefficients()
        {
            var codeBlock = CreateCodeBlock(0, 16, magnitudeBitPlanes: 8);
            Include(codeBlock, zeroBitPlanes: 0);
            AddSegment(codeBlock, codingPasses: 1, terminated: true, new byte[] { 0x00 });

            var coefficients = new JpxTier1Decoder().Decode(codeBlock, useMidpointReconstruction: false);

            Assert.IsNull(coefficients);
        }

        [Test]
        public void Decode_ExcessivePassCount_Clamped()
        {
            // 1 bit-plane allows a single cleanup pass; the extra signalled passes are ignored
            var codeBlock = CreateCodeBlock(8, 8, magnitudeBitPlanes: 1);
            Include(codeBlock, zeroBitPlanes: 0);
            AddSegment(codeBlock, codingPasses: 30, terminated: true, new byte[] { 0x12, 0x34, 0x56 });

            var coefficients = new JpxTier1Decoder().Decode(codeBlock, useMidpointReconstruction: false);

            Assert.IsNotNull(coefficients);
            Assert.GreaterOrEqual(coefficients!.Length, 64);
        }

        [Test]
        public void Decode_ReusesCoefficientBuffer()
        {
            // The returned buffer is only valid until the next Decode call: consecutive calls that fit in the
            // buffer return the same instance
            var decoder = new JpxTier1Decoder();

            var first = CreateCodeBlock(8, 8, magnitudeBitPlanes: 1);
            Include(first, zeroBitPlanes: 0);
            AddSegment(first, codingPasses: 1, terminated: true, new byte[] { 0x12, 0x34, 0x56 });

            var second = CreateCodeBlock(4, 4, magnitudeBitPlanes: 1);
            Include(second, zeroBitPlanes: 0);
            AddSegment(second, codingPasses: 1, terminated: true, new byte[] { 0x65, 0x43, 0x21 });

            var firstCoefficients = decoder.Decode(first, useMidpointReconstruction: false);
            var secondCoefficients = decoder.Decode(second, useMidpointReconstruction: false);

            Assert.IsNotNull(firstCoefficients);
            Assert.AreSame(firstCoefficients, secondCoefficients);
        }

        // Fuzz-style robustness tests: whatever the segment bytes are, the decoder must not throw and must produce a
        // coefficient array of the right size. Corrupt PDFs are common and Tier-1 decoding should degrade rather than
        // fail
        [TestCase(false, false, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        [TestCase(true, true, true, true)]
        public void Decode_RandomData_DoesNotThrow(
            bool bypass, bool verticallyCausal, bool segmentationSymbols, bool resetContexts)
        {
            var style = new JpxCodeBlockStyle
            {
                SelectiveArithmeticCodingBypass = bypass,
                VerticallyCausalContext = verticallyCausal,
                SegmentationSymbolsAreUsed = segmentationSymbols,
                ResetContextProbabilitiesOnCodingPassBoundaries = resetContexts,
            };

            var random = new Random(0);
            var decoder = new JpxTier1Decoder();

            foreach (var subBandType in new[]
            {
                JpxSubBandType.LL, JpxSubBandType.HL, JpxSubBandType.LH, JpxSubBandType.HH,
            })
            {
                for (var iteration = 0; iteration < 10; iteration++)
                {
                    var width = 1 + random.Next(20);
                    var height = 1 + random.Next(20);

                    var codeBlock = CreateCodeBlock(width, height, magnitudeBitPlanes: 8, style, subBandType);
                    Include(codeBlock, zeroBitPlanes: random.Next(3));

                    // Segment structure of a bypass stream per ITU-T T.800 (06/2019) Table D.9:
                    // passes 0-9 MQ, then alternating raw and MQ segments
                    var segmentData = new byte[random.Next(40)];
                    random.NextBytes(segmentData);
                    AddSegment(codeBlock, codingPasses: 10, terminated: true, segmentData);

                    segmentData = new byte[random.Next(20)];
                    random.NextBytes(segmentData);
                    AddSegment(codeBlock, codingPasses: 2, terminated: true, segmentData);

                    segmentData = new byte[random.Next(20)];
                    random.NextBytes(segmentData);
                    AddSegment(codeBlock, codingPasses: 1, terminated: true, segmentData);

                    var coefficients = decoder.Decode(codeBlock, useMidpointReconstruction: true);

                    Assert.IsNotNull(coefficients);
                    Assert.GreaterOrEqual(coefficients!.Length, width * height);
                }
            }
        }

        // The two code-blocks of the worked decoding example in ITU-T T.800 (06/2019) Section J.10.4. The decoded
        // coefficients are given in the spec, and the arithmetic decoding is traced decision by decision in Tables
        // J.22 and J.23. The decoder returns fixed-point coefficients with one fractional bit, so the expected
        // values are the spec coefficients multiplied by 2.

        [Test]
        public void Decode_T800AnnexJ10FirstCodeBlock()
        {
            // The 1x5 LL band code-block: Mb = 9 (Table J.20 footnote a), 3 zero bit-planes and 16 coding passes
            // signalled in the first packet header (Table J.20).
            var codeBlock = CreateCodeBlock(1, 5, magnitudeBitPlanes: 9);
            Include(codeBlock, zeroBitPlanes: 3);
            AddSegment(codeBlock, codingPasses: 16, terminated: false,
                new byte[] { 0x01, 0x8F, 0x0D, 0xC8, 0x75, 0x5D });

            var coefficients = new JpxTier1Decoder().Decode(codeBlock, useMidpointReconstruction: false);

            Assert.IsNotNull(coefficients);
            Assert.AreEqual(
                new[] { -26 * 2, -22 * 2, -30 * 2, -32 * 2, -19 * 2 },
                CodeBlockArea(coefficients!, 5));
        }

        [Test]
        public void Decode_T800AnnexJ10SecondCodeBlock()
        {
            // The 1x4 1LH band code-block: Mb = 10 (Table J.21 footnote a), 7 zero bit-planes and 7 coding passes
            // signalled in the second packet header (Table J.21).
            var codeBlock = CreateCodeBlock(1, 4, magnitudeBitPlanes: 10, subBandType: JpxSubBandType.LH);
            Include(codeBlock, zeroBitPlanes: 7);
            AddSegment(codeBlock, codingPasses: 7, terminated: false, new byte[] { 0x0F, 0xB1, 0x76 });

            var coefficients = new JpxTier1Decoder().Decode(codeBlock, useMidpointReconstruction: false);

            Assert.IsNotNull(coefficients);
            Assert.AreEqual(
                new[] { 1 * 2, 5 * 2, 1 * 2, 0 },
                CodeBlockArea(coefficients!, 4));
        }

        /// <summary>The decoded coefficient buffer may be larger than the code-block area; trims the excess.</summary>
        private static int[] CodeBlockArea(int[] coefficients, int area)
        {
            var result = new int[area];
            Array.Copy(coefficients, result, area);
            return result;
        }

        private static JpxCodeBlock CreateCodeBlock(
            int width, int height, int magnitudeBitPlanes,
            JpxCodeBlockStyle style = default, JpxSubBandType subBandType = JpxSubBandType.LL)
        {
            var subBand = new JpxResolutionLevelSubBand
            {
                Type = subBandType,
                MagnitudeBitPlanes = magnitudeBitPlanes,
                CodeBlockStyle = style,
            };

            return new JpxCodeBlock(subBand, 0, 0, 0, 0, width, height, 0, 0);
        }

        private static void AddSegment(JpxCodeBlock codeBlock, int codingPasses, bool terminated, byte[] data)
        {
            var segment = new JpxCodeBlockSegment
            {
                CodingPasses = codingPasses,
                Terminated = terminated,
            };
            segment.Data.Append(new ArraySegment<byte>(data));
            codeBlock.Segments.Add(segment);
            codeBlock.CodingPasses += codingPasses;
        }

        private static void Include(JpxCodeBlock codeBlock, int zeroBitPlanes)
        {
            codeBlock.Included = true;
            codeBlock.ZeroBitPlanes = zeroBitPlanes;
        }
    }
}
