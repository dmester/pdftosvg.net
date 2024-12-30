// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jbig2.Coding;
using PdfToSvg.Imaging.Jbig2.Model;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.DecodingProcedures
{
    internal class JbigHalftoneRegionDecoder
    {
        /// <summary>
        /// HBW
        /// </summary>
        public int Width;

        /// <summary>
        /// HBH
        /// </summary>
        public int Height;

        /// <summary>
        /// HMMR
        /// </summary>
        public bool UseMmr;

        /// <summary>
        /// HTEMPLATE
        /// </summary>
        public int Template;

        /// <summary>
        /// HNUMPATS and HPATS
        /// </summary>
        public JbigBitmap[] Patterns = ArrayUtils.Empty<JbigBitmap>();

        /// <summary>
        /// HDEFPIXEL
        /// </summary>
        public bool DefaultPixel;

        /// <summary>
        /// HCOMBOP
        /// </summary>
        public JbigCombinationOperator CombinationOperator;

        /// <summary>
        /// HENABLESKIP
        /// </summary>
        public bool EnableSkip;

        /// <summary>
        /// HGW
        /// </summary>
        public int GridWidth;

        /// <summary>
        /// HGH
        /// </summary>
        public int GridHeight;

        /// <summary>
        /// HGX
        /// </summary>
        public int GridX;

        /// <summary>
        /// HGY
        /// </summary>
        public int GridY;

        /// <summary>
        /// HRX
        /// </summary>
        public int GridVectorX;

        /// <summary>
        /// HRY
        /// </summary>
        public int GridVectorY;

        /// <summary>
        /// HPW
        /// </summary>
        public int PatternWidth;

        /// <summary>
        /// HPH
        /// </summary>
        public int PatternHeight;

        public JbigBitmap Decode(VariableBitReader reader, JbigArithmeticContexts cx)
        {
            // 6.6.5 Decoding the halftone region

            // 1)
            var bitmap = new JbigBitmap(Width, Height);

            if (DefaultPixel)
            {
                bitmap.Fill(DefaultPixel);
            }

            // 2)
            var skip = EnableSkip ? ComputeSkipBitmap() : null;

            // 3)
            var bitsPerPattern = MathUtils.IntLog2Ceil(Patterns.Length);

            // 4)
            var grayscaleValues = DecodeGrayscale(reader, bitsPerPattern, skip, cx);

            // 5)
            DrawPatterns(bitmap, grayscaleValues);

            return bitmap;
        }

        private JbigBitmap ComputeSkipBitmap()
        {
            // 6.6.5.1 Computing HSKIP

            var bitmap = new JbigBitmap(GridWidth, GridHeight);

            for (var m = 0; m < GridHeight; m++)
            {
                for (var n = 0; n < GridWidth; n++)
                {
                    var x = (GridX + m * GridVectorY + n * GridVectorX) >> 8;
                    var y = (GridY + m * GridVectorX - n * GridVectorY) >> 8;

                    if (x + PatternWidth <= 0 ||
                        x >= Width ||
                        y + PatternHeight <= 0 ||
                        y >= Height)
                    {
                        bitmap[n, m] = true;
                    }
                }
            }

            return bitmap;
        }

        private void DrawPatterns(JbigBitmap bitmap, byte[] grayscaleValues)
        {
            // 6.6.5.2 Rendering the patterns

            var grayIndex = 0;

            for (var m = 0; m < GridHeight; m++)
            {
                for (var n = 0; n < GridWidth; n++)
                {
                    // i)
                    var x = (GridX + m * GridVectorY + n * GridVectorX) >> 8;
                    var y = (GridY + m * GridVectorX - n * GridVectorY) >> 8;

                    // ii)
                    var grayscaleValue = grayscaleValues[grayIndex++];
                    var pattern = Patterns[grayscaleValue];

                    bitmap.Draw(pattern, x, y, CombinationOperator);
                }
            }
        }

        private byte[] DecodeGrayscale(VariableBitReader reader, int bitsPerPattern, JbigBitmap? skip, JbigArithmeticContexts cx)
        {
            // Table C.4
            var decoder = new JbigGenericRegionDecoder
            {
                UseMmr = UseMmr,
                Width = GridWidth,
                Height = GridHeight,
                TypicalPrediction = false,
                Skip = skip,
                Template = Template,
                ATX = [(sbyte)(Template < 2 ? 3 : 2), -3, 2, -2],
                ATY = [-1, -1, -2, -2],
            };

            var arithmeticDecoder = new JbigArithmeticDecoder(reader);

            // C.5 Decoding the gray-scale image
            var grayscaleValues = new byte[GridWidth * GridHeight];

            JbigBitmap? previousPlane = null;

            for (var planeIndex = bitsPerPattern - 1; planeIndex >= 0; planeIndex--)
            {
                // a)
                var plane = UseMmr
                    ? decoder.DecodeMmr(reader)
                    : decoder.DecodeArithmetic(arithmeticDecoder, cx);

                // b)
                if (previousPlane != null)
                {
                    plane.Draw(previousPlane, 0, 0, JbigCombinationOperator.Xor);
                }

                // 4)
                var planeBuffer = plane.GetBuffer();
                for (var valueIndex = 0; valueIndex < grayscaleValues.Length; valueIndex++)
                {
                    if (planeBuffer[valueIndex])
                    {
                        grayscaleValues[valueIndex] |= (byte)(1 << planeIndex);
                    }
                }

                previousPlane = plane;
            }

            return grayscaleValues;
        }
    }
}
