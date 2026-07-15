// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal class JpegBlockTilerTests
    {
        private const int BlockSide = 8;
        private const int BlockLength = BlockSide * BlockSide;

        private static float[][] RandomPlanes(int channelCount, int sampleCount, int seed)
        {
            var random = new Random(seed);
            var planes = new float[channelCount][];

            for (var c = 0; c < channelCount; c++)
            {
                planes[c] = new float[sampleCount];
                for (var i = 0; i < sampleCount; i++)
                {
                    planes[c][i] = (float)random.NextDouble();
                }
            }

            return planes;
        }

        private static float[] ReadAllBlocks(JpegBlockTiler reader, float[] batchBuffer)
        {
            var allBlocks = new List<float>();

            foreach (var blockCount in reader.ReadBlocks(batchBuffer))
            {
                allBlocks.AddRange(batchBuffer.Take(blockCount * BlockLength));
            }

            return allBlocks.ToArray();
        }

        // Reference implementation: gathers the blocks of the whole image in MCU order (channels
        // interleaved per MCU, MCUs row-major), replicating the last column and row into partial
        // edge blocks, decoding the samples and scaling them to a clamped [0, 255] domain.
        private static float[] ReferenceBlocks(float[][] planes, int width, int height, DecodeArray decodeArray, float maxSourceValue)
        {
            var mcusPerRow = (width + BlockSide - 1) / BlockSide;
            var mcusPerColumn = (height + BlockSide - 1) / BlockSide;

            var result = new float[mcusPerRow * mcusPerColumn * planes.Length * BlockLength];
            var cursor = 0;

            for (var mcuY = 0; mcuY < mcusPerColumn; mcuY++)
            {
                for (var mcuX = 0; mcuX < mcusPerRow; mcuX++)
                {
                    for (var c = 0; c < planes.Length; c++)
                    {
                        var range = decodeArray[c];

                        for (var row = 0; row < BlockSide; row++)
                        {
                            for (var col = 0; col < BlockSide; col++)
                            {
                                var x = Math.Min(mcuX * BlockSide + col, width - 1);
                                var y = Math.Min(mcuY * BlockSide + row, height - 1);

                                var decoded = range.Decode(planes[c][y * width + x] * maxSourceValue) * 255f;
                                result[cursor++] = Math.Max(0f, Math.Min(255f, decoded));
                            }
                        }
                    }
                }
            }

            return result;
        }

        [Test]
        public void ReadBlocks_MatchesReference()
        {
            // Odd dimensions exercise partial blocks along both the right and bottom edges, and
            // the small batch buffer exercises batch splitting. The decode array inverts one
            // channel and compresses another.
            const int Width = 17;
            const int Height = 9;
            const float MaxSourceValue = 255f;

            var decodeArray = new DecodeArray(8, new[] { 0f, 1f, 1f, 0f, .2f, .8f });
            var planes = RandomPlanes(channelCount: 3, sampleCount: Width * Height, seed: 1);

            var reader = new JpegBlockTiler(planes, Width, Height, MaxSourceValue, decodeArray);
            var batchBuffer = new float[2 * 3 * BlockLength];

            var actual = ReadAllBlocks(reader, batchBuffer);
            var expected = ReferenceBlocks(planes, Width, Height, decodeArray, MaxSourceValue);

            Assert.AreEqual(expected.Length, actual.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                // The reference computes the affine decode in two steps, so allow for floating
                // point rounding differences against the reader's fused single step.
                Assert.That(actual[i], Is.EqualTo(expected[i]).Within(0.01f), "Sample " + i);
            }
        }

        [Test]
        public void ReadBlocks_EmitsWholeMcusPerBatch()
        {
            // A 24x8 image with 3 channels holds 3 MCUs of 3 blocks each. A buffer with room for
            // 8 blocks must be filled with only 2 whole MCUs per batch.
            const int Width = 24;
            const int Height = 8;

            var decodeArray = new DecodeArray(8, new[] { 0f, 1f, 0f, 1f, 0f, 1f });
            var planes = RandomPlanes(channelCount: 3, sampleCount: Width * Height, seed: 2);

            var reader = new JpegBlockTiler(planes, Width, Height, 255f, decodeArray);
            var batchBuffer = new float[8 * BlockLength];

            var batchSizes = reader.ReadBlocks(batchBuffer).ToList();

            Assert.AreEqual(new[] { 6, 3 }, batchSizes);
        }

        [Test]
        public void ReadBlocks_ReplicatesEdgePixels()
        {
            // In a 9x9 image, all but the first column of the second horizontal block fall
            // outside the image and must replicate the last column.
            const int Width = 9;
            const int Height = 9;

            var decodeArray = new DecodeArray(8, new[] { 0f, 1f });
            var planes = RandomPlanes(channelCount: 1, sampleCount: Width * Height, seed: 3);

            var reader = new JpegBlockTiler(planes, Width, Height, 255f, decodeArray);
            var batchBuffer = new float[4 * BlockLength];

            var blocks = ReadAllBlocks(reader, batchBuffer);

            // Block 1 covers columns 8-15, which all resolve to the last image column.
            var block1 = blocks.Skip(1 * BlockLength).Take(BlockLength).ToArray();

            for (var row = 0; row < BlockSide; row++)
            {
                var expected = block1[row * BlockSide];

                for (var col = 1; col < BlockSide; col++)
                {
                    Assert.AreEqual(expected, block1[row * BlockSide + col], $"Row {row}, column {col}");
                }
            }
        }

        [Test]
        public void ReadBlocks_ClampsOutput()
        {
            // A decode range of [-1, 2] produces values in [-255, 510] which must clamp to [0, 255].
            const int Width = 8;
            const int Height = 8;

            var decodeArray = new DecodeArray(8, new[] { -1f, 2f });

            var plane = new float[Width * Height];
            for (var i = 0; i < plane.Length; i++)
            {
                plane[i] = i % 2 == 0 ? 0f : 1f;
            }

            var reader = new JpegBlockTiler(new[] { plane }, Width, Height, 255f, decodeArray);
            var batchBuffer = new float[BlockLength];

            var blocks = ReadAllBlocks(reader, batchBuffer);

            for (var i = 0; i < blocks.Length; i++)
            {
                Assert.AreEqual(i % 2 == 0 ? 0f : 255f, blocks[i], "Sample " + i);
            }
        }

        [Test]
        public void ReadBlocks_BufferSmallerThanMcuThrows()
        {
            var decodeArray = new DecodeArray(8, new[] { 0f, 1f, 0f, 1f, 0f, 1f });
            var planes = RandomPlanes(channelCount: 3, sampleCount: 64, seed: 4);

            var reader = new JpegBlockTiler(planes, 8, 8, 255f, decodeArray);
            var batchBuffer = new float[2 * BlockLength];

            Assert.Throws<ArgumentException>(() => reader.ReadBlocks(batchBuffer).ToList());
        }
    }
}
