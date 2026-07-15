// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.IO;
using System;
using System.IO;

namespace PdfToSvg.Imaging.Jpx.Container
{
    /// <summary>
    /// Detects whether the input is a bare JPEG 2000 code-stream or a JP2 file, and, for a JP2 file, walks the box
    /// structure to extract the code-stream and header boxes.
    /// </summary>
    internal static class JpxBoxParser
    {
        private const uint Jp2Brand = 0x6a703220; // 'jp2 '
        private const uint JpxBrand = 0x6a707820; // 'jpx '

        private const uint Jp2SignatureContent = 0x0d0a870a;

        private const int ComponentBitDepthVaries = 255;

        private class Jp2HeaderState
        {
            public bool FoundImageHeader;
            public bool FoundBitsPerComponent;
            public bool FoundPalette;
            public bool FoundComponentMapping;
            public bool FoundChannelDefinition;
            public int ColorSpecificationCount;
        }

        public static JpxContainer Parse(byte[] data, int offset, int count)
        {
            var reader = new JpxDataReader(data, offset, count);
            var container = new JpxContainer();

            // A bare code-stream starts directly with the SOC marker
            if (reader.TryReadMarker(JpxMarker.SOC))
            {
                container.IsJp2 = false;
                container.Codestream = new ArraySegment<byte>(data, offset, count);
                return container;
            }

            container.IsJp2 = true;
            ReadBoxes(reader, container);
            return container;
        }

        private static void ReadBoxes(JpxDataReader reader, JpxContainer container)
        {
            if (reader.Length < 8)
            {
                throw new JpxException("Invalid JPEG 2000 stream");
            }

            // ITU-T T.800 (06/2019) Figure I.1 describes structure

            var signature = ReadBox(reader);
            if (signature.Type != JpxBoxType.Signature)
            {
                throw new JpxException("Missing JPEG 2000 signature box");
            }
            ReadSignature(signature.Content);

            var fileType = ReadBox(reader);
            if (fileType.Type != JpxBoxType.FileType)
            {
                throw new JpxException("Missing JPEG 2000 'ftyp' file type box");
            }
            ReadFileType(fileType.Content);

            var foundHeader = false;
            var foundCodestream = false;
            var headerState = new Jp2HeaderState();

            while (reader.Cursor < reader.Length)
            {
                var box = ReadBox(reader);

                switch (box.Type)
                {
                    case JpxBoxType.Signature:
                        Log.WriteLine("Ignoring duplicate JPEG 2000 signature box");
                        break;

                    case JpxBoxType.FileType:
                        Log.WriteLine("Ignoring duplicate JPEG 2000 'ftyp' file type box");
                        break;

                    case JpxBoxType.JP2Header:
                        if (foundHeader)
                        {
                            Log.WriteLine("Ignoring duplicate JPEG 2000 'jp2h' header box");
                            break;
                        }
                        if (foundCodestream)
                        {
                            throw new JpxException("JPEG 2000 header box must appear before the codestream");
                        }

                        foundHeader = true;
                        ReadJp2Header(box.Content, container, headerState);
                        break;

                    case JpxBoxType.BitsPerComponent:
                    case JpxBoxType.ColourSpecification:
                    case JpxBoxType.Palette:
                    case JpxBoxType.ComponentMapping:
                    case JpxBoxType.ChannelDefinition:
                        // The spec requires these boxes inside jp2h, but some producers write too short a length on
                        // jp2h, causing trailing child boxes to end up at file level. Other readers tolerate this.
                        if (foundHeader)
                        {
                            Log.WriteLine("JPEG 2000 '{0}' box found at file level instead of within jp2h", box.Type);
                            ReadJp2HeaderChildBox(box, container, headerState);
                        }
                        break;

                    case JpxBoxType.ContiguousCodestream:
                        if (!foundHeader)
                        {
                            throw new JpxException("JPEG 2000 header box must appear before the codestream");
                        }
                        if (!foundCodestream)
                        {
                            foundCodestream = true;
                            container.Codestream = box.Content.Data;
                        }
                        break;
                }
            }

            if (!foundHeader)
            {
                throw new JpxException("Missing JPEG 2000 'jp2h' header box");
            }
            if (!foundCodestream)
            {
                throw new JpxException("Missing JPEG 2000 'jp2c' codestream box");
            }

            if (headerState.ColorSpecificationCount == 0)
            {
                throw new JpxException("Missing JPEG 2000 colour specification box");
            }
            if (container.ImageHeader.BitsPerComponent == ComponentBitDepthVaries && !headerState.FoundBitsPerComponent)
            {
                throw new JpxException("Missing JPEG 2000 bits per component box");
            }

            ValidatePaletteAndComponentMapping(container, headerState);
        }

