// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.IO;

namespace PdfToSvg.Imaging.Jpeg
{
    internal class JpegEncoder
    {
        private const int BlockSize = 8;

        private readonly MemoryStream stream = new MemoryStream();

        private readonly JpegQuantizationTable[] quantizationTables = new JpegQuantizationTable[4];

        private readonly JpegHuffmanTable[] huffmanDCTables = new JpegHuffmanTable[4];
        private readonly JpegHuffmanTable[] huffmanACTables = new JpegHuffmanTable[4];

        private JpegComponent[] components = ArrayUtils.Empty<JpegComponent>();

        private JpegImageDataWriter? imageDataWriter;
        private int leftUntilRestart;

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

            if (ColorSpace != JpegColorSpace.Ycck)
            {
                return;
            }

            using var writer = BeginSegment(JpegMarkerCode.APP14);

            writer.WriteByte('A');
            writer.WriteByte('d');
            writer.WriteByte('o');
            writer.WriteByte('b');
            writer.WriteByte('e');

            writer.WriteUInt16(0x100); // DCTEncodeVersion
            writer.WriteUInt16(0);     // flags0
            writer.WriteUInt16(0);     // flags1
            writer.WriteByte(2);       // colorTransform ycck
        }

        private void WriteQuantizationTables()
        {
            using var writer = BeginSegment(JpegMarkerCode.DQT);

            for (var i = 0; i < quantizationTables.Length; i++)
            {
                var quantizationTable = quantizationTables[i];
                if (quantizationTable.Quantizers != null)
                {
                    const int elementPrecision = 0;

                    writer.WriteNibble(elementPrecision);
                    writer.WriteNibble(i);

                    for (var n = 0; n < quantizationTable.Quantizers.Length; n++)
                    {
                        writer.WriteByte((byte)quantizationTable.Quantizers[n]);
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

            leftUntilRestart = RestartInterval;
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

            switch (colorSpace)
            {
                case JpegColorSpace.Ycck:
                    treatAsLuminance = new[] { true, false, false, true };
                    break;

                case JpegColorSpace.Cmyk:
                    treatAsLuminance = new[] { true, true, true, true };
                    chromaSubSampling = JpegChromaSubSampling.None;
                    break;

                case JpegColorSpace.Gray:
                    treatAsLuminance = new[] { true };
                    chromaSubSampling = JpegChromaSubSampling.None;
                    break;

                default:
                    treatAsLuminance = new[] { true, false, false };
                    break;
            }

            if (chromaSubSampling == JpegChromaSubSampling.None)
            {
                mcuWidth = 1;
                mcuHeight = 1;
            }
            else
            {
                mcuWidth = ((int)ChromaSubSampling) >> 4;
                mcuHeight = ((int)ChromaSubSampling) & 0xf;
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
        }

        public void WriteMetadata()
        {
            PrepareMetadata();

            WriteMarker(JpegMarkerCode.SOI);

            WriteApp0();
            WriteApp14();

            WriteQuantizationTables();
            WriteFrame();

            WriteHuffmanTables();
            WriteRestartInterval();

            WriteStartOfScan();
        }

        /// <summary>
        /// Writes data to the JPEG image. The data should contain interleaved component samples in the destination
        /// color space. No color space conversion is done by <see cref="WriteImageData"/>.
        /// </summary>
        public void WriteImageData(short[] data, int offset, int count)
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

        private void WriteMcuRow(JpegImageDataWriter imageDataWriter)
        {
            var inputBlock = new short[BlockSize * BlockSize];
            var dctBlock = new short[BlockSize * BlockSize];
            var zzBlock = new short[BlockSize * BlockSize];

            for (var mcuX = 0; mcuX < mcuPerLine; mcuX++)
            {
                if (RestartInterval > 0 && leftUntilRestart-- <= 0)
                {
                    leftUntilRestart += RestartInterval;
                    components.Restart();
                    imageDataWriter.WriteRestartMarker();
                }

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

                            JpegDct.Forward(inputBlock);

                            JpegZigZag.ZigZag(inputBlock, zzBlock);

                            component.QuantizationTable.Quantize(zzBlock);

                            var dc = zzBlock[0];
                            zzBlock[0] = (short)(dc - component.DCPredictor);
                            component.DCPredictor = dc;

                            imageDataWriter.WriteDataUnit(zzBlock, component.HuffmanDCTable, component.HuffmanACTable);
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
