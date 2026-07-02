// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegDataUnit
    {
        private static readonly int[] reverseOrder;
        private static readonly int[] order;

        static JpegDataUnit()
        {
            reverseOrder = JpegZigZag.GetReverseOrder();

            // Transpose the indices. Note that this is not the same thing as transposing the actual matrix!
            for (var i = 0; i < reverseOrder.Length; i++)
            {
                var index = reverseOrder[i];
                var x = index % 8;
                var y = index / 8;
                reverseOrder[i] = x * 8 + y;
            }

            order = new int[64];
            for (var i = 0; i < 64; i++)
            {
                order[reverseOrder[i]] = i;
            }
        }

        /// <summary>
        /// Writes a block containing a single value.
        /// </summary>
        public static void WriteDataUnitZeroAc(this JpegImageDataWriter writer,
            short diff, JpegHuffmanTable dcTable, JpegHuffmanTable acTable)
        {
            var diffSize = writer.GetSsss(diff);

            writer.WriteCode(dcTable.EncodeOrThrow(diffSize));
            writer.WriteValue(diffSize, diff);
            writer.WriteCode(acTable.EncodeOrThrow(0));
        }

        /// <summary>
        /// Writes a block as a data unit. Zig zag ordering and the last DCT transpose should not have been applied on
        /// the input block.
        /// </summary>
        public static void WriteDataUnitZigZag(this JpegImageDataWriter writer,
            short[] block, JpegHuffmanTable dcTable, JpegHuffmanTable acTable)
        {
            var order = reverseOrder;

            var diff = block[0];
            var diffSize = writer.GetSsss(diff);

            writer.WriteCode(dcTable.EncodeOrThrow(diffSize));
            writer.WriteValue(diffSize, diff);

            var lastNonZero = 63;
            while (lastNonZero > 0 && block[order[lastNonZero]] == 0)
            {
                lastNonZero--;
            }

            var cursor = 1;

            while (cursor <= lastNonZero)
            {
                var zeroCount = 0;

                while (cursor <= lastNonZero && block[order[cursor]] == 0)
                {
                    cursor++;
                    zeroCount++;

                    if (zeroCount == 16)
                    {
                        writer.WriteCode(acTable.EncodeOrThrow(0xf0));
                        zeroCount = 0;
                    }
                }

                var value = block[order[cursor++]];
                var valueSize = writer.GetSsss(value);

                var zeroesAndSize = (zeroCount << 4) | valueSize;
                writer.WriteCode(acTable.EncodeOrThrow(zeroesAndSize));

                writer.WriteValue(valueSize, value);
            }

            if (lastNonZero < 63)
            {
                // EOB
                writer.WriteCode(acTable.EncodeOrThrow(0));
            }
        }

        /// <summary>
        /// Reads a block from a data unit. Zig zag ordering and first DCT transpose will be preapplied on the resulting
        /// block.
        /// </summary>
        /// <param name="reader">Reader from which data is read.</param>
        /// <param name="data">Destination buffer</param>
        /// <param name="acTable">AC quantization table</param>
        /// <param name="dcTable">DC quantization table</param>
        /// <param name="zeroAc">
        /// <c>true</c> if the block only contains a single value, making further optimizations possible.
        /// </param>
        public static void ReadDataUnit(this JpegImageDataReader reader,
            short[] data, JpegHuffmanTable dcTable, JpegHuffmanTable acTable, out bool zeroAc)
        {
            var order = reverseOrder;
            const int EndOfBlock = 0;

            if (data.Length != 64)
            {
                throw new ArgumentException(nameof(data));
            }

            // Coefficients are scattered into their (transposed) natural positions through order[], so any coefficient
            // that is not read (e.g. after an end-of-block) must be zeroed up front. Clearing only a contiguous tail
            // would leave stale coefficients from the previously decoded block at the scattered positions.
            Array.Clear(data, 0, data.Length);

            // Read DC
            var diffSize = dcTable.ReadValueFrom(reader);
            if (diffSize < 0)
            {
                zeroAc = true;
                return;
            }

            data[order[0]] = (short)reader.ReadValue(diffSize);

            var cursor = 1;

            // Read AC
            while (cursor < data.Length)
            {
                var zeroesAndSize = acTable.ReadValueFrom(reader);

                if (zeroesAndSize < 0 ||
                    zeroesAndSize == EndOfBlock)
                {
                    zeroAc = cursor == 1;
                    return;
                }

                var zeroes = zeroesAndSize >> 4;
                var size = zeroesAndSize & 0xf;

                for (var i = 0; i < zeroes && cursor < data.Length; i++)
                {
                    data[order[cursor++]] = 0;
                }

                var value = reader.ReadValue(size);

                if (cursor < data.Length)
                {
                    data[order[cursor++]] = (short)value;
                }
            }

            zeroAc = false;
        }
    }
}
