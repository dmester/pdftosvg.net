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
    internal class JbigSymbolDictionaryDecoder
    {
        /// <summary>
        /// SDHUFF
        /// </summary>
        public bool UseHuffman;

        /// <summary>
        /// SDREFAGG
        /// </summary>
        public bool RefinementAndAggregated;

        /// <summary>
        /// SDNUMINSYMS and SDINSYMS
        /// </summary>
        public JbigBitmap[] InputSymbols = ArrayUtils.Empty<JbigBitmap>();

        /// <summary>
        /// SDNUMNEWSYMS
        /// </summary>
        public int NewSymbolCount;

        /// <summary>
        /// SDNUMEXSYMS
        /// </summary>
        public int ExportedSymbolCount;

        /// <summary>
        /// SDHUFFDH
        /// </summary>
        public JbigHuffmanTable DifferenceHeightTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SDHUFFDW
        /// </summary>
        public JbigHuffmanTable DifferenceWidthTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SDHUFFBMSIZE
        /// </summary>
        public JbigHuffmanTable CollectiveBitmapSizeTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SDHUFFAGGINST
        /// </summary>
        public JbigHuffmanTable AggregatedSymbolCountTable = JbigStandardHuffmanTable.TableB1;

        /// <summary>
        /// SDTEMPLATE
        /// </summary>
        public int Template;

        /// <summary>
        /// SDATXx
        /// </summary>
        public sbyte[] ATX = ArrayUtils.Empty<sbyte>();

        /// <summary>
        /// SDATYx
        /// </summary>
        public sbyte[] ATY = ArrayUtils.Empty<sbyte>();

        /// <summary>
        /// SDRTEMPLATE
        /// </summary>
        public int RefinementTemplate;

        /// <summary>
        /// SDRATXx
        /// </summary>
        public sbyte[] RefinementATX = ArrayUtils.Empty<sbyte>();

        /// <summary>
        /// SDRATYx
        /// </summary>
        public sbyte[] RefinementATY = ArrayUtils.Empty<sbyte>();

        public JbigBitmap[] Decode(VariableBitReader reader, JbigArithmeticContexts cx)
        {
            // 6.5.5 Decoding the symbol dictionary

            // 1) (also satisfies 6.5.8.2.4)
            var inputAndNewSymbols = new JbigBitmap[InputSymbols.Length + NewSymbolCount];
            Array.Copy(InputSymbols, inputAndNewSymbols, InputSymbols.Length);

            for (var i = 0; i < inputAndNewSymbols.Length; i++)
            {
                inputAndNewSymbols[i] ??= JbigBitmap.Empty;
            }

            // 6.5.8.2.3 Setting SBSYMCODES and SBSYMCODELEN
            int symbolCodeLength;
            var symbolCodes = JbigHuffmanTable.Empty;

            if (UseHuffman)
            {
                symbolCodeLength = Math.Max(MathUtils.IntLog2Ceil(inputAndNewSymbols.Length), 1);

                if (RefinementAndAggregated)
                {
                    symbolCodes = CreateSymbolCodes(inputAndNewSymbols.Length, symbolCodeLength);
                }
            }
            else
            {
                symbolCodeLength = MathUtils.IntLog2Ceil(inputAndNewSymbols.Length);
                cx.IAID.EnsureSize(1 << symbolCodeLength);
            }

            // 2)
            var newSymbolWidths = new int[NewSymbolCount];

            // 3)
            var heightClassHeight = 0;
            var symbolsDecoded = 0;

            var arithmeticDecoder = new JbigArithmeticDecoder(reader);

            // 4) a)
            while (symbolsDecoded < NewSymbolCount)
            {
                // 4) b)
                var heightClassDeltaHeight = UseHuffman
                    ? DifferenceHeightTable.DecodeValue(reader)
                    : arithmeticDecoder.DecodeInteger(cx.IADH);

                heightClassHeight += heightClassDeltaHeight;
                var symbolWidth = 0;
                var totalWidth = 0;
                var firstHeightClassSymbolIndex = symbolsDecoded;

                // 4) c)

                // We need to consume the OOB symbol, so we cannot have `NSYMSDECODED < NewSymbolCount` as condition.
                // But we also want a condition to prevent infinity loops, especially since the arithmetic decoder does
                // not detect the end of stream reliably.
                while (symbolsDecoded < NewSymbolCount + 1)
                {
                    // i)
                    var deltaWidth = UseHuffman
                        ? DifferenceWidthTable.DecodeValueOrOob(reader)
                        : arithmeticDecoder.DecodeIntegerOrOob(cx.IADW);

                    if (deltaWidth.IsOob || symbolsDecoded >= NewSymbolCount)
                    {
                        break;
                    }

                    symbolWidth = symbolWidth + deltaWidth.Value;
                    totalWidth = totalWidth + symbolWidth;

                    // ii)
                    if (RefinementAndAggregated)
                    {
                        // 6.5.8.2 Refinement/aggregate-coded symbol bitmap
                        inputAndNewSymbols[InputSymbols.Length + symbolsDecoded] = DecodeRefinementAggregateCodedSymbol(
                            reader,
                            arithmeticDecoder, cx,
                            symbolWidth, heightClassHeight,
                            inputAndNewSymbols,
                            symbolCodes, symbolCodeLength);
                    }
                    else if (!UseHuffman)
                    {
                        // 6.5.8.1 Direct-coded symbol bitmap
                        var decoder = new JbigGenericRegionDecoder
                        {
                            Width = symbolWidth,
                            Height = heightClassHeight,
                            Template = Template,
                            TypicalPrediction = false,
                            ATX = ATX,
                            ATY = ATY,
                        };

                        inputAndNewSymbols[InputSymbols.Length + symbolsDecoded] = decoder.DecodeArithmetic(arithmeticDecoder, cx);
                    }

                    // iii)
                    else
                    {
                        newSymbolWidths[symbolsDecoded] = symbolWidth;
                    }

                    // iv)
                    symbolsDecoded = symbolsDecoded + 1;
                }

                // d)
                if (UseHuffman && !RefinementAndAggregated)
                {
                    var collectiveBitmap = DecodeCollectiveBitmap(reader, totalWidth, heightClassHeight);

                    // Break up
                    if (newSymbolWidths.Length > 1)
                    {
                        var cursorX = 0;

                        for (var i = firstHeightClassSymbolIndex; i < symbolsDecoded; i++)
                        {
                            inputAndNewSymbols[InputSymbols.Length + i] = collectiveBitmap.Crop(cursorX, 0, newSymbolWidths[i], heightClassHeight);
                            cursorX += newSymbolWidths[i];
                        }
                    }
                    else
                    {
                        inputAndNewSymbols[InputSymbols.Length + firstHeightClassSymbolIndex] = collectiveBitmap;
                    }
                }
            }

            return GetExportedSymbols(reader, arithmeticDecoder, cx, inputAndNewSymbols);
        }

        private static JbigHuffmanTable CreateSymbolCodes(int symbolCount, int symbolCodeLength)
        {
            var symbolCodeLengths = new List<JbigHuffmanRange>();

            for (var symbolIndex = 0; symbolIndex < symbolCount; symbolIndex++)
            {
                symbolCodeLengths.Add(JbigHuffmanRange.Single(symbolIndex, symbolCodeLength));
            }

            return new JbigHuffmanTable(symbolCodeLengths);
        }

        private JbigBitmap DecodeCollectiveBitmap(VariableBitReader reader, int totalWidth, int symbolHeight)
        {
            // 6.5.9 Height class collective bitmap

            // 1)
            var collectiveBitmapSize = CollectiveBitmapSizeTable.DecodeValue(reader);

            // 2)
            reader.AlignByte();

            // 3)
            JbigBitmap collectiveBitmap;

            if (collectiveBitmapSize == 0)
            {
                collectiveBitmap = ReadUncompressed(reader, totalWidth, symbolHeight);
            }

            // 4)
            else
            {
                reader.AlignByte();
                var startPosition = reader.Cursor.Cursor;
                var subReader = reader.CreateSubReader(collectiveBitmapSize);

                // Table 19
                var decoder = new JbigGenericRegionDecoder
                {
                    Width = totalWidth,
                    Height = symbolHeight,
                };

                collectiveBitmap = decoder.DecodeMmr(subReader);
            }

            // 5)
            reader.AlignByte();

            return collectiveBitmap;
        }

        private JbigBitmap DecodeRefinementAggregateCodedSymbol(
            VariableBitReader reader,
            JbigArithmeticDecoder arithmeticDecoder, JbigArithmeticContexts cx,
            int symbolWidth,
            int symbolHeight,
            JbigBitmap[] inputAndNewSymbols,
            JbigHuffmanTable symbolCodes, int symbolCodeLength
            )
        {
            // 6.5.8.2 Refinement/aggregate-coded symbol bitmap

            // 1)
            var symbolInstanceCount = UseHuffman
                ? AggregatedSymbolCountTable.DecodeValue(reader)
                : arithmeticDecoder.DecodeInteger(cx.IAAI);

            // 2)
            if (symbolInstanceCount > 1)
            {
                // Table 17
                var textDecoder = new JbigTextRegionDecoder
                {
                    UseHuffman = UseHuffman,
                    UseRefinementCoding = true,
                    Width = symbolWidth,
                    Height = symbolHeight,
                    SymbolInstanceCount = symbolInstanceCount,
                    Log2StripSize = 0,

                    Symbols = inputAndNewSymbols,
                    SymbolCodes = symbolCodes,

                    DefaultPixel = false,
                    CombinationOperator = JbigCombinationOperator.Or,
                    Transposed = false,
                    RefCorner = JbigTextRegionCorner.TopLeft,
                    SOffset = 0,

                    FirstSymbolSCoordinateTable = JbigStandardHuffmanTable.TableB6,
                    SubsequentSymbolSCoordinateTable = JbigStandardHuffmanTable.TableB8,
                    SymbolTCoordinateTable = JbigStandardHuffmanTable.TableB11,
                    DifferentialWidthTable = JbigStandardHuffmanTable.TableB15,
                    DifferentialHeightTable = JbigStandardHuffmanTable.TableB15,
                    DifferentialXTable = JbigStandardHuffmanTable.TableB15,
                    DifferentialYTable = JbigStandardHuffmanTable.TableB15,
                    RefinementSizeTable = JbigStandardHuffmanTable.TableB1,

                    RefinementTemplate = RefinementTemplate,
                    RefinementATX = RefinementATX,
                    RefinementATY = RefinementATY,
                };

                return textDecoder.Decode(reader, arithmeticDecoder, cx);
            }

            // 3)
            else
            {
                // 6.5.8.2.2 Decoding a bitmap when REFAGGNINST = 1
                var idi = UseHuffman
                    ? symbolCodes.DecodeValue(reader)
                    : arithmeticDecoder.DecodeSymbol(cx.IAID, symbolCodeLength);

                var rdx = UseHuffman
                    ? JbigStandardHuffmanTable.TableB15.DecodeValue(reader)
                    : arithmeticDecoder.DecodeInteger(cx.IARDX);

                var rdy = UseHuffman
                    ? JbigStandardHuffmanTable.TableB15.DecodeValue(reader)
                    : arithmeticDecoder.DecodeInteger(cx.IARDY);

                if (UseHuffman)
                {
                    var symbolSize = JbigStandardHuffmanTable.TableB1.DecodeValue(reader);
                    reader.AlignByte();
                    arithmeticDecoder = new JbigArithmeticDecoder(reader, symbolSize);
                    reader.SkipBytes(symbolSize);
                }

                var refinementDecoder = new JbigGenericRefinementRegionDecoder
                {
                    Width = symbolWidth,
                    Height = symbolHeight,
                    Template = RefinementTemplate,
                    ReferenceBitmap = inputAndNewSymbols[idi],
                    ReferenceDx = rdx,
                    ReferenceDy = rdy,
                    TypicalPrediction = false,
                    ATX = RefinementATX,
                    ATY = RefinementATY,
                };

                return refinementDecoder.Decode(arithmeticDecoder, cx);
            }
        }

        private JbigBitmap[] GetExportedSymbols(VariableBitReader reader,
            JbigArithmeticDecoder arithmeticDecoder, JbigArithmeticContexts cx,
            JbigBitmap[] inputAndNewSymbols)
        {
            // 6.5.10 Exported symbols

            var exportNextSymbol = false;
            var exportedSymbols = new List<JbigBitmap>();

            var lastRunWasZeroLength = false;

            for (var exportIndex = 0; exportIndex < inputAndNewSymbols.Length;)
            {
                var exportRunLength = UseHuffman
                    ? JbigStandardHuffmanTable.TableB1.DecodeValue(reader)
                    : arithmeticDecoder.DecodeInteger(cx.IAEX);

                if (exportRunLength < 0)
                {
                    throw new JbigException("Invalid export run length " + exportRunLength);
                }

                if (exportRunLength == 0 && lastRunWasZeroLength)
                {
                    // Protect against malformed input
                    throw new JbigException("Unexpected consecutive zero length runs");
                }
                else
                {
                    lastRunWasZeroLength = exportRunLength == 0;
                }

                if (exportNextSymbol)
                {
                    for (var i = 0; i < exportRunLength && exportIndex < inputAndNewSymbols.Length; i++)
                    {
                        var symbolToExport = inputAndNewSymbols[exportIndex];
                        exportedSymbols.Add(symbolToExport);
                        exportIndex++;
                    }
                }
                else
                {
                    exportIndex += exportRunLength;
                }

                exportNextSymbol = !exportNextSymbol;
            }

            return exportedSymbols.ToArray();
        }

        private JbigBitmap ReadUncompressed(VariableBitReader reader, int width, int height)
        {
            // 6.5.9 3)

            var bitmap = new JbigBitmap(width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    bitmap[x, y] = reader.ReadBit() == 1;
                }

                reader.AlignByte();
            }

            return bitmap;
        }
    }
}
