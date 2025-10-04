// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;

namespace PdfToSvg.Imaging.Jpeg
{
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

        public IEnumerable<short[]> ReadImageData()
        {
            // E.2.3 Control procedure for decoding a scan

            var reader = new JpegImageDataReader(scanData.Array!, scanData.Offset, scanData.Count);

            var mcusV = (lineCount - 1) / mcuHeight / BlockSize + 1;
            var mcusH = (samplesPerLine - 1) / mcuWidth / BlockSize + 1;

            var mcuRowBitmap = new JpegBitmap(samplesPerLine, mcuHeight * BlockSize, frameComponents.Length);
            var dataUnitBitmap = new JpegBitmap(BlockSize, BlockSize, 1);

            var rawDataUnit = new short[BlockSize * BlockSize];
            var deZigZaggedDataUnit = new short[BlockSize * BlockSize];

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
                                reader.ReadDataUnit(rawDataUnit, component.HuffmanDCTable, component.HuffmanACTable);

                                component.DCPredictor = rawDataUnit[0] = (short)(component.DCPredictor + rawDataUnit[0]);

                                component.QuantizationTable.Dequantize(rawDataUnit);

                                JpegZigZag.ReverseZigZag(rawDataUnit, dataUnitBitmap.Data);

                                JpegDct.Inverse(dataUnitBitmap.Data);

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
