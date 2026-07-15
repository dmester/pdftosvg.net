// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;

#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
#endif

namespace PdfToSvg.Imaging.Jpeg
{
    /// <summary>
    /// Creates JPEG 8x8 blocks out of a planar float color channels. The blocks are interleaved with one block from
    /// each channel, then restarts with next block.
    /// </summary>
    internal class JpegBlockTiler
    {
        private const float MaxTargetValue = 255f;
        private const int BlockSide = 8;
        private const int BlockLength = BlockSide * BlockSide;

        private readonly float[][] planes;
        private readonly int width;
        private readonly int height;
        private readonly ScaledDecodeRange[] ranges;

        private readonly struct ScaledDecodeRange(float maxSourceValue, DecodeRange range)
        {
            public readonly float Offset = range.Dmin * MaxTargetValue;
            public readonly float Multiplier = range.Multiplier * maxSourceValue * MaxTargetValue;
        }

        /// <param name="planes">One plane per colour channel with samples normalized to [0, 1]</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <param name="maxSourceValue">Upper bound of the raw sample domain the /Decode ranges are defined against, i.e. 2^bpc - 1</param>
        /// <param name="decodeArray">Decode array operating on input and output value range [0, 1]</param>
        public JpegBlockTiler(float[][] planes, int width, int height, float maxSourceValue, DecodeArray decodeArray)
        {
            if (planes == null) throw new ArgumentNullException(nameof(planes));
            if (decodeArray == null) throw new ArgumentNullException(nameof(decodeArray));
            if (planes.Length == 0)
            {
                throw new ArgumentException("At least one color channel is required", nameof(planes));
            }

            foreach (var plane in planes)
            {
                if (plane.Length < width * height)
                {
                    throw new ArgumentException("Color channel smaller than the image area", nameof(planes));
                }
            }

            this.planes = planes;
            this.width = width;
            this.height = height;

            ranges = new ScaledDecodeRange[planes.Length];
            for (var c = 0; c < planes.Length; c++)
            {
                ranges[c] = new ScaledDecodeRange(maxSourceValue, decodeArray[c]);
            }
        }

        /// <summary>
        /// Fills <paramref name="blocks"/> with as many whole MCUs as fit and yields the number of blocks written,
        /// until the image is exhausted. The block count is always a multiple of the channel count.
        /// </summary>
        public IEnumerable<int> ReadBlocks(float[] blocks)
        {
            var channelCount = planes.Length;
            var batchBlockCount = blocks.Length / (BlockLength * channelCount) * channelCount;

            if (batchBlockCount == 0)
            {
                throw new ArgumentException("The block buffer is too small to hold a whole MCU", nameof(blocks));
            }

            var bufferedBlockCount = 0;

            for (var pixelY = 0; pixelY < height; pixelY += BlockSide)
            {
                for (var pixelX = 0; pixelX < width; pixelX += BlockSide)
                {
                    FillMcu(blocks, bufferedBlockCount * BlockLength, pixelX, pixelY);
                    bufferedBlockCount += channelCount;

                    if (bufferedBlockCount == batchBlockCount)
                    {
                        yield return bufferedBlockCount;
                        bufferedBlockCount = 0;
                    }
                }
            }

            if (bufferedBlockCount > 0)
            {
                yield return bufferedBlockCount;
            }
        }

        private void FillMcu(float[] blocks, int blockOffset, int pixelX, int pixelY)
        {
            var isInteriorBlock =
                pixelX + BlockSide <= width &&
                pixelY + BlockSide <= height;

            for (var i = 0; i < planes.Length; i++, blockOffset += BlockLength)
            {
                if (isInteriorBlock)
                {
                    FillInteriorBlock(planes[i], ranges[i], blocks, blockOffset, pixelY * width + pixelX);
                }
                else
                {
                    FillEdgeBlock(planes[i], ranges[i], blocks, blockOffset, pixelX, pixelY);
                }
            }
        }

        private void FillInteriorBlock(float[] plane, ScaledDecodeRange range, float[] blocks, int blockOffset, int sourceOffset)
        {
#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var planeRef = ref MemoryMarshal.GetArrayDataReference(plane);
                ref var blocksRef = ref MemoryMarshal.GetArrayDataReference(blocks);

                var offsetVector = Vector256.Create(range.Offset);
                var multiplierVector = Vector256.Create(range.Multiplier);
                var minVector = Vector256<float>.Zero;
                var maxVector = Vector256.Create(MaxTargetValue);

                for (var row = 0; row < BlockSide; row++)
                {
                    var value = Vector256.LoadUnsafe(ref planeRef, (nuint)(sourceOffset + row * width));

                    value = offsetVector + value * multiplierVector;
                    value = VectorUtils.ClampNative(value, minVector, maxVector);

                    value.StoreUnsafe(ref blocksRef, (nuint)(blockOffset + row * BlockSide));
                }

                return;
            }

            if (Vector128.IsHardwareAccelerated)
            {
                ref var planeRef = ref MemoryMarshal.GetArrayDataReference(plane);
                ref var blocksRef = ref MemoryMarshal.GetArrayDataReference(blocks);

                var offsetVector = Vector128.Create(range.Offset);
                var multiplierVector = Vector128.Create(range.Multiplier);
                var minVector = Vector128<float>.Zero;
                var maxVector = Vector128.Create(MaxTargetValue);

                for (var row = 0; row < BlockSide; row++)
                {
                    for (var vector = 0; vector < BlockSide / Vector128<float>.Count; vector++)
                    {
                        var value = Vector128.LoadUnsafe(ref planeRef, (nuint)(sourceOffset + row * width + vector * Vector128<float>.Count));

                        value = offsetVector + value * multiplierVector;
                        value = VectorUtils.ClampNative(value, minVector, maxVector);

                        value.StoreUnsafe(ref blocksRef, (nuint)(blockOffset + row * BlockSide + vector * Vector128<float>.Count));
                    }
                }

                return;
            }
#endif

            for (var row = 0; row < BlockSide; row++)
            {
                var rowSourceOffset = sourceOffset + row * width;
                var rowBlockOffset = blockOffset + row * BlockSide;

                for (var col = 0; col < BlockSide; col++)
                {
                    var scaledValue = range.Offset + plane[rowSourceOffset + col] * range.Multiplier;
                    blocks[rowBlockOffset + col] = MathUtils.Clamp(scaledValue, 0f, MaxTargetValue);
                }
            }
        }

        // Scalar only: partial blocks occur in at most one MCU column and one MCU row per image.
        private void FillEdgeBlock(float[] plane, ScaledDecodeRange range, float[] blocks, int blockOffset, int pixelX, int pixelY)
        {
            for (var row = 0; row < BlockSide; row++)
            {
                var sourceY = pixelY + row;
                if (sourceY >= height)
                {
                    sourceY = height - 1;
                }

                var rowSourceOffset = sourceY * width;
                var rowBlockOffset = blockOffset + row * BlockSide;

                for (var col = 0; col < BlockSide; col++)
                {
                    var sourceX = pixelX + col;
                    if (sourceX >= width)
                    {
                        sourceX = width - 1;
                    }

                    var scaledValue = range.Offset + plane[rowSourceOffset + sourceX] * range.Multiplier;
                    blocks[rowBlockOffset + col] = MathUtils.Clamp(scaledValue, 0f, MaxTargetValue);
                }
            }
        }
    }
}
