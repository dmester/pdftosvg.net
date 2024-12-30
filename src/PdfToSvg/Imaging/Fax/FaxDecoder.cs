// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Fax
{
    internal class FaxDecoder
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int K { get; set; }

        public bool EncodedByteAlign { get; set; }

        private static int FindB1(bool[] referenceLine, int a0, bool a0Color)
        {
            var cursor = a0 + 1;

            if (cursor == 0)
            {
                if (referenceLine[cursor] != a0Color)
                {
                    return cursor;
                }

                cursor++;
            }

            while (cursor < referenceLine.Length)
            {
                if (referenceLine[cursor] != a0Color &&
                    referenceLine[cursor - 1] == a0Color)
                {
                    return cursor;
                }

                cursor++;
            }

            return cursor;
        }

        private static int FindB2(bool[] referenceLine, int b1, bool a0Color)
        {
            var cursor = b1 + 1;

            if (cursor >= referenceLine.Length)
            {
                return referenceLine.Length;
            }

            do
            {
                if (referenceLine[cursor] == a0Color)
                {
                    return cursor;
                }

                cursor++;
            }
            while (cursor < referenceLine.Length);

            return cursor;
        }

        private static void Swap<T>(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        private static void Fill<T>(T[] buffer, int from, int to, T value)
        {
            if (from < 0)
            {
                from = 0;
            }

            if (to > buffer.Length)
            {
                to = buffer.Length;
            }

            while (from < to)
            {
                buffer[from] = value;
                from++;
            }
        }

        public IEnumerable<bool[]> ReadRows(byte[] data, int offset, int count)
        {
            var reader = new VariableBitReader(data, offset, count);
            return ReadRows(reader);
        }

        public IEnumerable<bool[]> ReadRows(VariableBitReader reader)
        {
            var referenceLine = new bool[Width];
            var codeLine = new bool[Width];

            // Initialize reference line to white
            Fill(referenceLine, from: 0, to: referenceLine.Length, value: true);

            while (true)
            {
                var codeLineCursor = -1;
                var isWhite = true;

                // Read optional end-of-line
                var originalCursor = reader.Cursor;
                var eol = reader.ReadBits(FaxCodes.EndOfLineCodeLength);
                if (eol != FaxCodes.EndOfLine)
                {
                    reader.Cursor = originalCursor;
                }

                var oneDimensionalCoding = K == 0 || K > 0 && reader.ReadBit() == 1;

                if (oneDimensionalCoding)
                {
                    // One dimensional encoding
                    if (codeLineCursor < 0)
                    {
                        codeLineCursor++;
                    }

                    do
                    {
                        var table = isWhite ? FaxCodes.WhiteRunLengthCodes : FaxCodes.BlackRunLengthCodes;

                        if (!reader.TryReadRunLength(table, out var runLength))
                        {
                            yield break;
                        }

                        Fill(codeLine, from: codeLineCursor, to: codeLineCursor + runLength, value: isWhite);
                        codeLineCursor += runLength;

                        isWhite = !isWhite;
                    }
                    while (codeLineCursor < codeLine.Length);
                }
                else
                {
                    // Two dimensional encoding
                    do
                    {
                        if (!reader.TryReadCode(FaxCodes.CodingModes, out var codingMode))
                        {
                            yield break;
                        }

                        var verticalOffset = codingMode switch
                        {
                            FaxCodes.VerticalLeft3 => -3,
                            FaxCodes.VerticalLeft2 => -2,
                            FaxCodes.VerticalLeft1 => -1,
                            FaxCodes.Vertical0 => 0,
                            FaxCodes.VerticalRight1 => 1,
                            FaxCodes.VerticalRight2 => 2,
                            FaxCodes.VerticalRight3 => 3,
                            _ => int.MinValue,
                        };

                        if (verticalOffset != int.MinValue)
                        {
                            // Vertical mode
                            var b1 = FindB1(referenceLine, codeLineCursor, isWhite);
                            var a1 = b1 + verticalOffset;

                            if (codeLineCursor < a1)
                            {
                                Fill(codeLine, from: codeLineCursor, to: a1, value: isWhite);
                                codeLineCursor = a1;
                            }

                            isWhite = !isWhite;
                        }
                        else if (codingMode == FaxCodes.Horizontal)
                        {
                            // Horizontal mode
                            if (codeLineCursor < 0)
                            {
                                codeLineCursor++;
                            }

                            for (var m = 0; m < 2; m++)
                            {
                                var table = isWhite ? FaxCodes.WhiteRunLengthCodes : FaxCodes.BlackRunLengthCodes;

                                if (!reader.TryReadRunLength(table, out var runLength))
                                {
                                    yield break;
                                }

                                Fill(codeLine, from: codeLineCursor, to: codeLineCursor + runLength, value: isWhite);
                                codeLineCursor += runLength;

                                isWhite = !isWhite;
                            }
                        }
                        else if (codingMode == FaxCodes.Pass)
                        {
                            // Pass mode 
                            var b1 = FindB1(referenceLine, codeLineCursor, isWhite);
                            var b2 = FindB2(referenceLine, b1, isWhite);

                            if (codeLineCursor < b2)
                            {
                                Fill(codeLine, from: codeLineCursor, to: b2, value: isWhite);
                                codeLineCursor = b2;
                            }
                        }
                        else
                        {
                            // Unexpected code
                            yield break;
                        }
                    }
                    while (codeLineCursor < codeLine.Length);
                }

                yield return codeLine;

                if (EncodedByteAlign)
                {
                    reader.AlignByte();
                }

                Swap(ref codeLine, ref referenceLine);
            }
        }
    }
}
