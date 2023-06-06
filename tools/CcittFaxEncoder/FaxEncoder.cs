// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcittFaxEncoder
{
    /// <summary>
    /// Encodes image data to be decoded by the CCITTFaxDecode filter in PDF. Performance has not been optimized as
    /// this implementation is only intended for producing test cases for PdfToSvg.NET.
    /// </summary>
    internal class FaxEncoder
    {
        private readonly FaxWriter writer = new();
        private bool[]? referenceLine;
        private int y = 0;

        public int K { get; set; }
        public bool EndOfLine { get; set; }
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

        private static int FindNext(bool[] line, int startIndex, bool findColor)
        {
            var cursor = startIndex + 1;

            if (cursor >= line.Length)
            {
                return line.Length;
            }

            do
            {
                if (line[cursor] == findColor)
                {
                    return cursor;
                }

                cursor++;
            }
            while (cursor < line.Length);

            return cursor;
        }

        public void WriteEndOfBlock()
        {
            writer.WriteCode(K < 0
                ? FaxCodes.EndOfFacsimileBlock
                : FaxCodes.ReturnToControl);
        }

        public void WriteRow(bool[] codingLine)
        {
            var isWhite = true;
            var a0 = -1;

            if (referenceLine == null)
            {
                referenceLine = new bool[codingLine.Length];
                Array.Fill(referenceLine, true);
            }
            else if (referenceLine.Length != codingLine.Length)
            {
                throw new ArgumentException("Unexpected length of row.", nameof(codingLine));
            }

            var oneDimensionalCoding = K == 0 || K > 0 && y % K == 0;

            if (EndOfLine)
            {
                writer.WriteCode(FaxCodes.EndOfLine);
            }

            if (K > 0)
            {
                writer.WriteCode(oneDimensionalCoding ? 0b11 : 0b10);
            }

            if (oneDimensionalCoding)
            {
                // One dimensional encoding
                do
                {
                    var a1 = FindNext(codingLine, a0, !isWhite);

                    if (a0 < 0)
                    {
                        a0 = 0;
                    }

                    var Ma0a1 = FaxCodes.EncodeRunLength(a1 - a0, isWhite);
                    writer.WriteCode(Ma0a1);

                    isWhite = !isWhite;
                    a0 = a1;
                }
                while (a0 < codingLine.Length);
            }
            else
            {
                // Two dimensional encoding
                do
                {
                    var a1 = FindNext(codingLine, a0, !isWhite);
                    var b1 = FindB1(referenceLine, a0, isWhite);
                    var b2 = FindNext(referenceLine, b1, isWhite);

                    if (b2 < a1)
                    {
                        // Pass mode
                        writer.WriteCode(FaxCodes.Pass);
                        a0 = b2;

                    }
                    else if (Math.Abs(a1 - b1) <= 3)
                    {
                        // Vertical coding
                        writer.WriteCode((a1 - b1) switch
                        {
                            -3 => FaxCodes.VerticalLeft3,
                            -2 => FaxCodes.VerticalLeft2,
                            -1 => FaxCodes.VerticalLeft1,
                            +1 => FaxCodes.VerticalRight1,
                            +2 => FaxCodes.VerticalRight2,
                            +3 => FaxCodes.VerticalRight3,
                            _ => FaxCodes.Vertical0,
                        });

                        a0 = a1;
                        isWhite = !isWhite;
                    }
                    else
                    {
                        // Horizontal coding
                        var a2 = FindNext(codingLine, a1, isWhite);

                        writer.WriteCode(FaxCodes.Horizontal);

                        if (a0 < 0)
                        {
                            a0 = 0;
                        }

                        var Ma0a1 = FaxCodes.EncodeRunLength(a1 - a0, isWhite);
                        writer.WriteCode(Ma0a1);

                        var Ma1a2 = FaxCodes.EncodeRunLength(a2 - a1, !isWhite);
                        writer.WriteCode(Ma1a2);

                        a0 = a2;
                    }
                }
                while (a0 < codingLine.Length);
            }

            if (EncodedByteAlign)
            {
                writer.ByteAlign(0);
            }

            Array.Copy(codingLine, referenceLine, codingLine.Length);
            y++;
        }

        public byte[] ToArray() => writer.ToArray();
    }
}
