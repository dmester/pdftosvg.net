// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal class JpegBitmap
    {
        private const int BlockSize = 8;

        public static JpegBitmap Empty { get; } = new JpegBitmap(0, 0, 0);

        public readonly short[] Data;
        public readonly int Width;
        public readonly int Height;
        public readonly int Components;
        public int Length => Data.Length;

        public JpegBitmap(int width, int height, int components)
        {
            Width = width;
            Height = height;
            Components = components;
            Data = new short[width * height * components];
        }

        public void DrawNearestNeighbourClippedOnto(
            JpegBitmap dest,
            int destX, int destY, int destWidth, int destHeight,
            int destComponent)
        {
            if (Components != 1) throw new NotSupportedException("Can only draw 1 component bitmaps onto other bitmaps.");

            var clippedDestHeight = Math.Min(destHeight, dest.Height - destY);
            var clippedDestWidth = Math.Min(destWidth, dest.Width - destX);

            for (var dy = 0; dy < clippedDestHeight; dy++)
            {
                var destCursor = ((destY + dy) * dest.Width + destX) * dest.Components + destComponent;
                var sy = Height * dy / destHeight;

                for (var dx = 0; dx < clippedDestWidth; dx++)
                {
                    var sx = Width * dx / destWidth;
                    var si = sy * Width + sx;

                    dest.Data[destCursor] = Data[si];

                    destCursor += dest.Components;
                }
            }
        }

        public void GetBlock(short[] block,
            int x, int y, int componentIndex,
            int subSamplingX, int subSamplingY)
        {
            if (block.Length != BlockSize * BlockSize)
            {
                throw new ArgumentException(
                    "Expected input block to be " + BlockSize + "x" + BlockSize + ".",
                    nameof(block));
            }

            var blockCursor = 0;

            var maxX = MathUtils.Clamp(1 + (Width - x - 1) / subSamplingX, 0, BlockSize);
            var maxY = MathUtils.Clamp(1 + (Height - y - 1) / subSamplingY, 0, BlockSize);

            for (var iy = 0; iy < maxY; iy++)
            {
                var inputCursor =
                    ((y + iy * subSamplingY) * Width + x) * Components +
                    componentIndex;

                var lastValue = (short)0;

                for (var ix = 0; ix < maxX; ix++)
                {
                    lastValue = Data[inputCursor];

                    block[blockCursor++] = lastValue;

                    inputCursor += Components * subSamplingX;
                }

                for (var ix = maxX; ix < BlockSize; ix++)
                {
                    block[blockCursor++] = lastValue;
                }
            }

            var lastRowCursor = blockCursor - BlockSize;

            if (lastRowCursor < 0)
            {
                while (blockCursor < block.Length)
                {
                    block[blockCursor++] = 0;
                }
            }
            else
            {
                while (blockCursor < block.Length)
                {
                    block[blockCursor++] = block[lastRowCursor++];
                }
            }
        }
    }
}
