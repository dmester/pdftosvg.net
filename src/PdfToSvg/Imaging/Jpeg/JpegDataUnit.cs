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
        public static void WriteDataUnit(this JpegImageDataWriter writer, short[] block, JpegHuffmanTable dcTable, JpegHuffmanTable acTable)
        {
            var diff = block[0];
            var diffSize = writer.GetSsss(diff);

            writer.WriteCode(dcTable.EncodeOrThrow(diffSize));

            writer.WriteValue(diffSize, diff);

            var lastNonZero = 63;
            while (lastNonZero > 0 && block[lastNonZero] == 0)
            {
                lastNonZero--;
            }

            var cursor = 1;

            while (cursor <= lastNonZero)
            {
                var zeroCount = 0;

                while (cursor <= lastNonZero && block[cursor] == 0)
                {
                    cursor++;
                    zeroCount++;

                    if (zeroCount == 16)
                    {
                        writer.WriteCode(acTable.EncodeOrThrow(0xf0));
                        zeroCount = 0;
                    }
                }

                var value = block[cursor++];
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

        public static void ReadDataUnit(this JpegImageDataReader reader, short[] data, JpegHuffmanTable dcTable, JpegHuffmanTable acTable)
        {
            const int EndOfBlock = 0;

            var cursor = 0;

            if (data.Length != 64) throw new ArgumentException(nameof(data));

            // Read DC
            var diffSize = reader.ReadHuffman(dcTable);
            if (diffSize < 0)
            {
                Array.Clear(data, 0, data.Length);
                return;
            }

            data[cursor++] = (short)reader.ReadValue(diffSize);

            // Read AC
            while (cursor < data.Length)
            {
                var zeroesAndSize = reader.ReadHuffman(acTable);

                if (zeroesAndSize < 0 ||
                    zeroesAndSize == EndOfBlock)
                {
                    Array.Clear(data, cursor, data.Length - cursor);
                    return;
                }

                var zeroes = zeroesAndSize >> 4;
                var size = zeroesAndSize & 0xf;

                for (var i = 0; i < zeroes && cursor < data.Length; i++)
                {
                    data[cursor++] = 0;
                }

                var value = reader.ReadValue(size);

                if (cursor < data.Length)
                {
                    data[cursor++] = (short)value;
                }
            }
        }

    }
}