        private static void ReadJp2Header(JpxDataReader reader, JpxContainer container, Jp2HeaderState state)
        {
            // ITU-T T.800 (06/2019) I.5.3 JP2 Header box
            var firstBox = true;

            while (reader.Cursor < reader.Length)
            {
                var box = ReadBox(reader);

                if (firstBox && box.Type != JpxBoxType.ImageHeader)
                {
                    throw new JpxException("JPEG 2000 image header box must be first in the JP2 header box");
                }

                if (!state.FoundImageHeader && box.Type != JpxBoxType.ImageHeader)
                {
                    throw new JpxException("JPEG 2000 image header box must precede JP2 header child boxes");
                }

                firstBox = false;

                ReadJp2HeaderChildBox(box, container, state);
            }

            if (!state.FoundImageHeader)
            {
                throw new JpxException("Missing JPEG 2000 image header box");
            }
        }

        private static void ReadJp2HeaderChildBox(JpxBox box, JpxContainer container, Jp2HeaderState state)
        {
            switch (box.Type)
            {
                case JpxBoxType.ImageHeader:
                    if (state.FoundImageHeader)
                    {
                        Log.WriteLine("Ignoring duplicate JPEG 2000 'ihdr' image header box");
                    }
                    else
                    {
                        state.FoundImageHeader = true;
                        container.ImageHeader = ReadImageHeader(box.Content);
                    }
                    break;

                case JpxBoxType.BitsPerComponent:
                    if (state.FoundBitsPerComponent)
                    {
                        Log.WriteLine("Ignoring duplicate JPEG 2000 'bpcc' bits per component box");
                    }
                    else
                    {
                        state.FoundBitsPerComponent = true;
                        container.BitsPerComponent = ReadBitsPerComponent(box.Content, container.ImageHeader);
                    }
                    break;

                case JpxBoxType.ColourSpecification:
                    state.ColorSpecificationCount++;
                    ReadColourSpecification(box.Content, container);
                    break;

                case JpxBoxType.Palette:
                    if (state.FoundPalette)
                    {
                        Log.WriteLine("Ignoring duplicate JPEG 2000 'pclr' palette box");
                    }
                    else
                    {
                        state.FoundPalette = true;
                        container.Palette = ReadPalette(box.Content);
                    }
                    break;

                case JpxBoxType.ComponentMapping:
                    if (state.FoundComponentMapping)
                    {
                        Log.WriteLine("Ignoring duplicate JPEG 2000 'cmap' component mapping box");
                    }
                    else
                    {
                        state.FoundComponentMapping = true;
                        container.ComponentMappings = ReadComponentMapping(box.Content, container);
                    }
                    break;

                case JpxBoxType.ChannelDefinition:
                    if (state.FoundChannelDefinition)
                    {
                        Log.WriteLine("Ignoring duplicate JPEG 2000 'cdef' channel definition box");
                    }
                    else
                    {
                        state.FoundChannelDefinition = true;
                        container.ChannelDefinitions = ReadChannelDefinition(box.Content);
                    }
                    break;
            }
        }

