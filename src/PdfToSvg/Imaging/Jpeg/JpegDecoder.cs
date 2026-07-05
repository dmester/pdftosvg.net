// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace PdfToSvg.Imaging.Jpeg
{
    /// <threadsafety instance="false" />
    internal class JpegDecoder
    {
        private const int BlockSize = 8;

        private readonly JpegQuantizationTable[] quantizationTables = new JpegQuantizationTable[4];

        private readonly JpegHuffmanTable[] huffmanDCTables = new JpegHuffmanTable[4];
        private readonly JpegHuffmanTable[] huffmanACTables = new JpegHuffmanTable[4];

        private int samplePrecision;
        private int lineCount;
        private int samplesPerLine;

        private JpegComponent[] frameComponents = ArrayUtils.Empty<JpegComponent>();
        private JpegComponent[] scanComponents = ArrayUtils.Empty<JpegComponent>();

        private ArraySegment<byte> scanData = new ArraySegment<byte>();

        private int restartInterval;

        private int mcuWidth;
        private int mcuHeight;

        private int adobeColorTransformCode = -1;

        public int Width => samplesPerLine;
        public int Height => lineCount;
        public int Components => frameComponents.Length;
        public bool IsSupported => Components > 0;

        public bool HasAdobeMarker => adobeColorTransformCode >= 0;

        public JpegColorSpace ColorSpace
        {
            get
            {
                // See Adobe specific color transform codes here:
                // https://docs.oracle.com/javase/8/docs/api/javax/imageio/metadata/doc-files/jpeg_metadata.html

                switch (Components)
                {
                    case 1:
                        return JpegColorSpace.Gray;

                    case 3:
                        return adobeColorTransformCode == 0
                            ? JpegColorSpace.Rgb
                            : JpegColorSpace.YCbCr;

                    case 4:
                        return adobeColorTransformCode == 2
                            ? JpegColorSpace.Ycck
                            : JpegColorSpace.Cmyk;

                    default:
                        return JpegColorSpace.Unknown;
                }
            }
        }

        public int Quality
        {
            get
            {
                const int DefaultQuality = 90;

                if (frameComponents.Length < 1)
                {
                    return DefaultQuality;
                }

                var luminanceTable = frameComponents[0].QuantizationTable;
                return luminanceTable.EstimateQuality(JpegQuantizationTable.Luminance);
            }
        }

        public JpegChromaSubSampling ChromaSubSampling
        {
            get
            {
                if (frameComponents.Length >= 3 &&
                    frameComponents[1].HorizontalSamplingFactor == 1 &&
                    frameComponents[1].VerticalSamplingFactor == 1 &&
                    frameComponents[2].HorizontalSamplingFactor == 1 &&
                    frameComponents[2].VerticalSamplingFactor == 1
                    )
                {
                    var h = frameComponents[0].HorizontalSamplingFactor;
                    var v = frameComponents[0].VerticalSamplingFactor;

                    var factor = (JpegChromaSubSampling)((h << 4) | v);

                    switch (factor)
                    {
                        case JpegChromaSubSampling.Ratio444:
                        case JpegChromaSubSampling.Ratio440:
                        case JpegChromaSubSampling.Ratio422:
                        case JpegChromaSubSampling.Ratio420:
                        case JpegChromaSubSampling.Ratio411:
                            return factor;
                    }
                }

                for (var i = 0; i < frameComponents.Length; i++)
                {
                    if (frameComponents[i].HorizontalSamplingFactor != 1 ||
                        frameComponents[i].VerticalSamplingFactor != 1)
                    {
                        return JpegChromaSubSampling.Custom;
                    }
                }

                return JpegChromaSubSampling.None;
            }
        }

        private void ReadQuantizationTables(JpegSegmentReader reader)
        {
            while (reader.Cursor < reader.Length)
            {
                var elementPrecision = reader.ReadNibble();
                var tableId = reader.ReadNibble();

                var quantizationTable = new ushort[JpegQuantizationTable.Size];

                if (elementPrecision == 0)
                {
                    // 8 bit precision
                    for (var i = 0; i < quantizationTable.Length; i++)
                    {
                        quantizationTable[i] = (ushort)reader.ReadByte();
                    }
                }
                else
                {
                    // 16 bit precision
                    for (var i = 0; i < quantizationTable.Length; i++)
                    {
                        quantizationTable[i] = (ushort)reader.ReadUInt16();
                    }
                }

                quantizationTables[tableId] = new JpegQuantizationTable(quantizationTable);
            }
        }

        private void ReadHuffmanTables(JpegSegmentReader reader)
        {
            while (reader.Cursor < reader.Length)
            {
                var tableClass = reader.ReadNibble();
                var tableId = reader.ReadNibble();

                var bits = reader.ReadBytes(16);

                var valueCount = 0;
                for (var i = 0; i < 16; i++)
                {
                    valueCount += bits.Array![bits.Offset + i];
                }

                var huffval = reader.ReadBytes(valueCount);

                var table = new JpegHuffmanTable(bits, huffval);

                if (tableClass == 0)
                {
                    huffmanDCTables[tableId] = table;
                }
                else
                {
                    huffmanACTables[tableId] = table;
                }
            }
        }

        private void ReadRestartInterval(JpegSegmentReader reader)
        {
            restartInterval = reader.ReadUInt16();
        }

        private void ReadFrame(JpegSegmentReader reader)
        {
            samplePrecision = reader.ReadByte();

            if (samplePrecision != 8)
            {
                if (samplePrecision == 12)
                {
                    throw new NotSupportedException(
                        "JPEG frames with 12-bit sample precision are not supported by this library");
                }
                else
                {
                    throw new JpegException("Invalid JPEG frame sample precision " + samplePrecision);
                }
            }

            lineCount = reader.ReadUInt16();
            samplesPerLine = reader.ReadUInt16();

            var componentCount = reader.ReadByte();
            frameComponents = new JpegComponent[componentCount];

            for (var i = 0; i < componentCount; i++)
            {
                var component = new JpegComponent();

                component.ComponentId = reader.ReadByte();
                component.HorizontalSamplingFactor = reader.ReadNibble();
                component.VerticalSamplingFactor = reader.ReadNibble();
                component.QuantizationTableId = reader.ReadByte();

                frameComponents[i] = component;

                mcuWidth = Math.Max(mcuWidth, component.HorizontalSamplingFactor);
                mcuHeight = Math.Max(mcuHeight, component.VerticalSamplingFactor);
            }
        }

        private void ReadApp14(JpegSegmentReader reader)
        {
            // Format specified in section 18 in:
            // https://www.pdfa.org/norm-refs/5116.DCT_Filter.pdf

            if (reader.ReadByte() == 'A' &&
                reader.ReadByte() == 'd' &&
                reader.ReadByte() == 'o' &&
                reader.ReadByte() == 'b' &&
                reader.ReadByte() == 'e')
            {
                var version = reader.ReadUInt16();
                var flags0 = reader.ReadUInt16();
                var flags1 = reader.ReadUInt16();
                adobeColorTransformCode = reader.ReadByte();
            }
        }

        private void ReadStartOfScan(JpegSegmentReader reader)
        {
            var numComponents = reader.ReadByte();

            scanComponents = new JpegComponent[numComponents];

            for (var i = 0; i < numComponents; i++)
            {
                var componentId = reader.ReadByte();
                var dcId = reader.ReadNibble();
                var acId = reader.ReadNibble();

                var component = new JpegComponent();

                var componentIndex = -1;

                for (var fi = 0; fi < frameComponents.Length; fi++)
                {
                    if (frameComponents[fi].ComponentId == componentId)
                    {
                        componentIndex = fi;
                        break;
                    }
                }

                if (componentIndex < 0)
                {
                    throw new JpegException("Unknown component id " + componentId + " in scan component " + i + ".");
                }

                var frameComponent = frameComponents[componentIndex];

                if (acId < 0 ||
                    acId >= huffmanACTables.Length ||
                    huffmanACTables[acId] == null)
                {
                    throw new JpegException("Unknown AC Huffman table selector " + acId + " in component id " + componentId + ".");
                }

                if (dcId < 0 ||
                    dcId >= huffmanDCTables.Length ||
                    huffmanDCTables[dcId] == null)
                {
                    throw new JpegException("Unknown DC Huffman table selector " + dcId + " in component id " + componentId + ".");
                }

                if (frameComponent.QuantizationTableId < 0 ||
                    frameComponent.QuantizationTableId >= quantizationTables.Length ||
                    quantizationTables[frameComponent.QuantizationTableId].Quantizers == null)
                {
                    throw new JpegException("Unknown quantization table id " + frameComponent.QuantizationTableId + " in component id " + componentId + ".");
                }

                component.ComponentIndex = componentIndex;

                component.HuffmanDCTableId = dcId;
                component.HuffmanDCTable = huffmanDCTables[dcId];

                component.HuffmanACTableId = acId;
                component.HuffmanACTable = huffmanACTables[acId];

                component.QuantizationTableId = frameComponent.QuantizationTableId;
                component.QuantizationTable = quantizationTables[frameComponent.QuantizationTableId];

                // Both the components and quantization tables should now be read
                frameComponent.QuantizationTable = component.QuantizationTable;

                component.HorizontalSamplingFactor = frameComponent.HorizontalSamplingFactor;
                component.VerticalSamplingFactor = frameComponent.VerticalSamplingFactor;

                scanComponents[i] = component;
            }

            // Not read fields:
            // Ss = reader.ReadByte();
            // Se = reader.ReadByte();
            // Ah = reader.ReadNibble();
            // Al = reader.ReadNibble();
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private void ReadBlock(JpegImageDataReader reader, JpegComponent component, float[] rawBlockBuffer, float[] output, int outputOffset)
        {
            if (outputOffset + BlockSize * BlockSize > output.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(outputOffset));
            }

            ref var pRawBlockBuffer = ref MemoryMarshal.GetArrayDataReference(rawBlockBuffer);

            reader.ReadDataUnit(rawBlockBuffer, component.HuffmanDCTable, component.HuffmanACTable, out var zeroAc);

            component.DCPredictor = (int)(pRawBlockBuffer = component.DCPredictor + pRawBlockBuffer);

#if NET8_0_OR_GREATER
            ref var pOutput = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(output), outputOffset);

            if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
            {
                // AVX2
                ref var pDecodedBlock = ref Unsafe.As<float, Vector256<float>>(ref pOutput);

                if (zeroAc)
                {
                    // Solid block => full IDCT and dequantization can be skipped
                    // T.81 A.3.1 Level shift needed
                    var shiftedValue = component.QuantizationTable.DCMultiplier * pRawBlockBuffer / 8 + 128f;
                    var clampedValue = MathUtils.Clamp(shiftedValue, 0f, 255f);
                    var valueVector = Vector256.Create(clampedValue);

                    Unsafe.Add(ref pDecodedBlock, 0) = valueVector;
                    Unsafe.Add(ref pDecodedBlock, 1) = valueVector;
                    Unsafe.Add(ref pDecodedBlock, 2) = valueVector;
                    Unsafe.Add(ref pDecodedBlock, 3) = valueVector;
                    Unsafe.Add(ref pDecodedBlock, 4) = valueVector;
                    Unsafe.Add(ref pDecodedBlock, 5) = valueVector;
                    Unsafe.Add(ref pDecodedBlock, 6) = valueVector;
                    Unsafe.Add(ref pDecodedBlock, 7) = valueVector;
                }
                else
                {
                    ref var pVectorBuffer = ref Unsafe.As<float, Vector256<float>>(ref pRawBlockBuffer);
                    var fRow0 = Unsafe.Add(ref pVectorBuffer, 0);
                    var fRow1 = Unsafe.Add(ref pVectorBuffer, 1);
                    var fRow2 = Unsafe.Add(ref pVectorBuffer, 2);
                    var fRow3 = Unsafe.Add(ref pVectorBuffer, 3);
                    var fRow4 = Unsafe.Add(ref pVectorBuffer, 4);
                    var fRow5 = Unsafe.Add(ref pVectorBuffer, 5);
                    var fRow6 = Unsafe.Add(ref pVectorBuffer, 6);
                    var fRow7 = Unsafe.Add(ref pVectorBuffer, 7);

                    component.QuantizationTable.DequantizeTransposedZigZag256(
                        ref fRow0, ref fRow1, ref fRow2, ref fRow3, ref fRow4, ref fRow5, ref fRow6, ref fRow7);

                    JpegDct.InverseAvx(ref fRow0, ref fRow1, ref fRow2, ref fRow3, ref fRow4, ref fRow5, ref fRow6, ref fRow7);

                    Unsafe.Add(ref pDecodedBlock, 0) = fRow0;
                    Unsafe.Add(ref pDecodedBlock, 1) = fRow1;
                    Unsafe.Add(ref pDecodedBlock, 2) = fRow2;
                    Unsafe.Add(ref pDecodedBlock, 3) = fRow3;
                    Unsafe.Add(ref pDecodedBlock, 4) = fRow4;
                    Unsafe.Add(ref pDecodedBlock, 5) = fRow5;
                    Unsafe.Add(ref pDecodedBlock, 6) = fRow6;
                    Unsafe.Add(ref pDecodedBlock, 7) = fRow7;
                }
            }
            else if (Vector128.IsHardwareAccelerated && Sse2.IsSupported)
            {
                // SSE2
                ref var pDecodedBlock = ref Unsafe.As<float, Vector128<float>>(ref pOutput);

                if (zeroAc)
                {
                    // Solid block => full IDCT and dequantization can be skipped
                    // T.81 A.3.1 Level shift needed
                    var shiftedValue = component.QuantizationTable.DCMultiplier * pRawBlockBuffer / 8 + 128f;
                    var clampedValue = MathUtils.Clamp(shiftedValue, 0f, 255f);
                    var valueVector = Vector128.Create(clampedValue);

                    for (var i = 0; i < 16; i++)
                    {
                        Unsafe.Add(ref pDecodedBlock, i) = valueVector;
                    }
                }
                else
                {
                    ref var pVectorBuffer = ref Unsafe.As<float, Vector128<float>>(ref pRawBlockBuffer);
                    var row0_lo = Unsafe.Add(ref pVectorBuffer, 0);
                    var row0_hi = Unsafe.Add(ref pVectorBuffer, 1);
                    var row1_lo = Unsafe.Add(ref pVectorBuffer, 2);
                    var row1_hi = Unsafe.Add(ref pVectorBuffer, 3);
                    var row2_lo = Unsafe.Add(ref pVectorBuffer, 4);
                    var row2_hi = Unsafe.Add(ref pVectorBuffer, 5);
                    var row3_lo = Unsafe.Add(ref pVectorBuffer, 6);
                    var row3_hi = Unsafe.Add(ref pVectorBuffer, 7);
                    var row4_lo = Unsafe.Add(ref pVectorBuffer, 8);
                    var row4_hi = Unsafe.Add(ref pVectorBuffer, 9);
                    var row5_lo = Unsafe.Add(ref pVectorBuffer, 10);
                    var row5_hi = Unsafe.Add(ref pVectorBuffer, 11);
                    var row6_lo = Unsafe.Add(ref pVectorBuffer, 12);
                    var row6_hi = Unsafe.Add(ref pVectorBuffer, 13);
                    var row7_lo = Unsafe.Add(ref pVectorBuffer, 14);
                    var row7_hi = Unsafe.Add(ref pVectorBuffer, 15);

                    component.QuantizationTable.DequantizeTransposedZigZag128(
                        ref row0_lo, ref row0_hi,
                        ref row1_lo, ref row1_hi,
                        ref row2_lo, ref row2_hi,
                        ref row3_lo, ref row3_hi,
                        ref row4_lo, ref row4_hi,
                        ref row5_lo, ref row5_hi,
                        ref row6_lo, ref row6_hi,
                        ref row7_lo, ref row7_hi
                        );

                    JpegDct.InverseSse(
                        ref row0_lo, ref row0_hi,
                        ref row1_lo, ref row1_hi,
                        ref row2_lo, ref row2_hi,
                        ref row3_lo, ref row3_hi,
                        ref row4_lo, ref row4_hi,
                        ref row5_lo, ref row5_hi,
                        ref row6_lo, ref row6_hi,
                        ref row7_lo, ref row7_hi);

                    Unsafe.Add(ref pDecodedBlock, 0) = row0_lo;
                    Unsafe.Add(ref pDecodedBlock, 1) = row0_hi;
                    Unsafe.Add(ref pDecodedBlock, 2) = row1_lo;
                    Unsafe.Add(ref pDecodedBlock, 3) = row1_hi;
                    Unsafe.Add(ref pDecodedBlock, 4) = row2_lo;
                    Unsafe.Add(ref pDecodedBlock, 5) = row2_hi;
                    Unsafe.Add(ref pDecodedBlock, 6) = row3_lo;
                    Unsafe.Add(ref pDecodedBlock, 7) = row3_hi;
                    Unsafe.Add(ref pDecodedBlock, 8) = row4_lo;
                    Unsafe.Add(ref pDecodedBlock, 9) = row4_hi;
                    Unsafe.Add(ref pDecodedBlock, 10) = row5_lo;
                    Unsafe.Add(ref pDecodedBlock, 11) = row5_hi;
                    Unsafe.Add(ref pDecodedBlock, 12) = row6_lo;
                    Unsafe.Add(ref pDecodedBlock, 13) = row6_hi;
                    Unsafe.Add(ref pDecodedBlock, 14) = row7_lo;
                    Unsafe.Add(ref pDecodedBlock, 15) = row7_hi;
                }
            }
            else
#endif
            {
                // Scalar
                if (zeroAc)
                {
                    // Solid block => full IDCT and dequantization can be skipped
                    // T.81 A.3.1 Level shift needed
                    var shiftedValue = component.QuantizationTable.DCMultiplier * pRawBlockBuffer / 8 + 128f;
                    var clampedValue = MathUtils.Clamp(shiftedValue, 0f, 255f);

                    for (var i = 0; i < BlockSize * BlockSize; i++)
                    {
                        output[outputOffset + i] = clampedValue;
                    }
                }
                else
                {
                    component.QuantizationTable.DequantizeTransposedZigZagScalar(rawBlockBuffer);

                    JpegDct.InverseScalar(rawBlockBuffer);

                    Array.Copy(rawBlockBuffer, 0, output, outputOffset, BlockSize * BlockSize);
                }
            }
        }

        public IEnumerable<int> ReadBlocks(float[] outputBlocks)
        {
            // E.2.3 Control procedure for decoding a scan

            var reader = new JpegImageDataReader(scanData);

            var mcusV = (lineCount - 1) / mcuHeight / BlockSize + 1;
            var mcusH = (samplesPerLine - 1) / mcuWidth / BlockSize + 1;

            var rawDataUnit = new float[BlockSize * BlockSize];

            var leftUntilRestart = restartInterval;

            var outputCount = 0;
            var maxBlockCount = (outputBlocks.Length / (BlockSize * BlockSize) / Components) * Components;

            for (var mcuY = 0; mcuY < mcusV; mcuY++)
            {
                for (var mcuX = 0; mcuX < mcusH; mcuX++)
                {
                    if (restartInterval > 0 && leftUntilRestart-- <= 0)
                    {
                        leftUntilRestart += restartInterval;

                        scanComponents.Restart();

                        if (!reader.ReadRestartMarker())
                        {
                            throw new JpegException("Expected restart marker.");
                        }
                    }

                    for (var c = 0; c < scanComponents.Length; c++)
                    {
                        var component = scanComponents[c];

                        for (var duy = 0; duy < component.VerticalSamplingFactor; duy++)
                        {
                            for (var dux = 0; dux < component.HorizontalSamplingFactor; dux++)
                            {
                                ReadBlock(reader, component, rawDataUnit, outputBlocks, outputCount * BlockSize * BlockSize);

                                outputCount++;

                                if (outputCount == maxBlockCount)
                                {
                                    yield return outputCount;
                                    outputCount = 0;
                                }
                            }
                        }
                    }
                }
            }

            if (outputCount > 0)
            {
                yield return outputCount;
            }
        }

        public IEnumerable<float[]> ReadImageData()
        {
            // E.2.3 Control procedure for decoding a scan

            var reader = new JpegImageDataReader(scanData);

            var mcusV = (lineCount - 1) / mcuHeight / BlockSize + 1;
            var mcusH = (samplesPerLine - 1) / mcuWidth / BlockSize + 1;

            var mcuRowBitmap = new JpegBitmap(samplesPerLine, mcuHeight * BlockSize, frameComponents.Length);
            var dataUnitBitmap = new JpegBitmap(BlockSize, BlockSize, 1);

            var rawDataUnit = new float[BlockSize * BlockSize];

            var leftUntilRestart = restartInterval;

            for (var mcuY = 0; mcuY < mcusV; mcuY++)
            {
                for (var mcuX = 0; mcuX < mcusH; mcuX++)
                {
                    if (restartInterval > 0 && leftUntilRestart-- <= 0)
                    {
                        leftUntilRestart += restartInterval;

                        scanComponents.Restart();

                        if (!reader.ReadRestartMarker())
                        {
                            throw new JpegException("Expected restart marker.");
                        }
                    }

                    var mcuDestX = mcuX * mcuWidth * BlockSize;

                    for (var c = 0; c < scanComponents.Length; c++)
                    {
                        var component = scanComponents[c];

                        var dataUnitWidth = BlockSize * mcuWidth / component.HorizontalSamplingFactor;
                        var dataUnitHeight = BlockSize * mcuHeight / component.VerticalSamplingFactor;

                        for (var duy = 0; duy < component.VerticalSamplingFactor; duy++)
                        {
                            for (var dux = 0; dux < component.HorizontalSamplingFactor; dux++)
                            {
                                ReadBlock(reader, component, rawDataUnit, dataUnitBitmap.Data, 0);

                                dataUnitBitmap.DrawNearestNeighbourClippedOnto(
                                    dest: mcuRowBitmap,
                                    destX: BlockSize * dux * mcuWidth / component.HorizontalSamplingFactor + mcuDestX,
                                    destY: BlockSize * duy * mcuHeight / component.VerticalSamplingFactor,
                                    destWidth: dataUnitWidth,
                                    destHeight: dataUnitHeight,
                                    destComponent: component.ComponentIndex
                                    );
                            }
                        }

                    }
                }

                yield return mcuRowBitmap.Data;
            }
        }

        public void ReadMetadata(byte[] data, int offset, int count)
        {
            var reader = new JpegSegmentReader(data, offset, count);

            var marker = (JpegMarkerCode)reader.ReadUInt16();
            if (marker != JpegMarkerCode.SOI)
            {
                throw new JpegException("Expected JPEG data to start with a SOI marker.");
            }

            marker = (JpegMarkerCode)reader.ReadUInt16();

            while (
                marker >= JpegMarkerCode.FirstMarker &&
                marker != JpegMarkerCode.EOI)
            {
                var segmentLength = reader.ReadUInt16();
                var segmentReader = reader.SliceReader(segmentLength - 2);

                switch (marker)
                {
                    case JpegMarkerCode.APP14:
                        ReadApp14(segmentReader);
                        break;

                    case JpegMarkerCode.DHT:
                        ReadHuffmanTables(segmentReader);
                        break;

                    case JpegMarkerCode.DQT:
                        ReadQuantizationTables(segmentReader);
                        break;

                    case JpegMarkerCode.DRI:
                        ReadRestartInterval(segmentReader);
                        break;

                    case JpegMarkerCode.SOF0:
                        ReadFrame(segmentReader);
                        break;

                    case JpegMarkerCode.SOS:
                        if (IsSupported)
                        {
                            ReadStartOfScan(segmentReader);
                            scanData = reader.ReadBytes(reader.Length - reader.Cursor);
                        }
                        break;
                }

                if (reader.Cursor >= reader.Length)
                {
                    break;
                }

                marker = (JpegMarkerCode)reader.ReadUInt16();
            }
        }
    }
}
