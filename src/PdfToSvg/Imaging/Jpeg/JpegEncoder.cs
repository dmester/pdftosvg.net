// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.IO;

#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace PdfToSvg.Imaging.Jpeg
{
    /// <threadsafety instance="false" />
    internal class JpegEncoder
    {
        private const int BlockSize = 8;

        private readonly MemoryStream stream = new MemoryStream();

        // To prevent reallocating these buffers on each call
        private readonly int[] reusableIntBlock = new int[BlockSize * BlockSize];
        private readonly float[] reusableFloatBlock = new float[BlockSize * BlockSize];

        private readonly JpegQuantizationTable[] quantizationTables = new JpegQuantizationTable[4];

        private readonly JpegHuffmanTable[] huffmanDCTables = new JpegHuffmanTable[4];
        private readonly JpegHuffmanTable[] huffmanACTables = new JpegHuffmanTable[4];

        private JpegComponent[] components = ArrayUtils.Empty<JpegComponent>();

        private JpegImageDataWriter? imageDataWriter;
        private JpegBlockEnumerator blockEnumerator;

        private int nextLine;

        private int mcuWidth = 1;
        private int mcuHeight = 1;
        private int mcuPerLine;

        private JpegBitmap mcuRow = JpegBitmap.Empty;
        private int mcuRowCursor;

        public int Width { get; set; }
        public int Height { get; set; }

        public JpegColorSpace ColorSpace { get; set; } = JpegColorSpace.YCbCr;

        public int RestartInterval { get; set; }

        public JpegChromaSubSampling ChromaSubSampling { get; set; }

        public int Quality { get; set; } = 90;

        private JpegSegmentWriter BeginSegment(JpegMarkerCode marker)
        {
            return new JpegSegmentWriter(stream, marker);
        }

        private void WriteMarker(JpegMarkerCode marker)
        {
            stream.WriteByte(0xff);
            stream.WriteByte((byte)marker);
        }

        private void WriteApp0()
        {
            using var writer = BeginSegment(JpegMarkerCode.APP0);

            writer.WriteByte((byte)'J');
            writer.WriteByte((byte)'F');
            writer.WriteByte((byte)'I');
            writer.WriteByte((byte)'F');
            writer.WriteByte(0);

            writer.WriteUInt16(0x0102); // version
            writer.WriteByte(0);        // units

            writer.WriteUInt16(0);      // Xdensity
            writer.WriteUInt16(0);      // Ydensity

            writer.WriteByte(0);        // Xthumbnail
            writer.WriteByte(0);        // Ythumbnail
        }

        private void WriteApp14()
        {
            // Format specified in section 18 in:
            // https://www.pdfa.org/norm-refs/5116.DCT_Filter.pdf

            // More info:
            // https://exiftool.org/TagNames/JPEG.html#Adobe
            // https://docs.oracle.com/javase/8/docs/api/javax/imageio/metadata/doc-files/jpeg_metadata.html#color

            var colorTransform = ColorSpace switch
            {
                JpegColorSpace.Ycck => 2,
                JpegColorSpace.YCbCr => 1,
                _ => 0,
            };

            const int DCTDecodeVersion = 100;
            const int flags0 = 0;
            const int flags1 = 0;

            using var writer = BeginSegment(JpegMarkerCode.APP14);

            writer.WriteByte('A');
            writer.WriteByte('d');
            writer.WriteByte('o');
            writer.WriteByte('b');
            writer.WriteByte('e');

            writer.WriteUInt16(DCTDecodeVersion);
            writer.WriteUInt16(flags0);
            writer.WriteUInt16(flags1);
            writer.WriteByte(colorTransform);
        }

        private void WriteQuantizationTables()
        {
            using var writer = BeginSegment(JpegMarkerCode.DQT);

            for (var i = 0; i < quantizationTables.Length; i++)
            {
                var quantizationTable = quantizationTables[i];
                if (quantizationTable.Quantizers != null)
                {
                    // 8-bit precision (Pq = 0). This is the only precision allowed in baseline JPEG, so the
                    // quantization tables must be created with quantizers clamped to 255 (see
                    // JpegQuantizationTable.Quality with forceBaseline: true).
                    const int elementPrecision = 0;

                    writer.WriteNibble(elementPrecision);
                    writer.WriteNibble(i);

                    foreach (var quantizer in quantizationTable.Quantizers)
                    {
                        writer.WriteByte((byte)quantizer);
                    }
                }
            }
        }

        private void WriteHuffmanTables()
        {
            using var writer = BeginSegment(JpegMarkerCode.DHT);

            void WriteTables(JpegHuffmanTable[] tables, int tableClass)
            {
                for (var i = 0; i < tables.Length; i++)
                {
                    if (tables[i] == null)
                    {
                        continue;
                    }

                    writer.WriteNibble(tableClass);
                    writer.WriteNibble(i); // Table id

                    writer.WriteBytes(tables[i].Bits);
                    writer.WriteBytes(tables[i].Huffval);
                }
            }

            WriteTables(huffmanDCTables, tableClass: 0);
            WriteTables(huffmanACTables, tableClass: 1);
        }

        private void WriteRestartInterval()
        {
            if (RestartInterval == 0)
            {
                return;
            }

            using var writer = BeginSegment(JpegMarkerCode.DRI);
            writer.WriteUInt16(RestartInterval);

            blockEnumerator.ResetRestartInterval();
        }

        private void WriteFrame()
        {
            using var writer = BeginSegment(JpegMarkerCode.SOF0);

            const int samplePrecision = 8;

            writer.WriteByte(samplePrecision);
            writer.WriteUInt16(Height);
            writer.WriteUInt16(Width);

            writer.WriteByte(components.Length);

            for (var componentId = 0; componentId < components.Length; componentId++)
            {
                var component = components[componentId];

                writer.WriteByte(componentId);

                writer.WriteNibble(component.HorizontalSamplingFactor);
                writer.WriteNibble(component.VerticalSamplingFactor);

                writer.WriteByte(component.QuantizationTableId);
            }
        }

        private void WriteStartOfScan()
        {
            using var writer = BeginSegment(JpegMarkerCode.SOS);

            const int ss = 0;
            const int se = 63;
            const int ah = 0;
            const int al = 0;

            writer.WriteByte(components.Length);

            for (var componentId = 0; componentId < components.Length; componentId++)
            {
                var component = components[componentId];

                writer.WriteByte(componentId);
                writer.WriteNibble(component.HuffmanDCTableId);
                writer.WriteNibble(component.HuffmanACTableId);
            }

            writer.WriteByte(ss);
            writer.WriteByte(se);
            writer.WriteNibble(ah);
            writer.WriteNibble(al);
        }

        private void PrepareMetadata()
        {
            bool[] treatAsLuminance;

            var colorSpace = ColorSpace;
            var chromaSubSampling = ChromaSubSampling;

            if (chromaSubSampling == JpegChromaSubSampling.Custom)
            {
                chromaSubSampling = JpegChromaSubSampling.None;
            }

            switch (colorSpace)
            {
                case JpegColorSpace.Ycck:
                    treatAsLuminance = [true, false, false, true];
                    break;

                case JpegColorSpace.Cmyk:
                    treatAsLuminance = [true, true, true, true];
                    chromaSubSampling = JpegChromaSubSampling.None;
                    break;

                case JpegColorSpace.Rgb:
                    treatAsLuminance = [true, true, true];
                    chromaSubSampling = JpegChromaSubSampling.None;
                    break;

                case JpegColorSpace.Gray:
                    treatAsLuminance = [true];
                    chromaSubSampling = JpegChromaSubSampling.None;
                    break;

                default:
                    treatAsLuminance = [true, false, false];
                    break;
            }

            if (chromaSubSampling == JpegChromaSubSampling.None)
            {
                mcuWidth = 1;
                mcuHeight = 1;
            }
            else
            {
                mcuWidth = ((int)chromaSubSampling) >> 4;
                mcuHeight = ((int)chromaSubSampling) & 0xf;
            }

            quantizationTables[0] = JpegQuantizationTable.Luminance.Quality(Quality);
            quantizationTables[1] = JpegQuantizationTable.Chrominance.Quality(Quality);

            huffmanDCTables[0] = JpegHuffmanTable.DefaultLuminanceDCTable;
            huffmanDCTables[1] = JpegHuffmanTable.DefaultChrominanceDCTable;

            huffmanACTables[0] = JpegHuffmanTable.DefaultLuminanceACTable;
            huffmanACTables[1] = JpegHuffmanTable.DefaultChrominanceACTable;

            components = new JpegComponent[treatAsLuminance.Length];

            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i] = new JpegComponent();

                component.ComponentIndex = i;

                if (treatAsLuminance[i])
                {
                    component.QuantizationTableId = 0;

                    component.HuffmanDCTableId = 0;
                    component.HuffmanACTableId = 0;

                    component.HorizontalSamplingFactor = mcuWidth;
                    component.VerticalSamplingFactor = mcuHeight;
                }
                else
                {
                    component.QuantizationTableId = 1;

                    component.HuffmanDCTableId = 1;
                    component.HuffmanACTableId = 1;

                    component.HorizontalSamplingFactor = 1;
                    component.VerticalSamplingFactor = 1;
                }

                component.QuantizationTable = quantizationTables[component.QuantizationTableId];

                component.HuffmanDCTable = huffmanDCTables[component.HuffmanDCTableId];
                component.HuffmanACTable = huffmanACTables[component.HuffmanACTableId];
            }

            mcuPerLine = (Width - 1) / mcuWidth / BlockSize + 1;
            mcuRow = new JpegBitmap(Width, mcuHeight * BlockSize, components.Length);

            imageDataWriter = new JpegImageDataWriter(stream);
            blockEnumerator = new JpegBlockEnumerator(components, RestartInterval);
        }

        public void WriteMetadata()
        {
            PrepareMetadata();

            WriteMarker(JpegMarkerCode.SOI);

            //
            // Pdfium will not render JPEG images with an APP14 marker correctly if there is an APP0 marker.
            //
            // Handling in Java described here:
            // https://docs.oracle.com/javase/8/docs/api/javax/imageio/metadata/doc-files/jpeg_metadata.html#color
            //
            if (ColorSpace == JpegColorSpace.Ycck ||
                ColorSpace == JpegColorSpace.Rgb)
            {
                // Adobe JPEG
                WriteApp14();
            }
            else
            {
                // JFIF JPEG
                WriteApp0();
            }

            WriteQuantizationTables();
            WriteFrame();

            WriteHuffmanTables();
            WriteRestartInterval();

            WriteStartOfScan();
        }

        /// <summary>
        /// Writes data to the JPEG image. The data should contain interleaved component samples in the destination
        /// color space. No color space conversion is done by <see cref="WriteImageData(float[])"/>.
        /// </summary>
        public void WriteImageData(float[] data) => WriteImageData(data, 0, data.Length);

        /// <summary>
        /// Writes data to the JPEG image. The data should contain interleaved component samples in the destination
        /// color space. No color space conversion is done by <see cref="WriteImageData(float[], int, int)"/>.
        /// </summary>
        public void WriteImageData(float[] data, int offset, int count)
        {
            if (imageDataWriter == null)
            {
                throw new InvalidOperationException(
                    "Cannot write data before " + nameof(WriteMetadata) + " has been called.");
            }

            while (count > 0 && nextLine < Height)
            {
                var iterationRead = Math.Min(count, mcuRow.Length - mcuRowCursor);

                Array.Copy(data, offset, mcuRow.Data, mcuRowCursor, iterationRead);

                mcuRowCursor += iterationRead;
                offset += iterationRead;
                count -= iterationRead;

                if (mcuRowCursor == mcuRow.Length)
                {
                    WriteMcuRow(imageDataWriter);
                    mcuRowCursor = 0;
                }
            }
        }

        public void WriteEndImage()
        {
            if (imageDataWriter == null)
            {
                throw new InvalidOperationException(
                    "Cannot end an image before " + nameof(WriteMetadata) + " has been called.");
            }

            if (nextLine < Height)
            {
                var rowLength = Width * components.Length;
                var y = mcuRowCursor / rowLength;

                if (y > 0)
                {
                    mcuRowCursor = y * rowLength;

                    // Repeat last line
                    while (y++ < mcuHeight * BlockSize)
                    {
                        Array.Copy(
                            mcuRow.Data, mcuRowCursor - rowLength,
                            mcuRow.Data, mcuRowCursor,
                            rowLength);

                        mcuRowCursor += rowLength;
                    }

                    // Write last MCU
                    WriteMcuRow(imageDataWriter);
                    mcuRowCursor = 0;
                }
            }

            imageDataWriter.Dispose();
            WriteMarker(JpegMarkerCode.EOI);
        }

        public void WriteBlocks(float[] sourceBlocks, int blockCount)
        {
            if (sourceBlocks == null)
            {
                throw new ArgumentNullException(nameof(sourceBlocks));
            }
            if (blockCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockCount), "Block count cannot be negative");
            }
            if (blockCount * (BlockSize * BlockSize) > sourceBlocks.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(blockCount),
                    "The source block buffer does not contain enough data for " + blockCount + "blocks");
            }

            var imageDataWriter = this.imageDataWriter;
            if (imageDataWriter == null)
            {
                throw new InvalidOperationException(
                    "Cannot write data before " + nameof(WriteMetadata) + " has been called.");
            }

            var destBlock = reusableIntBlock;
            var scalarDctBlock = reusableFloatBlock;

            for (var blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                blockEnumerator.MoveNext();

                if (blockEnumerator.ShouldRestart)
                {
                    imageDataWriter.WriteRestartMarker();
                    components.Restart();
                }

                var component = blockEnumerator.Component;
                var isSolidBlock = false;

                var blockStartIndex = blockIndex * (BlockSize * BlockSize);

#if NET8_0_OR_GREATER
                if (Avx2.IsSupported)
                {
                    // AVX2
                    ref var pSourceBlock = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(sourceBlocks), blockStartIndex);
                    ref var srcRow0 = ref Unsafe.As<float, Vector256<float>>(ref pSourceBlock);

                    isSolidBlock = JpegBlockUtils.IsSolidBlock256Unsafe(ref pSourceBlock);

                    if (!isSolidBlock)
                    {
                        var row0 = srcRow0;
                        var row1 = Unsafe.Add(ref srcRow0, 1);
                        var row2 = Unsafe.Add(ref srcRow0, 2);
                        var row3 = Unsafe.Add(ref srcRow0, 3);
                        var row4 = Unsafe.Add(ref srcRow0, 4);
                        var row5 = Unsafe.Add(ref srcRow0, 5);
                        var row6 = Unsafe.Add(ref srcRow0, 6);
                        var row7 = Unsafe.Add(ref srcRow0, 7);

                        JpegDct.ForwardAvx(
                            ref row0,
                            ref row1,
                            ref row2,
                            ref row3,
                            ref row4,
                            ref row5,
                            ref row6,
                            ref row7);

                        component.QuantizationTable.QuantizeTransposedZigZag256(
                            ref row0,
                            ref row1,
                            ref row2,
                            ref row3,
                            ref row4,
                            ref row5,
                            ref row6,
                            ref row7);

                        JpegBlockUtils.FillBlock256Unsafe(
                            destBlock,
                            row0,
                            row1,
                            row2,
                            row3,
                            row4,
                            row5,
                            row6,
                            row7);
                    }
                }
                else if (Sse2.IsSupported)
                {
                    // SSE2
                    ref var pSourceBlock = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(sourceBlocks), blockStartIndex);
                    ref var srcRow0 = ref Unsafe.As<float, Vector128<float>>(ref pSourceBlock);

                    isSolidBlock = JpegBlockUtils.IsSolidBlock128Unsafe(ref pSourceBlock);

                    if (!isSolidBlock)
                    {
                        var row0_lo = srcRow0;
                        var row0_hi = Unsafe.Add(ref srcRow0, 1);
                        var row1_lo = Unsafe.Add(ref srcRow0, 2);
                        var row1_hi = Unsafe.Add(ref srcRow0, 3);
                        var row2_lo = Unsafe.Add(ref srcRow0, 4);
                        var row2_hi = Unsafe.Add(ref srcRow0, 5);
                        var row3_lo = Unsafe.Add(ref srcRow0, 6);
                        var row3_hi = Unsafe.Add(ref srcRow0, 7);
                        var row4_lo = Unsafe.Add(ref srcRow0, 8);
                        var row4_hi = Unsafe.Add(ref srcRow0, 9);
                        var row5_lo = Unsafe.Add(ref srcRow0, 10);
                        var row5_hi = Unsafe.Add(ref srcRow0, 11);
                        var row6_lo = Unsafe.Add(ref srcRow0, 12);
                        var row6_hi = Unsafe.Add(ref srcRow0, 13);
                        var row7_lo = Unsafe.Add(ref srcRow0, 14);
                        var row7_hi = Unsafe.Add(ref srcRow0, 15);

                        JpegDct.ForwardSse(
                            ref row0_lo, ref row0_hi,
                            ref row1_lo, ref row1_hi,
                            ref row2_lo, ref row2_hi,
                            ref row3_lo, ref row3_hi,
                            ref row4_lo, ref row4_hi,
                            ref row5_lo, ref row5_hi,
                            ref row6_lo, ref row6_hi,
                            ref row7_lo, ref row7_hi);

                        component.QuantizationTable.QuantizeTransposedZigZag128(
                            ref row0_lo, ref row0_hi,
                            ref row1_lo, ref row1_hi,
                            ref row2_lo, ref row2_hi,
                            ref row3_lo, ref row3_hi,
                            ref row4_lo, ref row4_hi,
                            ref row5_lo, ref row5_hi,
                            ref row6_lo, ref row6_hi,
                            ref row7_lo, ref row7_hi);

                        JpegBlockUtils.FillBlock128Unsafe(
                            destBlock,
                            row0_lo, row0_hi,
                            row1_lo, row1_hi,
                            row2_lo, row2_hi,
                            row3_lo, row3_hi,
                            row4_lo, row4_hi,
                            row5_lo, row5_hi,
                            row6_lo, row6_hi,
                            row7_lo, row7_hi);
                    }
                }
                else
