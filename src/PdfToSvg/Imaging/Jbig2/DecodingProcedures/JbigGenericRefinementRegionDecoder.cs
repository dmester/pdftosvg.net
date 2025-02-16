// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jbig2.Coding;
using PdfToSvg.IO;
using System;
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

        private void GetTemplates(out JbigArithmeticTemplate decodedBitmapTemplate, out JbigArithmeticTemplate referenceBitmapTemplate)
        {
            if (Template == 0)
            {
                // Figure 12
                referenceBitmapTemplate = new JbigArithmeticTemplate([
                    ATX[1] - ReferenceDx, ATY[1] - ReferenceDy,
                    0 - ReferenceDx, -1 - ReferenceDy,
                    1 - ReferenceDx, -1 - ReferenceDy,

                    -1 - ReferenceDx, 0 - ReferenceDy,
                    0 - ReferenceDx, 0 - ReferenceDy,
                    1 - ReferenceDx, 0 - ReferenceDy,

                    -1 - ReferenceDx, 1 - ReferenceDy,
                    0 - ReferenceDx, 1 - ReferenceDy,
                    1 - ReferenceDx, 1 - ReferenceDy,
                ]);
                decodedBitmapTemplate = new JbigArithmeticTemplate([
                    ATX[0], ATY[0],
                    0, -1,
                    1, -1,

                    -1, 0,
                ], referenceBitmapTemplate.Pixels);
                return;
            }

            if (Template == 1)
            {
                // Figure 13
                referenceBitmapTemplate = new JbigArithmeticTemplate([
                    0 - ReferenceDx, -1 - ReferenceDy,

                    -1 - ReferenceDx, 0 - ReferenceDy,
                    0 - ReferenceDx, 0 - ReferenceDy,
                    1 - ReferenceDx, 0 - ReferenceDy,

                    0 - ReferenceDx, 1 - ReferenceDy,
                    1 - ReferenceDx, 1 - ReferenceDy,
                ]);
                decodedBitmapTemplate = new JbigArithmeticTemplate([
                    -1, -1,
                    0, -1,
                    1, -1,

                    -1, 0,
                ], referenceBitmapTemplate.Pixels);
                return;
            }

            throw new JbigException("Unknown refinement template " + Template);
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

            GetTemplates(out var decodedBitmapTemplate, out var referenceBitmapTemplate);
            var combinedPartialUpdateMask = decodedBitmapTemplate.PartialUpdateMask & referenceBitmapTemplate.PartialUpdateMask;

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
                var fullContextUpdateRequired = true;
                var context = 0;

                for (var x = 0; x < Width; x++)
                {
                    if (typicallyPredictedLine)
                    {
                        var predictedValue = GetPredictedValue(ReferenceBitmap, x - ReferenceDx, y - ReferenceDy);
                        if (predictedValue.HasValue)
                        {
                            bitmap[x, y] = predictedValue.Value;
                            fullContextUpdateRequired = true;
                            continue;
                        }
                    }

                    if (fullContextUpdateRequired)
                    {
                        context = 0;
                        decodedBitmapTemplate.FullUpdate(bitmap, x, y, ref context);
                        referenceBitmapTemplate.FullUpdate(ReferenceBitmap, x, y, ref context);
                        fullContextUpdateRequired = false;
                    }
                    else
                    {
                        context = (context << 1) & combinedPartialUpdateMask;
                        decodedBitmapTemplate.PartialUpdate(bitmap, x, y, ref context);
                        referenceBitmapTemplate.PartialUpdate(ReferenceBitmap, x, y, ref context);
                    }

                    cx.GR.EntryIndex = context;
                    var pixel = arithmeticDecoder.DecodeBit(cx.GR);
                    bitmap[x, y] = pixel == 1;
                }
            }

            return bitmap;
        }
    }
}
