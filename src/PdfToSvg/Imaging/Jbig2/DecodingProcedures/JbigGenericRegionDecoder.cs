// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Fax;
using PdfToSvg.Imaging.Jbig2.Coding;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.DecodingProcedures
{
    internal class JbigGenericRegionDecoder
    {
        /// <summary>
        /// GBW
        /// </summary>
        public bool UseMmr;

        /// <summary>
        /// GBW
        /// </summary>
        public int Width;

        /// <summary>
        /// GBH
        /// </summary>
        public int Height;

        /// <summary>
        /// GBTEMPLATE
        /// </summary>
        public int Template;

        /// <summary>
        /// TPGDON
        /// </summary>
        public bool TypicalPrediction;

        /// <summary>
        /// EXTTEMPLATE
        /// </summary>
        public bool ExtendedTemplate;

        /// <summary>
        /// SKIP
        /// </summary>
        public JbigBitmap? Skip;

        /// <summary>
        /// ATXx
        /// </summary>
        public sbyte[] ATX = ArrayUtils.Empty<sbyte>();

        /// <summary>
        /// ATYx
        /// </summary>
        public sbyte[] ATY = ArrayUtils.Empty<sbyte>();

        /// <summary>
        /// COLEXTFLAG
        /// </summary>
        public bool ColorExtension;

        public JbigBitmap Decode(VariableBitReader reader)
        {
            if (UseMmr)
            {
                return DecodeMmr(reader);
            }
            else
            {
                var arithmeticDecoder = new JbigArithmeticDecoder(reader);
                var cx = new JbigArithmeticContexts();
                return DecodeArithmetic(arithmeticDecoder, cx);
            }
        }

        public JbigBitmap DecodeArithmetic(JbigArithmeticDecoder arithmeticDecoder, JbigArithmeticContexts cx)
        {
            // 6.2.5.7 Decoding the bitmap

            var templateCoordinates = GetTemplateCoordinates();

            // 1)
            var typicallyPredictedLine = false;

            // 2)
            var bitmap = new JbigBitmap(Width, Height);

            for (var y = 0; y < bitmap.Height; y++)
            {
                // b)
                if (TypicalPrediction)
                {
                    cx.GB.EntryIndex = Template switch
                    {
                        // Figure 8
                        0 => 0b10011_0110010_0101,

                        // Figure 9
                        1 => 0b0011_110010_101,

                        // Figure 10
                        2 => 0b001_11001_01,

                        // Figure 11
                        3 => 0b011001_0101,

                        _ => throw new JbigException("Unknown template " + Template)
                    };

                    var sltp = arithmeticDecoder.DecodeBit(cx.GB) == 1;
                    typicallyPredictedLine = typicallyPredictedLine ^ sltp;
                }

                // c)
                if (typicallyPredictedLine)
                {
                    // Copy previous row
                    var buffer = bitmap.GetBuffer();

                    var currentRowStartIndex = y * bitmap.Width;
                    var previousRowStartIndex = currentRowStartIndex - bitmap.Width;

                    Array.Copy(
                        sourceArray: buffer,
                        sourceIndex: previousRowStartIndex,
                        destinationArray: buffer,
                        destinationIndex: currentRowStartIndex,
                        length: bitmap.Width);
                }

                // d)
                else
                {
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        if (Skip != null && Skip[x, y])
                        {
                            bitmap[x, y] = false;
                        }
                        else
                        {
                            cx.GB.EntryIndex = GetContextFromTemplate(templateCoordinates, bitmap, x, y);
                            var pixel = arithmeticDecoder.DecodeBit(cx.GB);
                            bitmap[x, y] = pixel == 1;
                        }
                    }
                }
            }

            return bitmap;
        }

        public JbigBitmap DecodeMmr(VariableBitReader reader)
        {
            // 6.2.6 Decoding using MMR coding

            var faxDecoder = new FaxDecoder
            {
                Width = Width,
                Height = Height,
                K = -1,
            };

            var bitmap = new JbigBitmap(Width, Height);
            var y = 0;

            reader.AlignByte();

            foreach (var row in faxDecoder.ReadRows(reader))
            {
                for (var x = 0; x < row.Length; x++)
                {
                    bitmap[x, y] = !row[x];
                }

                if (++y >= faxDecoder.Height)
                {
                    break;
                }
            }

            ConsumeMmrEndOfLine(reader);
            reader.AlignByte();

            return bitmap;
        }

        private void ConsumeMmrEndOfLine(VariableBitReader reader)
        {
            // EOFB is optional in most cases, but not when reading halftone planes.
            // EOFB consists of two consecutive EOL.
            // Since we break the decoder when we have read the desired number of rows, we might have left the reader
            // before either an EOL or an EOFB (2x EOL)

            for (var i = 0; i < 2; i++)
            {
                var originalCursor = reader.Cursor;

                var potentialEol = reader.ReadBits(FaxCodes.EndOfLineCodeLength);
                if (potentialEol != FaxCodes.EndOfLine)
                {
                    // Restore cursor
                    reader.Cursor = originalCursor;
                    break;
                }
            }
        }

        private int[] GetTemplateCoordinates()
        {
            if (Template == 0)
            {
                if (ExtendedTemplate)
                {
                    // Figure 3(b)
                    return [
                        ATX[10], ATY[10],
                        ATX[3], ATY[3],
                        ATX[1], ATY[1],
                        ATX[4], ATY[4],
                        ATX[8], ATY[8],

                        ATX[11], ATY[11],
                        ATX[2], ATY[2],
                        -1, -1,
                        0, -1,
                        1, -1,
                        ATX[5], ATY[5],
                        ATX[9], ATY[9],

                        ATX[7], ATY[7],
                        ATX[6], ATY[6],
                        ATX[0], ATY[0],
                        -1, 0,
                    ];
                }
                else
                {
                    // Figure 3(a)
                    return [
                        ATX[3], ATY[3],
                        -1, -2,
                        0, -2,
                        1, -2,
                        ATX[2], ATY[2],

                        ATX[1], ATY[1],
                        -2, -1,
                        -1, -1,
                        0, -1,
                        1, -1,
                        2, -1,
                        ATX[0], ATY[0],

                        -4, 0,
                        -3, 0,
                        -2, 0,
                        -1, 0,
                    ];
                }
            }

            if (Template == 1)
            {
                // Figure 4
                return [
                    -1, -2,
                    0, -2,
                    1, -2,
                    2, -2,

                    -2, -1,
                    -1, -1,
                    0, -1,
                    1, -1,
                    2, -1,
                    ATX[0], ATY[0],

                    -3, 0,
                    -2, 0,
                    -1, 0,
                ];
            }

            if (Template == 2)
            {
                // Figure 5
                return [
                    -1, -2,
                    0, -2,
                    1, -2,

                    -2, -1,
                    -1, -1,
                    0, -1,
                    1, -1,
                    ATX[0], ATY[0],

                    -2, 0,
                    -1, 0,
                ];
            }

            if (Template == 3)
            {
                // Figure 6
                return [
                    -3, -1,
                    -2, -1,
                    -1, -1,
                    0, -1,
                    1, -1,
                    ATX[0], ATY[0],

                    -4, 0,
                    -3, 0,
                    -2, 0,
                    -1, 0,
                ];
            }

            throw new JbigException("Unknown template " + Template);
        }

        private static int GetContextFromTemplate(int[] templateCoordinates, JbigBitmap bitmap, int originX, int originY)
        {
            var result = 0;

            for (var i = 0; i < templateCoordinates.Length; i += 2)
            {
                result <<= 1;

                var x = originX + templateCoordinates[i + 0];
                if (x < 0 || x >= bitmap.Width)
                {
                    continue;
                }

                var y = originY + templateCoordinates[i + 1];
                if (y < 0 || y >= bitmap.Height)
                {
                    continue;
                }

                if (bitmap[x, y])
                {
                    result |= 1;
                }
            }

            return result;
        }

    }
}