        private static JpxBox ReadBox(JpxDataReader reader)
        {
            const int UnknownLength = 0;
            const int XLBox = 1;

            var boxStart = reader.Cursor;
            if (reader.Length - reader.Cursor < 8)
            {
                throw new EndOfStreamException();
            }

            // ITU-T T.800 (06/2019) Section I.4 box structure
            var length = reader.ReadInt32();
            var type = (JpxBoxType)reader.ReadUInt32();

            if (length == XLBox)
            {
                if (reader.Length - reader.Cursor < 8)
                {
                    throw new EndOfStreamException();
                }

                var xlbox = (long)reader.ReadUInt64();

                if (xlbox < 16)
                {
                    throw new JpxException("Invalid JPEG 2000 box length");
                }

                var boxEnd = boxStart + xlbox;
                if (boxEnd > reader.Length || boxEnd < reader.Cursor)
                {
                    throw new JpxException("Invalid JPEG 2000 box length");
                }

                return new JpxBox(type, (int)xlbox, reader.Slice((int)(boxEnd - reader.Cursor)));
            }
            else if (length == UnknownLength || length >= 8)
            {
                var boxEnd = length == UnknownLength ? reader.Length : (long)boxStart + length;
                if (boxEnd > reader.Length || boxEnd < reader.Cursor)
                {
                    throw new JpxException("Invalid JPEG 2000 box length");
                }

                return new JpxBox(type, length, reader.Slice((int)(boxEnd - reader.Cursor)));
            }
            else
            {
                throw new JpxException("Invalid JPEG 2000 box length");
            }
        }

        private static void ReadSignature(JpxDataReader reader)
        {
            // ITU-T T.800 (06/2019) Section I.5.1
            if (reader.Length != 4 ||
                reader.ReadUInt32() != Jp2SignatureContent)
            {
                throw new JpxException("Invalid JPEG 2000 signature box");
            }
        }

        private static void ReadFileType(JpxDataReader reader)
        {
            // ITU-T T.800 (06/2019) Section I.5.2
            if (reader.Length < 8)
            {
                throw new JpxException("Invalid JPEG 2000 file type box");
            }

            var compatible = IsCompatibleBrand(reader.ReadUInt32());
            reader.ReadUInt32(); // Minor version

            while (reader.Length - reader.Cursor >= 4)
            {
                compatible |= IsCompatibleBrand(reader.ReadUInt32());
            }

            if (!compatible)
            {
                Log.WriteLine("JPEG 2000 file type box is not JP2 or JPX compatible. Decoding as JP2 anyway.");
            }
        }

        private static bool IsCompatibleBrand(uint brand)
        {
            // PDF embeds JPX baseline streams, so both the JP2 brand (T.800 I.5.2) and the JPX brands (T.801 M.8)
            // occur in practice
            return brand == Jp2Brand || brand == JpxBrand;
        }

        private static JpxImageHeaderBox ReadImageHeader(JpxDataReader reader)
        {
            if (reader.Length < 14)
            {
                throw new JpxException("Invalid JPEG 2000 image header box");
            }

            // ITU-T T.800 (06/2019) Section I.5.3.1 Image Header box
            var imageHeader = new JpxImageHeaderBox
            {
                Height = reader.ReadInt32(),
                Width = reader.ReadInt32(),
                NumberOfComponents = reader.ReadUInt16(),
                BitsPerComponent = reader.ReadByte(),
            };
            var compressionType = reader.ReadByte();
            var unknownColorSpace = reader.ReadByte();
            var intellectualProperty = reader.ReadByte();

            if (imageHeader.Height <= 0 ||
                imageHeader.Width <= 0 ||
                imageHeader.NumberOfComponents <= 0)
            {
                throw new JpxException("Invalid JPEG 2000 image header box");
            }
            if (compressionType != 7 ||
                unknownColorSpace > 1 ||
                intellectualProperty > 1 ||
                !IsValidBpc(imageHeader.BitsPerComponent))
            {
                throw new JpxException("Invalid JPEG 2000 image header box");
            }

            imageHeader.UnknownColorSpace = unknownColorSpace == 1;
            imageHeader.IntellectualProperty = intellectualProperty == 1;

            return imageHeader;
        }

        private static int[] ReadBitsPerComponent(JpxDataReader reader, JpxImageHeaderBox imageHeader)
        {
            // ITU-T T.800 (06/2019) Section I.5.3.2 Bits Per Component box
            if (reader.Length != imageHeader.NumberOfComponents)
            {
                throw new JpxException("Invalid JPEG 2000 bits per component box");
            }

            var bitsPerComponent = new int[imageHeader.NumberOfComponents];

            for (var i = 0; i < bitsPerComponent.Length; i++)
            {
                var bpc = reader.ReadByte();
                if (!IsValidBpc(bpc) || bpc == ComponentBitDepthVaries)
                {
                    throw new JpxException("Invalid JPEG 2000 bits per component box");
                }

                bitsPerComponent[i] = bpc;
            }

            return bitsPerComponent;
        }

