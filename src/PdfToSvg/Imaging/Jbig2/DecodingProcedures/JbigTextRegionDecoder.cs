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
    internal class JbigTextRegionDecoder
    {
        /// <summary>
        /// SBHUFF
        /// </summary>
        public bool UseHuffman;

        /// <summary>
        /// SBREFINE
        /// </summary>
        public bool UseRefinementCoding;

        /// <summary>
        /// SBW
        /// </summary>
        public int Width;

        /// <summary>
        /// SBH
        /// </summary>
        public int Height;

        /// <summary>
        /// SBNUMINSTANCES
        /// </summary>
        public int SymbolInstanceCount;

        /// <summary>
        /// SBSTRIPS
        /// </summary>
        public int Log2StripSize;

        /// <summary>
        /// SBSYMCODES
        /// </summary>
        public JbigHuffmanTable SymbolCodes = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SBSYMS and SBNUMSYMS
        /// </summary>
        public JbigBitmap[] Symbols = ArrayUtils.Empty<JbigBitmap>();

        /// <summary>
        /// SBDEFPIXEL
        /// </summary>
        public bool DefaultPixel;

        /// <summary>
        /// SBCOMBOP
        /// </summary>
        public JbigCombinationOperator CombinationOperator;

        /// <summary>
        /// TRANSPOSED
        /// </summary>
        public bool Transposed;

        /// <summary>
        /// REFCORNER
        /// </summary>
        public JbigTextRegionCorner RefCorner;

        /// <summary>
        /// SBDSOFFSET
        /// </summary>
        public int SOffset;

        /// <summary>
        /// SBHUFFFS
        /// </summary>
        public JbigHuffmanTable FirstSymbolSCoordinateTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SBHUFFFDS
        /// </summary>
        public JbigHuffmanTable SubsequentSymbolSCoordinateTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SBHUFFDT
        /// </summary>
        public JbigHuffmanTable SymbolTCoordinateTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SBHUFFRDW
        /// </summary>
        public JbigHuffmanTable DifferentialWidthTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SBHUFFRDH
        /// </summary>
        public JbigHuffmanTable DifferentialHeightTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SBHUFFRDX
        /// </summary>
        public JbigHuffmanTable DifferentialXTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SBHUFFRDY
        /// </summary>
        public JbigHuffmanTable DifferentialYTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SBHUFFRSIZE
        /// </summary>
        public JbigHuffmanTable RefinementSizeTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SBRTEMPLATE
        /// </summary>
        public int RefinementTemplate;

        /// <summary>
        /// SBRATXx
        /// </summary>
        public sbyte[] RefinementATX = ArrayUtils.Empty<sbyte>();

        /// <summary>
        /// SBRATYx
        /// </summary>
        public sbyte[] RefinementATY = ArrayUtils.Empty<sbyte>();

        public JbigBitmap Decode(VariableBitReader reader, JbigArithmeticContexts cx)
        {
            var arithmeticDecoder = new JbigArithmeticDecoder(reader);
            return Decode(reader, arithmeticDecoder, cx);
        }

        public JbigBitmap Decode(VariableBitReader reader, JbigArithmeticDecoder arithmeticDecoder, JbigArithmeticContexts cx)
        {
            var symbolCodeLength = MathUtils.IntLog2Ceil(Symbols.Length);
            cx.IAID.EnsureSize(1 << symbolCodeLength);

            // 6.4.5 Decoding the text region
            // 6.4.5  1)
            var regionBitmap = new JbigBitmap(Width, Height);

            if (DefaultPixel)
            {
                regionBitmap.Fill(DefaultPixel);
            }

            // 6.4.5  2)
            var deltaStripT = UseHuffman
                ? SymbolTCoordinateTable.DecodeValue(reader)
                : arithmeticDecoder.DecodeInteger(cx.IADT);

            var stripT = -deltaStripT * (1 << Log2StripSize);

            var firstS = 0;
            var curS = 0;

            // 6.4.5  3)
            // Colored images not supported

            // 6.4.5  4)
            for (var instance = 0; instance < SymbolInstanceCount;)
            {
                // b)
                deltaStripT = UseHuffman
                    ? SymbolTCoordinateTable.DecodeValue(reader)
                    : arithmeticDecoder.DecodeInteger(cx.IADT);

                stripT += deltaStripT * (1 << Log2StripSize);

                // c)
                // We need to consume the OOB symbol, so we cannot have `instance < SymbolInstanceCount` as condition.
                // But we also want a condition to prevent infinity loops, especially since the arithmetic decoder does
                // not detect the end of stream reliably.
                for (var symbolIndex = 0; instance < SymbolInstanceCount + 1; symbolIndex++)
                {
                    if (symbolIndex == 0)
                    {
                        // i)
                        var firstSOffset = UseHuffman
                            ? FirstSymbolSCoordinateTable.DecodeValueOrOob(reader)
                            : arithmeticDecoder.DecodeIntegerOrOob(cx.IAFS);

                        if (firstSOffset.IsOob)
                        {
                            break;
                        }

                        firstS += firstSOffset.Value;
                        curS = firstS;
                    }
                    else
                    {
                        // ii)
                        var subsequentSOffset = UseHuffman
                            ? SubsequentSymbolSCoordinateTable.DecodeValueOrOob(reader)
                            : arithmeticDecoder.DecodeIntegerOrOob(cx.IADS);

                        if (subsequentSOffset.IsOob)
                        {
                            break;
                        }

                        curS += subsequentSOffset.Value + SOffset;
                    }

                    if (instance >= SymbolInstanceCount)
                    {
                        break;
                    }

                    // iii)
                    var curt =
                        Log2StripSize == 0 ? 0 :
                        UseHuffman ? reader.ReadBitsOrThrow(Log2StripSize) :
                        arithmeticDecoder.DecodeInteger(cx.IAIT);

                    var ti = stripT + curt;

                    // iv)
                    var idi = UseHuffman
                        ? SymbolCodes.DecodeValue(reader)
                        : arithmeticDecoder.DecodeSymbol(cx.IAID, symbolCodeLength);
                    if (idi < 0 || idi >= Symbols.Length)
                    {
                        throw new JbigException(
                            "Invalid symbol reference " + idi + ". Only " + Symbols.Length + " symbols available");
                    }

                    // v)
                    JbigBitmap symbolBitmap;

                    var ri =
                        !UseRefinementCoding ? 0 :
                        UseHuffman ? reader.ReadBit() :
                        arithmeticDecoder.DecodeInteger(cx.IARI);

                    if (ri == 1)
                    {
                        var rdw = UseHuffman
                            ? DifferentialWidthTable.DecodeValue(reader)
                            : arithmeticDecoder.DecodeInteger(cx.IARDW);

                        var rdh = UseHuffman
                            ? DifferentialHeightTable.DecodeValue(reader)
                            : arithmeticDecoder.DecodeInteger(cx.IARDH);

                        var rdx = UseHuffman
                            ? DifferentialXTable.DecodeValue(reader)
                            : arithmeticDecoder.DecodeInteger(cx.IARDX);

                        var rdy = UseHuffman
                            ? DifferentialYTable.DecodeValue(reader)
                            : arithmeticDecoder.DecodeInteger(cx.IARDY);

                        if (UseHuffman)
                        {
                            var symbolSize = RefinementSizeTable.DecodeValue(reader);
                            reader.AlignByte();
                            arithmeticDecoder = new JbigArithmeticDecoder(reader, symbolSize);
                            reader.SkipBytes(symbolSize);
                        }

                        var ido = Symbols[idi];

                        // Table 12
                        var refinementDecoder = new JbigGenericRefinementRegionDecoder
                        {
                            Width = ido.Width + rdw,
                            Height = ido.Height + rdh,
                            Template = RefinementTemplate,
                            ReferenceBitmap = ido,
                            ReferenceDx = MathUtils.FloorDiv(rdw, 2) + rdx,
                            ReferenceDy = MathUtils.FloorDiv(rdh, 2) + rdy,
                            TypicalPrediction = false,
                            ATX = RefinementATX,
                            ATY = RefinementATY,
                        };

                        symbolBitmap = refinementDecoder.Decode(arithmeticDecoder, cx);
                    }
                    else
                    {
                        symbolBitmap = Symbols[idi];
                    }

                    // vi)
                    if (Transposed)
                    {
                        if (RefCorner == JbigTextRegionCorner.BottomLeft ||
                            RefCorner == JbigTextRegionCorner.BottomRight)
                        {
                            curS += symbolBitmap.Height - 1;
                        }
                    }
                    else
                    {
                        if (RefCorner == JbigTextRegionCorner.TopRight ||
                            RefCorner == JbigTextRegionCorner.BottomRight)
                        {
                            curS += symbolBitmap.Width - 1;
                        }
                    }

                    // vii)
                    var si = curS;

                    // viii)
                    var destX = Transposed ? ti : si;
                    var destY = Transposed ? si : ti;

                    switch (RefCorner)
                    {
                        case JbigTextRegionCorner.TopRight:
                            destX -= symbolBitmap.Width - 1;
                            break;

                        case JbigTextRegionCorner.BottomLeft:
                            destY -= symbolBitmap.Height - 1;
                            break;

                        case JbigTextRegionCorner.BottomRight:
                            destX -= symbolBitmap.Width - 1;
                            destY -= symbolBitmap.Height - 1;
                            break;
                    }

                    // ix)
                    // Color not supported

                    // x)
                    regionBitmap.Draw(symbolBitmap, destX, destY, CombinationOperator);

                    // xi)
                    if (Transposed)
                    {
                        if (RefCorner == JbigTextRegionCorner.TopLeft ||
                            RefCorner == JbigTextRegionCorner.TopRight)
                        {
                            curS += symbolBitmap.Height - 1;
                        }
                    }
                    else
                    {
                        if (RefCorner == JbigTextRegionCorner.TopLeft ||
                            RefCorner == JbigTextRegionCorner.BottomLeft)
                        {
                            curS += symbolBitmap.Width - 1;
                        }
                    }

                    instance++;
                }
            }

            return regionBitmap;
        }
    }
}
