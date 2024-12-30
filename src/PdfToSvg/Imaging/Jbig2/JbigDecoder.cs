// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jbig2.Coding;
using PdfToSvg.Imaging.Jbig2.DecodingProcedures;
using PdfToSvg.Imaging.Jbig2.Extensions;
using PdfToSvg.Imaging.Jbig2.Model;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Imaging.Jbig2
{
    internal class JbigDecoder
    {
        private static readonly byte[] Signature = [0x97, 0x4A, 0x42, 0x32, 0x0D, 0x0A, 0x1A, 0x0A];
        private const int UnknownDataLength = unchecked((int)0xffffffff);
        private const int UnknownPageHeight = unchecked((int)0xffffffff);
        private const int MaxPageSize = 10000;

        private readonly List<JbigSegment> segments = new();
        private readonly Dictionary<int, JbigSegment> segmentsByNumber = new();
        private readonly List<JbigPage> pages = new();

        public bool EmbeddedMode { get; set; }

        public int PageCount => pages.Count;

        internal static int DetectGenericRegionLength(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            const int RegionFlagsIndex = 17;
            const int TrailingRowCountLength = 4;

            if (inputCount <= RegionFlagsIndex)
            {
                throw new JbigException("The generic region was too short");
            }

            var isMmrEncoding = (inputBuffer[inputOffset + RegionFlagsIndex] & 0b1) == 1;
            byte[] endingByteSequence = isMmrEncoding ? [0x00, 0x00] : [0xFF, 0xAC];

            var startSearchOffset = inputOffset + RegionFlagsIndex + 1;
            var endSearchOffset = inputOffset + inputCount - endingByteSequence.Length - TrailingRowCountLength;

            for (var offset = startSearchOffset; offset <= endSearchOffset; offset++)
            {
                var isMatch = true;

                for (var i = 0; i < endingByteSequence.Length; i++)
                {
                    if (inputBuffer[offset + i] != endingByteSequence[i])
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    return
                        offset - inputOffset +
                        endingByteSequence.Length +
                        TrailingRowCountLength;
                }
            }

            return -1;
        }

        private void ReadPageInformation(JbigSegment segment, int segmentIndex)
        {
            var reader = segment.Reader;
            var page = segment.Page ?? throw new JbigException("Missing " + nameof(JbigPage) + " instance");

            // 7.4.8 Page information segment syntax

            page.Width = reader.ReadBytesOrThrow(4);
            page.Height = reader.ReadBytesOrThrow(4);
            page.XResolution = reader.ReadBytesOrThrow(4);
            page.YResolution = reader.ReadBytesOrThrow(4);


            // 7.4.8.5 Page segment flags

            // Bit 7
            page.MightContainColouredSegment = reader.ReadBitOrThrow() == 1;

            // Bit 6
            page.CombinationOperatorOverridden = reader.ReadBitOrThrow() == 1;

            // Bit 5
            page.RequiresAuxiliaryBuffers = reader.ReadBitOrThrow() == 1;

            // Bit 3-4
            page.DefaultCombinationOperator = (JbigCombinationOperator)reader.ReadBitsOrThrow(2);

            // Bit 2
            page.DefaultPixelValue = reader.ReadBitOrThrow() == 1;

            // Bit 1
            page.MightContainRefinements = reader.ReadBitOrThrow() == 1;

            // Bit 0
            page.MightBeLossless = reader.ReadBitOrThrow() == 1;


            // 7.4.8.6 Page striping information

            // Bit 15
            page.IsStriped = reader.ReadBitOrThrow() == 1;

            // Bit 0-14
            page.MaximumStripeSize = reader.ReadBitsOrThrow(15);


            if (page.Height == UnknownPageHeight)
            {
                if (!page.IsStriped)
                {
                    throw new JbigException("Expected an explicit page height on a non-striped page");
                }

                var lastEndOfStripe = segments
                    .SkipWhile(seg => seg != segment)
                    .TakeWhile(seg => seg.Type != JbigSegmentType.EndOfPage)
                    .Where(seg => seg.Type == JbigSegmentType.EndOfStripe && seg.Page == page)
                    .LastOrDefault();

                if (lastEndOfStripe == null)
                {
                    throw new JbigException("Expected to see at least one End-Of-Stripe segment following a striped page information segment");
                }

                if (lastEndOfStripe.Reader.Length < 4)
                {
                    throw new JbigException("Expected the end-of-stripe segment to contain a 32 bit Y coordinate");
                }

                lastEndOfStripe.Reader.Cursor = new VariableBitReaderCursor();
                page.Height = lastEndOfStripe.Reader.ReadBitsOrThrow(32);
            }

            if (page.Width < 1 ||
                page.Height < 1 ||
                page.Width > MaxPageSize ||
                page.Height > MaxPageSize)
            {
                throw new JbigException("Invalid JBig2 page dimensions " + page.Width + "x" + page.Height);
            }

            page.Bitmap = new JbigBitmap(page.Width, page.Height);

            if (page.DefaultPixelValue)
            {
                page.Bitmap.Fill(page.DefaultPixelValue);
            }
        }

        private void ReadHuffmanTable(JbigSegment segment)
        {
            var reader = segment.Reader;
            var ranges = new List<JbigHuffmanRange>();

            // B.2.1 Code table flags

            // Bit 7
            reader.SkipReservedBits(1);

            // Bit 4-6
            var rangeLengthSize = reader.ReadBitsOrThrow(3) + 1;

            // Bit 1-3
            var prefixLengthSize = reader.ReadBitsOrThrow(3) + 1;

            // Bit 0
            var hasOutOfBand = reader.ReadBitOrThrow() == 1;


            // B.2 Code table structure
            // 2)
            var huffmanTableLow = reader.ReadBytesOrThrow(4);

            // 3)
            var huffmanTableHigh = reader.ReadBytesOrThrow(4);

            // 4)
            var currentRangeLow = huffmanTableLow;

            do
            {
                // 5) a)
                var prefixLength = reader.ReadBitsOrThrow(prefixLengthSize);

                // 5) b)
                var rangeLength = reader.ReadBitsOrThrow(rangeLengthSize);

                // 5) c)
                ranges.Add(JbigHuffmanRange.Range(currentRangeLow, prefixLength, rangeLength));

                currentRangeLow += 1 << rangeLength;
            }
            while (currentRangeLow < huffmanTableHigh);

            // 6)
            var lowPrefixLength = reader.ReadBitsOrThrow(prefixLengthSize);
            ranges.Add(JbigHuffmanRange.Lower(huffmanTableLow - 1, lowPrefixLength));

            // 8)
            var highPrefixLength = reader.ReadBitsOrThrow(prefixLengthSize);
            ranges.Add(JbigHuffmanRange.Upper(huffmanTableHigh, highPrefixLength));

            // 10)
            if (hasOutOfBand)
            {
                // 10) a)
                var oobPrefixLength = reader.ReadBitsOrThrow(prefixLengthSize);
                ranges.Add(JbigHuffmanRange.OutOfBand(oobPrefixLength));
            }

            segment.HuffmanTable = new JbigHuffmanTable(ranges);
        }

        /// <summary>
        /// Placeholder
        /// </summary>
        private static JbigHuffmanTable UserSuppliedTable = new JbigHuffmanTable([]);

        private JbigHuffmanTable ReadHuffmanTable(VariableBitReader reader, JbigSegment segment, params JbigHuffmanTable?[] huffmanTables)
        {
            var bits = MathUtils.IntLog2Ceil(huffmanTables.Length);
            var index = reader.ReadBitsOrThrow(bits);

            if (index < huffmanTables.Length)
            {
                var table = huffmanTables[index];
                if (table != null)
                {
                    return table;
                }
            }

            throw new ArgumentException("Invalid Huffman table " + index + " in segment " + segment.SegmentNumber);
        }

        private void ResolveUserSuppliedTable(JbigSegment segment, ref JbigHuffmanTable table)
        {
            if (ReferenceEquals(table, UserSuppliedTable))
            {
                table = segment.ReferredSegments
                    .Where(x => x.Type == JbigSegmentType.Tables)
                    .ElementAtOrDefault(segment.UsedCustomHuffmanTables)
                    ?.HuffmanTable
                    ??
                    throw new JbigException("Segment " + segment.SegmentNumber + " referred to a user supplied Huffman table, but no Huffman table was referred by the segment");

                segment.UsedCustomHuffmanTables++;
            }
        }

        private void ReadRegionSegmentInfo(JbigSegment segment)
        {
            var reader = segment.Reader;

            // 7.4.1 Region segment information field
            var region = segment.RegionInfo;

            region.Width = reader.ReadBytesOrThrow(4);
            region.Height = reader.ReadBytesOrThrow(4);
            region.X = reader.ReadBytesOrThrow(4);
            region.Y = reader.ReadBytesOrThrow(4);

            // 7.4.1.5 Region segment flags

            // Bit 4-7
            reader.SkipReservedBits(4);

            // Bit 3
            region.ColorExtension = reader.ReadBitOrThrow() == 1;

            // Bit 0-2
            region.CombinationOperator = (JbigCombinationOperator)reader.ReadBitsOrThrow(3);


            // Validate

            if (region.Width < 1 ||
                region.Height < 1 ||
                region.Width > MaxPageSize ||
                region.Height > MaxPageSize)
            {
                throw new JbigException("Invalid JBig2 region dimensions " + region.Width + "x" + region.Height);
            }
        }

        private void ReadGenericRegion(JbigSegment segment)
        {
            var reader = segment.Reader;
            var decoder = new JbigGenericRegionDecoder();

            ReadRegionSegmentInfo(segment);

            // 7.4.6.2 Generic region segment flags

            // Bit 5-7
            reader.SkipReservedBits(3);

            // Bit 4
            decoder.ExtendedTemplate = reader.ReadBitOrThrow() == 1;

            // Bit 3
            decoder.TypicalPrediction = reader.ReadBitOrThrow() == 1;

            // Bit 1-2
            decoder.Template = reader.ReadBitsOrThrow(2);

            // Bit 0
            decoder.UseMmr = reader.ReadBitOrThrow() == 1;

            // 7.4.6.3 Generic region segment AT flags
            if (!decoder.UseMmr)
            {
                var numAt =
                    decoder.Template != 0 ? 1 : // Figure 51
                    decoder.ExtendedTemplate ? 12 : // Figure 50
                    4; // Figure 49

                decoder.ATX = new sbyte[numAt];
                decoder.ATY = new sbyte[numAt];

                for (var i = 0; i < numAt; i++)
                {
                    decoder.ATX[i] = (sbyte)(byte)reader.ReadByte();
                    decoder.ATY[i] = (sbyte)(byte)reader.ReadByte();
                }
            }

            // Table 37
            decoder.Width = segment.RegionInfo.Width;
            decoder.Height = segment.RegionInfo.Height;
            decoder.ColorExtension = segment.RegionInfo.ColorExtension;

            var bitmap = decoder.Decode(reader);
            HandleRegionBitmap(segment, bitmap, isIntermediateRegion: segment.Type == JbigSegmentType.IntermediateGenericRegion);
        }

        private void ReadGenericRefinementRegion(JbigSegment segment)
        {
            var reader = segment.Reader;
            var decoder = new JbigGenericRefinementRegionDecoder();

            ReadRegionSegmentInfo(segment);


            // 7.4.7.2 Generic refinement region segment flags

            // Bit 2-7
            reader.SkipReservedBits(6);

            // Bit 1
            decoder.TypicalPrediction = reader.ReadBitOrThrow() == 1;

            // Bit 0
            decoder.Template = reader.ReadBitOrThrow();


            // 7.4.7.3 Generic refinement region segment AT flags

            if (decoder.Template == 0)
            {
                decoder.ATX = new sbyte[2];
                decoder.ATY = new sbyte[2];

                for (var i = 0; i < 2; i++)
                {
                    decoder.ATX[i] = (sbyte)(byte)reader.ReadByte();
                    decoder.ATY[i] = (sbyte)(byte)reader.ReadByte();
                }
            }


            // 7.4.7.4 Reference bitmap selection

            var referenceBitmap = segment.ReferredSegments
                .Select(x => x.Bitmap)
                .WhereNotNull()
                .FirstOrDefault()
                ??
                segment.Page?.Bitmap;


            if (referenceBitmap == null)
            {
                // No bitmap to refine was found
                return;
            }


            // Table 38

            decoder.Width = segment.RegionInfo.Width;
            decoder.Height = segment.RegionInfo.Height;
            decoder.ReferenceBitmap = referenceBitmap;
            decoder.ReferenceDx = 0;
            decoder.ReferenceDy = 0;

            var bitmap = decoder.Decode(reader, new JbigArithmeticContexts());
            HandleRegionBitmap(segment, bitmap, isIntermediateRegion: segment.Type == JbigSegmentType.IntermediateGenericRefinementRegion);
        }

        private void ReadPatternDictionary(JbigSegment segment)
        {
            var reader = segment.Reader;
            var decoder = new JbigPatternDictionaryDecoder();


            // 7.4.4.1.1 Pattern dictionary flags

            // Bit 3-7
            reader.SkipReservedBits(5);

            // Bit 1-2
            decoder.Template = reader.ReadBitsOrThrow(2);

            // Bit 0
            decoder.UseMmr = reader.ReadBitOrThrow() == 1;


            // 7.4.4.1 Pattern dictionary segment data header

            decoder.Width = reader.ReadByte();
            decoder.Height = reader.ReadByte();
            decoder.GrayMax = reader.ReadBytesOrThrow(4);

            segment.PatternDictionary = decoder.Decode(reader);
        }

        private void ReadHalftone(JbigSegment segment)
        {
            var reader = segment.Reader;
            var decoder = new JbigHalftoneRegionDecoder();

            ReadRegionSegmentInfo(segment);

            // 7.4.5.1.1 Halftone region segment flags

            // Bit 7
            decoder.DefaultPixel = reader.ReadBitOrThrow() == 1;

            // Bit 4-6
            decoder.CombinationOperator = (JbigCombinationOperator)reader.ReadBitsOrThrow(3);

            // Bit 3
            decoder.EnableSkip = reader.ReadBitOrThrow() == 1;

            // Bit 1-2
            decoder.Template = reader.ReadBitsOrThrow(2);

            // Bit 0
            decoder.UseMmr = reader.ReadBitOrThrow() == 1;


            // 7.4.5.1.2 Halftone grid position and size

            decoder.GridWidth = reader.ReadBytesOrThrow(4);
            decoder.GridHeight = reader.ReadBytesOrThrow(4);
            decoder.GridX = reader.ReadBytesOrThrow(4);
            decoder.GridY = reader.ReadBytesOrThrow(4);


            // 7.4.5.1.3 Halftone grid vector

            decoder.GridVectorX = reader.ReadBytesOrThrow(2);
            decoder.GridVectorY = reader.ReadBytesOrThrow(2);


            // Table 36
            var patternDict = segment.ReferredSegments
                .Select(x => x.PatternDictionary)
                .WhereNotNull()
                .FirstOrDefault();

            if (patternDict == null)
            {
                throw new JbigException("Expected to find a referred pattern dictionary for the halftone segment");
            }

            decoder.Width = segment.RegionInfo.Width;
            decoder.Height = segment.RegionInfo.Height;
            decoder.PatternWidth = patternDict.Patterns[0].Width;
            decoder.PatternHeight = patternDict.Patterns[0].Height;
            decoder.Patterns = patternDict.Patterns;

            var bitmap = decoder.Decode(reader, new JbigArithmeticContexts());
            HandleRegionBitmap(segment, bitmap, isIntermediateRegion: segment.Type == JbigSegmentType.IntermediateHalftoneRegion);
        }

        private void ReadTextRegion(JbigSegment segment)
        {
            var reader = segment.Reader;
            var decoder = new JbigTextRegionDecoder();

            decoder.Symbols = segment
                .ReferredSegments
                .Select(x => x.SymbolDictionary)
                .WhereNotNull()
                .SelectMany(x => x.Symbols)
                .ToArray();

            // 7.4.3.1 Text region segment data header
            ReadRegionSegmentInfo(segment);

            decoder.Width = segment.RegionInfo.Width;
            decoder.Height = segment.RegionInfo.Height;

            // 7.4.3.1.1 Text region segment flags

            // Bit 15
            decoder.RefinementTemplate = reader.ReadBitOrThrow();

            // Bit 10-14
            decoder.SOffset = reader.ReadBitsOrThrow(5);

            // Bit 9
            decoder.DefaultPixel = reader.ReadBitOrThrow() == 1;

            // Bit 7-8
            decoder.CombinationOperator = (JbigCombinationOperator)reader.ReadBitsOrThrow(2);

            // Bit 6
            decoder.Transposed = reader.ReadBitOrThrow() == 1;

            // Bit 4-5
            decoder.RefCorner = (JbigTextRegionCorner)reader.ReadBitsOrThrow(2);

            // Bit 2-3
            decoder.Log2StripSize = reader.ReadBitsOrThrow(2);

            // Bit 1
            decoder.UseRefinementCoding = reader.ReadBitOrThrow() == 1;

            // Bit 0
            decoder.UseHuffman = reader.ReadBitOrThrow() == 1;

            // 7.4.3.1.2 Text region segment Huffman flags
            if (decoder.UseHuffman)
            {
                // Bit 15
                reader.SkipReservedBits(1);

                // Bit 14
                decoder.RefinementSizeTable = ReadHuffmanTable(reader, segment, [
                    JbigStandardHuffmanTable.TableB1,
                    UserSuppliedTable,
                ]);

                // Bits 12-13
                decoder.DifferentialYTable = ReadHuffmanTable(reader, segment, [
                    JbigStandardHuffmanTable.TableB14,
                    JbigStandardHuffmanTable.TableB15,
                    null,
                    UserSuppliedTable,
                ]);

                // Bits 10-11
                decoder.DifferentialXTable = ReadHuffmanTable(reader, segment, [
                    JbigStandardHuffmanTable.TableB14,
                    JbigStandardHuffmanTable.TableB15,
                    null,
                    UserSuppliedTable,
                ]);

                // Bits 8-9
                decoder.DifferentialHeightTable = ReadHuffmanTable(reader, segment, [
                    JbigStandardHuffmanTable.TableB14,
                    JbigStandardHuffmanTable.TableB15,
                    null,
                    UserSuppliedTable,
                ]);

                // Bit 6-7
                decoder.DifferentialWidthTable = ReadHuffmanTable(reader, segment, [
                    JbigStandardHuffmanTable.TableB14,
                    JbigStandardHuffmanTable.TableB15,
                    null,
                    UserSuppliedTable,
                ]);

                // Bit 4-5
                decoder.SymbolTCoordinateTable = ReadHuffmanTable(reader, segment, [
                    JbigStandardHuffmanTable.TableB11,
                    JbigStandardHuffmanTable.TableB12,
                    JbigStandardHuffmanTable.TableB13,
                    UserSuppliedTable,
                ]);

                // Bit 2-3
                decoder.SubsequentSymbolSCoordinateTable = ReadHuffmanTable(reader, segment, [
                    JbigStandardHuffmanTable.TableB8,
                    JbigStandardHuffmanTable.TableB9,
                    JbigStandardHuffmanTable.TableB10,
                    UserSuppliedTable,
                ]);

                // Bit 0-1
                decoder.FirstSymbolSCoordinateTable = ReadHuffmanTable(reader, segment, [
                    JbigStandardHuffmanTable.TableB6,
                    JbigStandardHuffmanTable.TableB7,
                    null,
                    UserSuppliedTable,
                ]);

                // 7.4.3.1.6 Text region segment Huffman table selection
                ResolveUserSuppliedTable(segment, ref decoder.FirstSymbolSCoordinateTable);
                ResolveUserSuppliedTable(segment, ref decoder.SubsequentSymbolSCoordinateTable);
                ResolveUserSuppliedTable(segment, ref decoder.SymbolTCoordinateTable);
                ResolveUserSuppliedTable(segment, ref decoder.DifferentialWidthTable);
                ResolveUserSuppliedTable(segment, ref decoder.DifferentialHeightTable);
                ResolveUserSuppliedTable(segment, ref decoder.DifferentialXTable);
                ResolveUserSuppliedTable(segment, ref decoder.DifferentialYTable);
                ResolveUserSuppliedTable(segment, ref decoder.RefinementSizeTable);
            }

            // 7.4.3.1.3 Text region refinement AT flags
            if (decoder.UseRefinementCoding && decoder.RefinementTemplate == 0)
            {
                decoder.RefinementATX = new sbyte[2];
                decoder.RefinementATY = new sbyte[2];

                decoder.RefinementATX[0] = (sbyte)(byte)reader.ReadByte();
                decoder.RefinementATY[0] = (sbyte)(byte)reader.ReadByte();
                decoder.RefinementATX[1] = (sbyte)(byte)reader.ReadByte();
                decoder.RefinementATY[1] = (sbyte)(byte)reader.ReadByte();
            }

            // SBNUMINSTANCES
            decoder.SymbolInstanceCount = reader.ReadBytesOrThrow(4);

            if (decoder.UseHuffman)
            {
                decoder.SymbolCodes = ReadHuffmanCodes(reader, decoder.Symbols.Length);
            }

            var bitmap = decoder.Decode(reader, new JbigArithmeticContexts());
            HandleRegionBitmap(segment, bitmap, isIntermediateRegion: segment.Type == JbigSegmentType.IntermediateTextRegion);
        }

        private JbigHuffmanTable ReadHuffmanCodes(VariableBitReader reader, int symbolCount)
        {
            // 7.4.3.1.7 Symbol ID Huffman table decoding

            const int RunCodes = 35;

            // 1)
            var codeLengths = new List<JbigHuffmanRange>(RunCodes);

            for (var runCode = 0; runCode < RunCodes; runCode++)
            {
                var codeLength = reader.ReadBitsOrThrow(4);
                if (codeLength > 0)
                {
                    codeLengths.Add(JbigHuffmanRange.Single(runCode, codeLength));
                }
            }

            // 2)
            var runCodes = new JbigHuffmanTable(codeLengths);

            // 3) 4)
            var symbolIdCodeLengths = new List<JbigHuffmanRange>(symbolCount);
            var symbolIndex = 0;
            var previousLength = 0;

            do
            {
                var runCode = runCodes.DecodeValue(reader);

                if (runCode == 0)
                {
                    // Skip
                    previousLength = 0;
                    symbolIndex++;
                }
                else if (runCode < 32)
                {
                    symbolIdCodeLengths.Add(JbigHuffmanRange.Single(symbolIndex, runCode));
                    previousLength = runCode;
                    symbolIndex++;
                }
                else if (runCode == 32)
                {
                    // Copy previous length
                    var repeatTimes = reader.ReadBitsOrThrow(2) + 3;

                    for (var i = 0; i < repeatTimes; i++)
                    {
                        symbolIdCodeLengths.Add(JbigHuffmanRange.Single(symbolIndex, previousLength));
                        symbolIndex++;
                    }
                }
                else if (runCode == 33 || runCode == 34)
                {
                    // Repeat zero length
                    var repeatTimes = runCode == 33
                        ? reader.ReadBitsOrThrow(3) + 3
                        : reader.ReadBitsOrThrow(7) + 11;

                    symbolIndex += repeatTimes;
                    previousLength = 0;
                }
                else
                {
                    throw new JbigException("Unexpected runcode " + runCode);
                }
            }
            while (symbolIndex < symbolCount);

            reader.AlignByte();

            return new JbigHuffmanTable(symbolIdCodeLengths);
        }

        private void ReadSymbolDictionary(JbigSegment segment)
        {
            var reader = segment.Reader;
            var decoder = new JbigSymbolDictionaryDecoder();

            segment.SymbolDictionary = new JbigSymbolDictionary();

            // 7.4.2.1.1 Symbol dictionary flags

            // Bit 13-15
            reader.SkipReservedBits(3);

            // Bit 12
            decoder.RefinementTemplate = reader.ReadBitOrThrow();

            // Bit 10-11
            decoder.Template = reader.ReadBitsOrThrow(2);

            // Bit 8-9
            var bitmapCodingContextRetained = reader.ReadBitOrThrow() == 1;
            var bitmapCodingContextUsed = reader.ReadBitOrThrow() == 1;

            // Bit 7
            decoder.AggregatedSymbolCountTable = ReadHuffmanTable(reader, segment, [
                JbigStandardHuffmanTable.TableB1,
                UserSuppliedTable,
            ]);

            // Bit 6
            decoder.CollectiveBitmapSizeTable = ReadHuffmanTable(reader, segment, [
                JbigStandardHuffmanTable.TableB1,
                UserSuppliedTable,
            ]);

            // Bit 4-5
            decoder.DifferenceWidthTable = ReadHuffmanTable(reader, segment, [
                JbigStandardHuffmanTable.TableB2,
                JbigStandardHuffmanTable.TableB3,
                null,
                UserSuppliedTable,
            ]);

            // Bit 2-3
            decoder.DifferenceHeightTable = ReadHuffmanTable(reader, segment, [
                JbigStandardHuffmanTable.TableB4,
                JbigStandardHuffmanTable.TableB5,
                null,
                UserSuppliedTable,
            ]);

            // Bit 1
            decoder.RefinementAndAggregated = reader.ReadBitOrThrow() == 1;

            // Bit 0
            decoder.UseHuffman = reader.ReadBitOrThrow() == 1;

            // 7.4.2.1.2 Symbol dictionary AT flags
            if (!decoder.UseHuffman)
            {
                if (decoder.Template == 0)
                {
                    decoder.ATX = new sbyte[4];
                    decoder.ATY = new sbyte[4];

                    decoder.ATX[0] = (sbyte)(byte)reader.ReadByte();
                    decoder.ATY[0] = (sbyte)(byte)reader.ReadByte();
                    decoder.ATX[1] = (sbyte)(byte)reader.ReadByte();
                    decoder.ATY[1] = (sbyte)(byte)reader.ReadByte();
                    decoder.ATX[2] = (sbyte)(byte)reader.ReadByte();
                    decoder.ATY[2] = (sbyte)(byte)reader.ReadByte();
                    decoder.ATX[3] = (sbyte)(byte)reader.ReadByte();
                    decoder.ATY[3] = (sbyte)(byte)reader.ReadByte();
                }
                else
                {
                    decoder.ATX = new sbyte[1];
                    decoder.ATY = new sbyte[1];

                    decoder.ATX[0] = (sbyte)(byte)reader.ReadByte();
                    decoder.ATY[0] = (sbyte)(byte)reader.ReadByte();
                }
            }

            // 7.4.2.1.3 Symbol dictionary refinement AT flags
            if (decoder.RefinementAndAggregated && decoder.RefinementTemplate == 0)
            {
                decoder.RefinementATX = new sbyte[2];
                decoder.RefinementATY = new sbyte[2];

                decoder.RefinementATX[0] = (sbyte)(byte)reader.ReadByte();
                decoder.RefinementATY[0] = (sbyte)(byte)reader.ReadByte();
                decoder.RefinementATX[1] = (sbyte)(byte)reader.ReadByte();
                decoder.RefinementATY[1] = (sbyte)(byte)reader.ReadByte();
            }

            decoder.ExportedSymbolCount = reader.ReadBytesOrThrow(4);
            decoder.NewSymbolCount = reader.ReadBytesOrThrow(4);

            // 7.4.2.1.6 Symbol dictionary segment Huffman table selection
            ResolveUserSuppliedTable(segment, ref decoder.DifferenceHeightTable);
            ResolveUserSuppliedTable(segment, ref decoder.DifferenceWidthTable);
            ResolveUserSuppliedTable(segment, ref decoder.CollectiveBitmapSizeTable);
            ResolveUserSuppliedTable(segment, ref decoder.AggregatedSymbolCountTable);

            // Context
            var cx = new JbigArithmeticContexts();

            if (bitmapCodingContextUsed)
            {
                for (var i = segment.ReferredSegments.Count - 1; i >= 0; i++)
                {
                    var referredSegment = segment.ReferredSegments[i];

                    if (referredSegment.Type == JbigSegmentType.SymbolDictionary &&
                        referredSegment.SymbolDictionary != null &&
                        referredSegment.SymbolDictionary.RetainedGBContext != null &&
                        referredSegment.SymbolDictionary.RetainedGRContext != null)
                    {
                        cx.Restore(
                            referredSegment.SymbolDictionary.RetainedGBContext,
                            referredSegment.SymbolDictionary.RetainedGRContext
                            );
                        break;
                    }
                }
            }

            if (bitmapCodingContextRetained)
            {
                segment.SymbolDictionary.RetainedGBContext = cx.GB;
                segment.SymbolDictionary.RetainedGRContext = cx.GR;
            }

            decoder.InputSymbols = segment.ReferredSegments
                .Select(x => x.SymbolDictionary)
                .WhereNotNull()
                .SelectMany(x => x.Symbols)
                .ToArray();

            // Fallback if decoding fails
            segment.SymbolDictionary.Symbols = new JbigBitmap[decoder.ExportedSymbolCount];
            for (var i = 0; i < segment.SymbolDictionary.Symbols.Length; i++)
            {
                segment.SymbolDictionary.Symbols[i] = JbigBitmap.Empty;
            }

            segment.SymbolDictionary.Symbols = decoder.Decode(reader, cx);
        }

        private void HandleRegionBitmap(JbigSegment segment, JbigBitmap resultBitmap, bool isIntermediateRegion)
        {
            if (isIntermediateRegion)
            {
                segment.Bitmap = resultBitmap;
                return;
            }

            if (segment.Page == null)
            {
                return;
            }

            var combinationOperator = segment.Page.CombinationOperatorOverridden
                ? segment.RegionInfo.CombinationOperator
                : segment.Page.DefaultCombinationOperator;

            segment.Page.Bitmap.Draw(resultBitmap, segment.RegionInfo.X, segment.RegionInfo.Y, combinationOperator);
        }

        private int DetermineDataLength(JbigSegment segment, byte[] data, int offset, int count)
        {
            if (segment.DataLength != UnknownDataLength)
            {
                return segment.DataLength;
            }

            if (segment.Type == JbigSegmentType.IntermediateGenericRegion ||
                segment.Type == JbigSegmentType.ImmediateLosslessGenericRegion ||
                segment.Type == JbigSegmentType.ImmediateGenericRegion)
            {
                return DetectGenericRegionLength(data, offset + segment.Offset, count - segment.Offset);
            }

            throw new JbigException("An explicit length must be specified on segments that are not generic regions.");
        }

        public void Read(byte[] data, int offset, int count)
        {
            var reader = new VariableBitReader(data, offset, count);
            var sequentialFileStructure = true;

            // D.4.1 ID string
            if (ArrayUtils.StartsWith(data, offset, count, Signature))
            {
                reader.SkipBytes(Signature.Length);

                // D.4.2 File header flags
                reader.SkipReservedBits(4);
                var coloredSegments = reader.ReadBitOrThrow();
                var atPixels = reader.ReadBitOrThrow() == 1;
                var unknownPageCount = reader.ReadBitOrThrow() == 1;
                sequentialFileStructure = reader.ReadBitOrThrow() == 1;

                // D.4.3 Number of pages
                if (!unknownPageCount)
                {
                    reader.ReadBytesOrThrow(4);
                }
            }
            else if (!EmbeddedMode)
            {
                throw new JbigException(
                    "Expected to see JBIG2 file signature in a standalone JBIG2 file. " +
                    "Set " + nameof(EmbeddedMode) + " to true to allow decoding JBIG2 data without the file signature and header.");
            }

            var firstSegmentIndex = segments.Count;

            while (!reader.EndOfInput)
            {
                var segment = new JbigSegment();

                // 7.2.2 Segment number
                segment.SegmentNumber = reader.ReadBytesOrThrow(4);

                // 7.2.3 Segment header flags
                var deferredNonRetain = reader.ReadBitOrThrow();
                var pageFieldSize = reader.ReadBitOrThrow();
                segment.Type = (JbigSegmentType)reader.ReadBitsOrThrow(6);

                if (segment.Type == JbigSegmentType.PageInformation)
                {
                    segment.Page = new JbigPage();
                    pages.Add(segment.Page);
                    segment.Page.PageNumber = pages.Count;
                }

                // 7.2.4 Referred-to segment count and retention flags
                var referredSegmentCount = reader.ReadBitsOrThrow(3);
                if (referredSegmentCount <= 4)
                {
                    // Short format, occupying 1 byte
                    reader.ReadBitsOrThrow(5);
                }
                else if (referredSegmentCount == 7)
                {
                    // Long format
                    referredSegmentCount = reader.ReadBitsOrThrow(29);

                    var retainBitCount = 1 + referredSegmentCount; // A retain bit for the segment itself
                    var retainBitByteCount = MathUtils.BitsToBytes(retainBitCount);

                    reader.SkipBytes(retainBitByteCount);
                }
                else
                {
                    throw new JbigException("Invalid count of referred-to segments");
                }

                // 7.2.5 Referred-to segment numbers
                var segmentNumberLength =
                    segment.SegmentNumber <= 256 ? 1 :
                    segment.SegmentNumber <= 65536 ? 2 :
                    4;

                for (var i = 0; i < referredSegmentCount; i++)
                {
                    var referredSegmentNumber = reader.ReadBytesOrThrow(segmentNumberLength);
                    segment.ReferredSegmentNumbers.Add(referredSegmentNumber);

                    if (segmentsByNumber.TryGetValue(referredSegmentNumber, out var referredSegment))
                    {
                        segment.ReferredSegments.Add(referredSegment);
                    }
                    else
                    {
                        Log.WriteLine("Referred segment " + referredSegmentNumber + " not found");
                    }
                }

                // 7.2.6 Segment page association
                var associatedPage = reader.ReadBytesOrThrow(pageFieldSize == 0 ? 1 : 4);
                if (associatedPage > 0 && associatedPage <= pages.Count)
                {
                    segment.Page = pages[associatedPage - 1];
                }

                // 7.2.7 Segment data length
                segment.DataLength = reader.ReadBytesOrThrow(4);

                if (sequentialFileStructure)
                {
                    segment.Offset = reader.Cursor.Cursor;
                    segment.DataLength = DetermineDataLength(segment, data, offset, count);
                    reader.SkipBytes(segment.DataLength);
                }

                segments.Add(segment);
                segmentsByNumber[segment.SegmentNumber] = segment;

                if (segment.Type == JbigSegmentType.EndOfFile)
                {
                    break;
                }
            }

            var dataCursor = reader.Cursor.Cursor;

            for (var segmentIndex = firstSegmentIndex; segmentIndex < segments.Count; segmentIndex++)
            {
                var segment = segments[segmentIndex];

                if (!sequentialFileStructure)
                {
                    segment.Offset = dataCursor;
                    segment.DataLength = DetermineDataLength(segment, data, offset, count);
                }

                segment.Reader = new VariableBitReader(data, offset + segment.Offset, segment.DataLength);
                dataCursor += segment.DataLength;
            }
        }

        public JbigBitmap DecodePage(int pageNumber, CancellationToken cancellationToken)
        {
            if (pageNumber < 1 || pageNumber > pages.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number out of range. The page number should be one based.");
            }

            for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                var segment = segments[segmentIndex];

                if (segment.Page != null &&
                    segment.Page.PageNumber != pageNumber)
                {
                    continue;
                }

                if (segment.Decoded)
                {
                    continue;
                }
                segment.Decoded = true;

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    switch (segment.Type)
                    {
                        case JbigSegmentType.Tables:
                            ReadHuffmanTable(segment);
                            break;

                        case JbigSegmentType.SymbolDictionary:
                            ReadSymbolDictionary(segment);
                            break;

                        case JbigSegmentType.PageInformation:
                            ReadPageInformation(segment, segmentIndex);
                            break;

                        case JbigSegmentType.ImmediateLosslessTextRegion:
                        case JbigSegmentType.ImmediateTextRegion:
                        case JbigSegmentType.IntermediateTextRegion:
                            ReadTextRegion(segment);
                            break;

                        case JbigSegmentType.ImmediateGenericRegion:
                        case JbigSegmentType.ImmediateLosslessGenericRegion:
                        case JbigSegmentType.IntermediateGenericRegion:
                            ReadGenericRegion(segment);
                            break;

                        case JbigSegmentType.PatternDictionary:
                            ReadPatternDictionary(segment);
                            break;

                        case JbigSegmentType.ImmediateHalftoneRegion:
                        case JbigSegmentType.ImmediateLosslessHalftoneRegion:
                        case JbigSegmentType.IntermediateHalftoneRegion:
                            ReadHalftone(segment);
                            break;

                        case JbigSegmentType.ImmediateGenericRefinementRegion:
                        case JbigSegmentType.ImmediateLosslessGenericRefinementRegion:
                        case JbigSegmentType.IntermediateGenericRefinementRegion:
                            ReadGenericRefinementRegion(segment);
                            break;

                        case JbigSegmentType.EndOfStripe:
                            // Handled upfront by ReadPageInformation
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // We will try to decode as much as possible even if a single segment fails
                    Log.WriteLine("Failed to decode segment " + segment.SegmentNumber + ": " + ex);
                }
            }

            return pages[pageNumber - 1].Bitmap;
        }
    }
}