#endif
                {
                    // Scalar
                    isSolidBlock = JpegBlockUtils.IsSolidBlockScalar(sourceBlocks, blockStartIndex);

                    if (!isSolidBlock)
                    {
                        JpegDct.ForwardScalar(sourceBlocks, blockStartIndex, scalarDctBlock);
                        component.QuantizationTable.QuantizeTransposedZigZagScalar(scalarDctBlock, destBlock);
                    }
                }

                if (isSolidBlock)
                {
                    // ForwardRow when all elements are equal:
                    //       [0]: x0 + x7 + x3 + x4 + x1 + x6 + x2 + x5 = 8 * (firstElement - 128)
                    //   [1...7]: 0
                    //
                    // After second pass:
                    //       [0]: 8 * (8 * (firstElement - 128))
                    //   [1...7]: 0
                    // 
                    // After downscale:
                    //       [0]: 8 * (firstElement - 128)
                    //   [1...7]: 0

                    var value = sourceBlocks[blockStartIndex];
                    var dc = MathUtils.RoundToInt((value - 128) * 8 * component.QuantizationTable.DCReverseMultiplier);

                    var diff = dc - component.DCPredictor;
                    component.DCPredictor = dc;

                    imageDataWriter.WriteDataUnitZeroAc(diff, component.HuffmanDCTable, component.HuffmanACTable);
                }
                else
                {
                    var dc = destBlock[0];
                    destBlock[0] = dc - component.DCPredictor;
                    component.DCPredictor = dc;

                    imageDataWriter.WriteDataUnitZigZag(destBlock, component.HuffmanDCTable, component.HuffmanACTable);
                }
            }
        }

        private void WriteMcuRow(JpegImageDataWriter imageDataWriter)
        {
            var inputBlock = new float[BlockSize * BlockSize];

            for (var mcuX = 0; mcuX < mcuPerLine; mcuX++)
            {
                for (var componentId = 0; componentId < components.Length; componentId++)
                {
                    var component = components[componentId];

                    var subSamplingX = 1;
                    var subSamplingY = 1;

                    if (component.VerticalSamplingFactor == 1 &&
                        component.HorizontalSamplingFactor == 1)
                    {
                        subSamplingX = mcuWidth;
                        subSamplingY = mcuHeight;
                    }

                    for (var duy = 0; duy < component.VerticalSamplingFactor; duy++)
                    {
                        for (var dux = 0; dux < component.HorizontalSamplingFactor; dux++)
                        {
                            mcuRow.GetBlock(inputBlock,
                                x: (mcuX * mcuWidth + dux) * BlockSize,
                                y: duy * BlockSize,
                                componentId,
                                subSamplingX, subSamplingY);

                            WriteBlocks(inputBlock, 1);
                        }
                    }
                }
            }

            nextLine += BlockSize * mcuHeight;
        }

        public byte[] ToByteArray()
        {
            return stream.ToArray();
        }
    }
}
