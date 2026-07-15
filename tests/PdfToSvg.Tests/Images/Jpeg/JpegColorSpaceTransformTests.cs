// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpeg;
using System;

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal class JpegColorSpaceTransformTests
    {
        private const int BlockSize = 64;

        // Rounding differences between the vectorized and scalar code paths.
        private const int Tolerance = 2;

        [Test]
        public void CmykBlocksToYcc_ConvertsDeinterleavedBlocks()
        {
            var blocks = CreateVariedBlocks(componentCount: 4, mcuCount: 2, seed: 1);
            var input = (float[])blocks.Clone();

            var outputBlocks = JpegColorSpaceTransform.CmykBlocksToYcc(blocks, blockCount: 4 * 2);

            // 4 CMYK blocks per MCU become 3 YCbCr blocks.
            Assert.AreEqual(3 * 2, outputBlocks);

            AssertCmykMatchesReference(input, blocks, mcuCount: 2);
        }

        [Test]
        public void CmykBlocksToYcc_HandlesSolidBlocks()
        {
            // One MCU where every block is a single (but different) value, exercising the solid-block fast path.
            var blocks = new float[4 * BlockSize];
            FillBlock(blocks, 0, 200); // C
            FillBlock(blocks, 1, 30);  // M
            FillBlock(blocks, 2, 90);  // Y
            FillBlock(blocks, 3, 10);  // K

            var input = (float[])blocks.Clone();

            var outputBlocks = JpegColorSpaceTransform.CmykBlocksToYcc(blocks, blockCount: 4);

            Assert.AreEqual(3, outputBlocks);
            AssertCmykMatchesReference(input, blocks, mcuCount: 1);
        }

        [Test]
        public void RgbBlocksToYcc_ConvertsDeinterleavedBlocks()
        {
            var blocks = CreateVariedBlocks(componentCount: 3, mcuCount: 2, seed: 3);
            var input = (float[])blocks.Clone();

            var outputBlocks = JpegColorSpaceTransform.RgbBlocksToYcc(blocks, blockCount: 3 * 2);

            // The conversion is 3 blocks to 3 blocks in place.
            Assert.AreEqual(3 * 2, outputBlocks);

            AssertRgbMatchesReference(input, blocks, mcuCount: 2);
        }

        [Test]
        public void RgbBlocksToYcc_HandlesSolidBlocks()
        {
            // One MCU where every block is a single (but different) value, exercising the solid-block fast path.
            var blocks = new float[3 * BlockSize];
            FillBlock(blocks, 0, 200); // R
            FillBlock(blocks, 1, 30);  // G
            FillBlock(blocks, 2, 90);  // B

            var input = (float[])blocks.Clone();

            var outputBlocks = JpegColorSpaceTransform.RgbBlocksToYcc(blocks, blockCount: 3);

            Assert.AreEqual(3, outputBlocks);
            AssertRgbMatchesReference(input, blocks, mcuCount: 1);
        }

        [Test]
        public void YcckBlocksToYcc_ConvertsDeinterleavedBlocks()
        {
            var blocks = CreateVariedBlocks(componentCount: 4, mcuCount: 2, seed: 2);
            var input = (float[])blocks.Clone();

            var outputBlocks = JpegColorSpaceTransform.YcckBlocksToYcc(blocks, blockCount: 4 * 2);

            Assert.AreEqual(3 * 2, outputBlocks);

            AssertYcckMatchesReference(input, blocks, mcuCount: 2);
        }

        private static void AssertCmykMatchesReference(float[] input, float[] output, int mcuCount)
        {
            for (var mcu = 0; mcu < mcuCount; mcu++)
            {
                var cBase = (mcu * 4 + 0) * BlockSize;
                var mBase = (mcu * 4 + 1) * BlockSize;
                var yBase = (mcu * 4 + 2) * BlockSize;
                var kBase = (mcu * 4 + 3) * BlockSize;

                var yccYBase = (mcu * 3 + 0) * BlockSize;
                var yccCbBase = (mcu * 3 + 1) * BlockSize;
                var yccCrBase = (mcu * 3 + 2) * BlockSize;

                for (var i = 0; i < BlockSize; i++)
                {
                    CmykToYccReference(input[cBase + i], input[mBase + i], input[yBase + i], input[kBase + i],
                        out var expectedY, out var expectedCb, out var expectedCr);

                    AssertClose(expectedY, output[yccYBase + i], mcu, i, "Y");
                    AssertClose(expectedCb, output[yccCbBase + i], mcu, i, "Cb");
                    AssertClose(expectedCr, output[yccCrBase + i], mcu, i, "Cr");
                }
            }
        }

        private static void AssertRgbMatchesReference(float[] input, float[] output, int mcuCount)
        {
            for (var mcu = 0; mcu < mcuCount; mcu++)
            {
                var rBase = (mcu * 3 + 0) * BlockSize;
                var gBase = (mcu * 3 + 1) * BlockSize;
                var bBase = (mcu * 3 + 2) * BlockSize;

                for (var i = 0; i < BlockSize; i++)
                {
                    JpegColorSpaceTransform.RgbToYcc(input[rBase + i], input[gBase + i], input[bBase + i],
                        out var expectedY, out var expectedCb, out var expectedCr);

                    AssertClose(MathUtils.Clamp(expectedY, 0f, 255f), output[rBase + i], mcu, i, "Y");
                    AssertClose(MathUtils.Clamp(expectedCb, 0f, 255f), output[gBase + i], mcu, i, "Cb");
                    AssertClose(MathUtils.Clamp(expectedCr, 0f, 255f), output[bBase + i], mcu, i, "Cr");
                }
            }
        }

        private static void AssertYcckMatchesReference(float[] input, float[] output, int mcuCount)
        {
            for (var mcu = 0; mcu < mcuCount; mcu++)
            {
                var yBase = (mcu * 4 + 0) * BlockSize;
                var cbBase = (mcu * 4 + 1) * BlockSize;
                var crBase = (mcu * 4 + 2) * BlockSize;
                var kBase = (mcu * 4 + 3) * BlockSize;

                var yccYBase = (mcu * 3 + 0) * BlockSize;
                var yccCbBase = (mcu * 3 + 1) * BlockSize;
                var yccCrBase = (mcu * 3 + 2) * BlockSize;

                for (var i = 0; i < BlockSize; i++)
                {
                    YcckToYccReference(input[yBase + i], input[cbBase + i], input[crBase + i], input[kBase + i],
                        out var expectedY, out var expectedCb, out var expectedCr);

                    AssertClose(expectedY, output[yccYBase + i], mcu, i, "Y");
                    AssertClose(expectedCb, output[yccCbBase + i], mcu, i, "Cb");
                    AssertClose(expectedCr, output[yccCrBase + i], mcu, i, "Cr");
                }
            }
        }

        private static void CmykToYccReference(float c, float m, float y, float k, out float yccY, out float yccCb, out float yccCr)
        {
            DeviceCmykColorSpace.ToRgb(c * (1f / 255), m * (1f / 255), y * (1f / 255), k * (1f / 255),
                out var rgbR, out var rgbG, out var rgbB);

            JpegColorSpaceTransform.RgbToYcc(rgbR * 255, rgbG * 255, rgbB * 255, out var fy, out var fcb, out var fcr);

            yccY = (float)MathUtils.Clamp(fy, 0f, 255f);
            yccCb = (float)MathUtils.Clamp(fcb, 0f, 255f);
            yccCr = (float)MathUtils.Clamp(fcr, 0f, 255f);
        }

        private static void YcckToYccReference(float ycckY, float ycckCb, float ycckCr, float ycckK, out float yccY, out float yccCb, out float yccCr)
        {
            JpegColorSpaceTransform.YccToRgb(ycckY, ycckCb, ycckCr, out var ycckR, out var ycckG, out var ycckB);

            DeviceCmykColorSpace.ToRgb(
                (255 - ycckR) * (1f / 255), (255 - ycckG) * (1f / 255),
                (255 - ycckB) * (1f / 255), ycckK * (1f / 255),
                out var rgbR, out var rgbG, out var rgbB);

            JpegColorSpaceTransform.RgbToYcc(rgbR * 255, rgbG * 255, rgbB * 255, out var fy, out var fcb, out var fcr);

            yccY = (float)MathUtils.Clamp(fy, 0f, 255f);
            yccCb = (float)MathUtils.Clamp(fcb, 0f, 255f);
            yccCr = (float)MathUtils.Clamp(fcr, 0f, 255f);
        }

        private static void AssertClose(float expected, float actual, int mcu, int index, string component)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(Tolerance),
                $"MCU {mcu}, sample {index}, component {component}");
        }

        private static float[] CreateVariedBlocks(int componentCount, int mcuCount, int seed)
        {
            var blocks = new float[componentCount * mcuCount * BlockSize];
            var random = new Random(seed);

            for (var i = 0; i < blocks.Length; i++)
            {
                blocks[i] = (float)random.Next(0, 256);
            }

            return blocks;
        }

        private static void FillBlock(float[] blocks, int blockIndex, float value)
        {
            var baseIndex = blockIndex * BlockSize;
            for (var i = 0; i < BlockSize; i++)
            {
                blocks[baseIndex + i] = value;
            }
        }
    }
}