        private static void ReadColourSpecification(JpxDataReader reader, JpxContainer container)
        {
            if (reader.Length < 3)
            {
                Log.WriteLine("Invalid JPEG 2000 colour specification box");
                return;
            }

            // ITU-T T.800 (06/2019) Section I.5.3.3 Colour Specification box
            var method = (JpxColorSpecificationMethod)reader.ReadByte();
            reader.SkipBytes(2); // Precedence + Approximation

            if (method == JpxColorSpecificationMethod.EnumeratedColorSpace)
            {
                if (reader.Length - reader.Cursor < 4)
                {
                    Log.WriteLine("Invalid JPEG 2000 colour specification box");
                    return;
                }

                var enumeratedColorSpace = (JpxEnumeratedColorSpace)reader.ReadInt32();

                // The first contiguous color specification box wins (I.5.3.3). Later boxes describe alternative
                // color interpretations and are ignored.
                // This is not entirely correct implemented, since we don't handle other METH values below, but it fits
                // our needs for PDF decoding well at the moment.
                if (container.EnumeratedColorSpace == JpxEnumeratedColorSpace.Unknown)
                {
                    container.EnumeratedColorSpace = enumeratedColorSpace;
                }
            }
            else
            {
                Log.WriteLine(
                    "Unsupported JPEG 2000 colour specification method {0} specified. " +
                    "Decoded image might have incorrect colors.",
                    method);
            }
        }

        private static JpxPaletteBox ReadPalette(JpxDataReader reader)
        {
            if (reader.Length < 3)
            {
                throw new JpxException("Invalid JPEG 2000 palette box");
            }

            // ITU-T T.800 (06/2019) Section I.5.3.4 Palette box
            var entryCount = reader.ReadUInt16();
            var columnCount = reader.ReadByte();

            if (entryCount < 1 || entryCount > 1024 || columnCount < 1)
            {
                throw new JpxException("Invalid JPEG 2000 palette box");
            }
            if (reader.Length - reader.Cursor < columnCount)
            {
                throw new JpxException("Invalid JPEG 2000 palette box");
            }

            var columns = new JpxPaletteColumn[columnCount];
            var bytesPerValue = new int[columnCount];
            var valuesByteCount = 0;

            for (var i = 0; i < columnCount; i++)
            {
                // Table I.13 – Bi values
                var b = reader.ReadByte();
                var precision = (b & 0b111_1111) + 1;
                if (precision > 31)
                {
                    // Note that the spec supports up to 38 bits, but this implementation stores colors as 32-bit
                    // integers.
                    throw new NotSupportedException("JPEG 2000 palette columns over 31 bits are not supported");
                }

                columns[i] = new JpxPaletteColumn(precision, signed: (b & 0b1000_0000) != 0);

                bytesPerValue[i] = MathUtils.BitsToBytes(precision);
                valuesByteCount += bytesPerValue[i];
            }

            if (reader.Length - reader.Cursor != (long)entryCount * valuesByteCount)
            {
                throw new JpxException("Invalid JPEG 2000 palette box");
            }

            var values = new int[entryCount, columnCount];
            for (var entry = 0; entry < entryCount; entry++)
            {
                for (var column = 0; column < columnCount; column++)
                {
                    values[entry, column] = ReadPaletteValue(reader, bytesPerValue[column], columns[column]);
                }
            }

            return new JpxPaletteBox
            {
                Columns = columns,
                Values = values,
            };
        }

