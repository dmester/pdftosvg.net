// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jbig2.Coding;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.DecodingProcedures
{
    internal class JbigGenericRefinementRegionDecoder
    {
        /// <summary>
        /// GRW
        /// </summary>
        public int Width;

        /// <summary>
        /// GRH
        /// </summary>
        public int Height;

        /// <summary>
        /// GRTEMPLATE
        /// </summary>
        public int Template;

        /// <summary>
        /// GRREFERENCE
        /// </summary>
        public JbigBitmap ReferenceBitmap = JbigBitmap.Empty;

        /// <summary>
        /// GRREFERENCEDX
        /// </summary>
        public int ReferenceDx;

        /// <summary>
        /// GRREFERENCEDY
        /// </summary>
        public int ReferenceDy;

        /// <summary>
        /// TPGRON
        /// </summary>
        public bool TypicalPrediction;

        /// <summary>
        /// GRATXx
        /// </summary>
        public sbyte[] ATX = ArrayUtils.Empty<sbyte>();

        /// <summary>
        /// GRATXy
        /// </summary>
        public sbyte[] ATY = ArrayUtils.Empty<sbyte>();

        private int[][] GetTemplateCoordinates()
        {
            if (Template == 0)
            {
                // Figure 12
                return [
                    // Decoded bitmap
                    [
                        ATX[0], ATY[0],
                        0, -1,
                        1, -1,

                        -1, 0,
                    ],

                    // Reference bitmap
                    [
                        ATX[1] - ReferenceDx, ATY[1] - ReferenceDy,
                        0 - ReferenceDx, -1 - ReferenceDy,
                        1 - ReferenceDx, -1 - ReferenceDy,

                        -1 - ReferenceDx, 0 - ReferenceDy,
                        0 - ReferenceDx, 0 - ReferenceDy,
                        1 - ReferenceDx, 0 - ReferenceDy,

                        -1 - ReferenceDx, 1 - ReferenceDy,
                        0 - ReferenceDx, 1 - ReferenceDy,
                        1 - ReferenceDx, 1 - ReferenceDy,
                    ],
                ];
            }

            if (Template == 1)
            {
                // Figure 13
                return [
                    // Decoded bitmap
                    [
                        -1, -1,
                        0, -1,
                        1, -1,

                        -1, 0,
                    ],

                    // Reference bitmap
                    [
                        0 - ReferenceDx, -1 - ReferenceDy,

                        -1 - ReferenceDx, 0 - ReferenceDy,
                        0 - ReferenceDx, 0 - ReferenceDy,
                        1 - ReferenceDx, 0 - ReferenceDy,

                        0 - ReferenceDx, 1 - ReferenceDy,
                        1 - ReferenceDx, 1 - ReferenceDy,
                    ],
                ];
            }

            throw new JbigException("Unknown refinement template " + Template);
        }

        private static int GetContextFromTemplate(int[][] templateCoordinatesForBitmaps, JbigBitmap[] bitmaps, int originX, int originY)
        {
            var result = 0;

            for (var bitmapIndex = 0; bitmapIndex < bitmaps.Length; bitmapIndex++)
            {
                var bitmap = bitmaps[bitmapIndex];
                var templateCoordinates = templateCoordinatesForBitmaps[bitmapIndex];

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
            }

            return result;
        }

        public static bool? GetPredictedValue(JbigBitmap bitmap, int x, int y)
        {
            bool predictedValue;

            if (x >= 0 &&
                y >= 0 &&
                x < bitmap.Width &&
                y < bitmap.Height)
            {
                predictedValue = bitmap[x, y];
            }
            else
            {
                // Pixels outside the bitmap should be considered 0
                predictedValue = false;
            }

            var minx = x - 1;
            var maxx = x + 1;
            var miny = y - 1;
            var maxy = y + 1;

            if (minx < 0)
            {
                // All values outside the bitmap are considered to be 0, so if the predicted value is 1, then the
                // surrounding values are not the same.
                if (predictedValue) return null;

                minx = 0;
            }

            if (miny < 0)
            {
                if (predictedValue) return null;
                miny = 0;
            }

            if (maxx >= bitmap.Width)
            {
                if (predictedValue) return null;
                maxx = bitmap.Width - 1;
            }

            if (maxy >= bitmap.Height)
            {
                if (predictedValue) return null;
                maxy = bitmap.Height - 1;
            }

            for (var cx = minx; cx <= maxx; cx++)
            {
                for (var cy = miny; cy <= maxy; cy++)
                {
                    if (bitmap[cx, cy] != predictedValue)
                    {
                        return null;
                    }
                }
            }

            return predictedValue;
        }

        public JbigBitmap Decode(VariableBitReader reader, JbigArithmeticContexts cx)
        {
            var arithmeticDecoder = new JbigArithmeticDecoder(reader);
            return Decode(arithmeticDecoder, cx);
        }

        public JbigBitmap Decode(JbigArithmeticDecoder arithmeticDecoder, JbigArithmeticContexts cx)
        {
            // 6.3.5.6 Decoding the refinement bitmap

            var templateCoordinates = GetTemplateCoordinates();

            // 1)
            var typicallyPredictedLine = false;

            // 2)
            var bitmap = new JbigBitmap(Width, Height);

            // 3)
            for (var y = 0; y < Height; y++)
            {
                // b)
                if (TypicalPrediction)
                {
                    cx.GR.EntryIndex = Template switch
                    {
                        // Figure 14
                        0 => 0b000_0__000_010_000,

                        // Figure 15
                        1 => 0b000_0__0_010_00,

                        _ => throw new JbigException("Unsupported refinement template " + Template)
                    };

                    var sltp = arithmeticDecoder.DecodeBit(cx.GR) == 1;
                    typicallyPredictedLine = typicallyPredictedLine ^ sltp;
                }

                // c) d)
                if (typicallyPredictedLine)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        var predictedValue = GetPredictedValue(ReferenceBitmap, x - ReferenceDx, y - ReferenceDy);
                        if (predictedValue.HasValue)
                        {
                            bitmap[x, y] = predictedValue.Value;
                        }
                        else
                        {
                            cx.GR.EntryIndex = GetContextFromTemplate(templateCoordinates, [bitmap, ReferenceBitmap], x, y);
                            var pixel = arithmeticDecoder.DecodeBit(cx.GR);
                            bitmap[x, y] = pixel == 1;
                        }
                    }
                }
                else
                {
                    for (var x = 0; x < Width; x++)
                    {
                        cx.GR.EntryIndex = GetContextFromTemplate(templateCoordinates, [bitmap, ReferenceBitmap], x, y);
                        var pixel = arithmeticDecoder.DecodeBit(cx.GR);
                        bitmap[x, y] = pixel == 1;
                    }
                }
            }

            return bitmap;
        }
    }
}