        private static JpxComponentMappingBox[] ReadComponentMapping(JpxDataReader reader, JpxContainer container)
        {
            if ((reader.Length & 3) != 0)
            {
                throw new JpxException("Invalid JPEG 2000 component mapping box");
            }

            // ITU-T T.800 (06/2019) Section I.5.3.5 Component Mapping box
            const int ComponentMappingEntrySize = 4;

            var imageHeader = container.ImageHeader;
            var mappingCount = reader.Length / ComponentMappingEntrySize;
            if (mappingCount > JpxConstraints.MaxComponentMappings)
            {
                throw new JpxException("Invalid JPEG 2000 component mapping box");
            }

            var mappings = new JpxComponentMappingBox[mappingCount];
            for (var i = 0; i < mappings.Length; i++)
            {
                var componentIndex = reader.ReadUInt16();
                var mappingType = (JpxComponentMappingType)reader.ReadByte();
                var paletteColumnIndex = reader.ReadByte();

                if (mappingType > JpxComponentMappingType.Palette ||
                    componentIndex >= imageHeader.NumberOfComponents ||
                    mappingType == JpxComponentMappingType.Direct && paletteColumnIndex != 0)
                {
                    throw new JpxException("Invalid JPEG 2000 component mapping box");
                }

                mappings[i] = new JpxComponentMappingBox
                {
                    ComponentIndex = componentIndex,
                    Type = mappingType,
                    PaletteColumnIndex = paletteColumnIndex,
                };
            }

            return mappings;
        }

        private static void ValidatePaletteAndComponentMapping(JpxContainer container, Jp2HeaderState state)
        {
            // ITU-T T.800 (06/2019) Sections I.5.3.4 and I.5.3.5 require pclr and cmap to occur together, but do
            // not prescribe their relative order within jp2h. Cross-box validation therefore happens only after all
            // boxes have been parsed.
            if (state.FoundPalette != state.FoundComponentMapping)
            {
                throw new JpxException("JPEG 2000 palette and component mapping boxes must occur together");
            }

            if (!state.FoundPalette)
            {
                return;
            }

            var palette = container.Palette!;
            foreach (var mapping in container.ComponentMappings)
            {
                if (mapping.Type == JpxComponentMappingType.Palette &&
                    (mapping.PaletteColumnIndex < 0 || mapping.PaletteColumnIndex >= palette.ColumnCount))
                {
                    throw new JpxException("Invalid JPEG 2000 component mapping box");
                }
            }
        }

        private static JpxChannelDefinitionBox[] ReadChannelDefinition(JpxDataReader reader)
        {
            if (reader.Length < 2)
            {
                throw new JpxException("Invalid JPEG 2000 channel definition box");
            }

            // ITU-T T.800 (06/2019) Section I.5.3.6 Channel Definition box
            const int ChannelDefinitionSize = 6;
            var channelCount = reader.ReadUInt16();
            if (channelCount == 0 ||
                reader.Length - reader.Cursor != (long)channelCount * ChannelDefinitionSize)
            {
                throw new JpxException("Invalid JPEG 2000 channel definition box");
            }

            var definitions = new JpxChannelDefinitionBox[channelCount];

            for (var i = 0; i < definitions.Length; i++)
            {
                var channelIndex = reader.ReadUInt16();
                var type = (JpxChannelType)reader.ReadUInt16();
                var association = reader.ReadUInt16();

                if (type != JpxChannelType.Color &&
                    type != JpxChannelType.Opacity &&
                    type != JpxChannelType.PremultipliedOpacity &&
                    type != JpxChannelType.Unspecified)
                {
                    throw new JpxException("Invalid JPEG 2000 channel definition box");
                }

                definitions[i] = new JpxChannelDefinitionBox
                {
                    ChannelIndex = channelIndex,
                    Type = type,
                    Association = association,
                };
            }

            return definitions;
        }

        private static int ReadPaletteValue(JpxDataReader reader, int byteCount, JpxPaletteColumn column)
        {
            var value = 0;
            for (var i = 0; i < byteCount; i++)
            {
                value = (value << 8) | reader.ReadByte();
            }

            value = value & column.MaxValue;

            if (column.Signed)
            {
                var signBit = 1L << (column.Precision - 1);
                if ((value & signBit) != 0)
                {
                    value = (int)(value - (1L << column.Precision));
                }
            }

            return value;
        }

        private static bool IsValidBpc(int bpc)
        {
            // Table I.6 - BPC values
            if (bpc == ComponentBitDepthVaries)
            {
                return true;
            }

            var precision = (bpc & 0b111_1111) + 1;
            return precision >= 1 && precision <= 38;
        }
    }
}
