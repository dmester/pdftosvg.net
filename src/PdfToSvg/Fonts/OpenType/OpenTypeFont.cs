// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    /// <summary>
    /// Parses .ttf and .otf files.
    /// </summary>
    /// <remarks>
    /// Specification:
    /// https://docs.microsoft.com/en-us/typography/opentype/spec/otff
    /// </remarks>
    internal class OpenTypeFont
    {
        private readonly List<Table> tables = new List<Table>();
        private readonly List<OpenTypeCMap> cmaps = new List<OpenTypeCMap>();
        private readonly byte[] buffer;
        private int cursor;

        private OpenTypeFont(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public byte[] Content => buffer;

        public string? FontFamily { get; private set; }

        public string? FontSubfamily { get; private set; }

        public string? FullName { get; private set; }

        public string? Copyright { get; private set; }

        public IList<OpenTypeCMap> CMaps => cmaps;

        [DebuggerDisplay("{TableTag}")]
        private class Table
        {
            public string TableTag = "";
            public uint Checksum;
            public int Offset;
            public uint Length;
        }

        [DebuggerDisplay("{PlatformID} {EncodingID}")]
        private class CMapEncoding
        {
            public ushort EncodingID;
            public OpenTypePlatformID PlatformID;
            public int Offset;
        }

        [DebuggerDisplay("{PlatformID} {EncodingID} Lang={LanguageID} Name={NameID}")]
        private class NameRecord
        {
            public ushort EncodingID;
            public OpenTypePlatformID PlatformID;
            public ushort LanguageID;
            public OpenTypeNameID NameID;
            public ushort Length;
            public int Offset;
        }

        public static OpenTypeFont Parse(Stream stream)
        {
            var font = new OpenTypeFont(stream.ToArray());
            font.Read();
            return font;
        }

        private byte ReadUInt8() => buffer[cursor++];

        private ushort ReadUInt16()
        {
            var result = unchecked((ushort)((buffer[cursor] << 8) | buffer[cursor + 1]));
            cursor += 2;
            return result;
        }

        private short ReadInt16()
        {
            var result = unchecked((short)((buffer[cursor] << 8) | buffer[cursor + 1]));
            cursor += 2;
            return result;
        }

        private uint ReadUInt32()
        {
            var result = unchecked((uint)(
                (buffer[cursor] << 24) |
                (buffer[cursor + 1] << 16) |
                (buffer[cursor + 2] << 8) |
                buffer[cursor + 3]));
            cursor += 4;
            return result;
        }

        private int ReadInt32()
        {
            var result =
                (buffer[cursor] << 24) |
                (buffer[cursor + 1] << 16) |
                (buffer[cursor + 2] << 8) |
                buffer[cursor + 3];
            cursor += 4;
            return result;
        }

        private string ReadTag()
        {
            var result = Encoding.ASCII.GetString(buffer, cursor, 4);
            cursor += 4;
            return result;
        }

        private void Read()
        {
            ReadTableDirectory();

            foreach (var table in tables)
            {
                switch (table.TableTag)
                {
                    case "name":
                        ReadName(table);
                        break;

                    case "cmap":
                        ReadCMaps(table);
                        break;
                }
            }
        }

        private void ReadTableDirectory()
        {
            var sfntVersion = ReadUInt32();

            if (sfntVersion != 0x00010000 &&
                sfntVersion != 0x4F54544F)
            {
                throw new Exception("Not an OpenType font.");
            }

            var numTables = ReadUInt16();
            var searchRange = ReadUInt16();
            var entrySelector = ReadUInt16();
            var rangeShift = ReadUInt16();

            for (var i = 0; i < numTables; i++)
            {
                var table = new Table();
                table.TableTag = ReadTag();
                table.Checksum = ReadUInt32();
                table.Offset = ReadInt32();
                table.Length = ReadUInt32();

                tables.Add(table);
            }
        }

        private void ReadCMaps(Table table)
        {
            cursor = table.Offset;

            var version = ReadUInt16();
            var numTables = ReadUInt16();

            var encodings = new List<CMapEncoding>();

            for (var i = 0; i < numTables; i++)
            {
                var encoding = new CMapEncoding();
                encoding.PlatformID = (OpenTypePlatformID)ReadUInt16();
                encoding.EncodingID = ReadUInt16();
                encoding.Offset = ReadInt32();
                encodings.Add(encoding);
            }

            foreach (var encoding in encodings)
            {
                cursor = table.Offset + encoding.Offset;

                List<OpenTypeCMapRange> ranges;
                var format = ReadUInt16();

                switch (format)
                {
                    case 0:
                        ranges = ReadCMapFormat0();
                        break;

                    case 4:
                        ranges = ReadCMapFormat4();
                        break;

                    case 12:
                        ranges = ReadCMapFormat12();
                        break;

                    default:
                        continue;
                }

                cmaps.Add(new OpenTypeCMap(encoding.PlatformID, encoding.EncodingID, ranges));
            }
        }

        private void ReadName(Table table)
        {
            cursor = table.Offset;

            var version = ReadUInt16();
            var count = ReadUInt16();
            var storageOffset = ReadUInt16();

            var names = new List<NameRecord>();

            for (var i = 0; i < count; i++)
            {
                var name = new NameRecord();
                name.PlatformID = (OpenTypePlatformID)ReadUInt16();
                name.EncodingID = ReadUInt16();
                name.LanguageID = ReadUInt16();
                name.NameID = (OpenTypeNameID)ReadUInt16();
                name.Length = ReadUInt16();
                name.Offset = ReadUInt16();
                names.Add(name);
            }

            // Prefer English names for Windows
            var preferredNames = names
                .OrderBy(x => x.PlatformID == OpenTypePlatformID.Windows ? 0 : 1)
                .ThenBy(x => x.LanguageID == 1033 ? 0 : 1)
                .GroupBy(x => x.NameID)
                .Select(x => x.First());

            foreach (var name in preferredNames)
            {
                cursor = table.Offset + storageOffset + name.Offset;

                var encoding = name.PlatformID == OpenTypePlatformID.Windows ? Encoding.BigEndianUnicode : Encoding.ASCII;
                var value = encoding.GetString(buffer, cursor, name.Length);

                switch (name.NameID)
                {
                    case OpenTypeNameID.Copyright:
                        Copyright = value;
                        break;

                    case OpenTypeNameID.FontFamily:
                        FontFamily = value;
                        break;

                    case OpenTypeNameID.FontSubfamily:
                        FontSubfamily = value;
                        break;

                    case OpenTypeNameID.FullFontName:
                        FullName = value;
                        break;
                }
            }
        }

        private List<OpenTypeCMapRange> ReadCMapFormat0()
        {
            var ranges = new List<OpenTypeCMapRange>();

            var length = ReadUInt16();
            var language = ReadUInt16();

            for (var i = 0u; i < 256 && i < length - 6; i++)
            {
                var glyphId = ReadUInt8();
                if (glyphId != 0)
                {
                    ranges.Add(new OpenTypeCMapRange(
                        startUnicode: i,
                        endUnicode: i,
                        startGlyphIndex: glyphId
                        ));
                }
            }

            return ranges;
        }

        private List<OpenTypeCMapRange> ReadCMapFormat4()
        {
            var ranges = new List<OpenTypeCMapRange>();

            var length = ReadUInt16();
            var language = ReadUInt16();
            var segCount = ReadUInt16() / 2;
            var searchRange = ReadUInt16();
            var entrySelector = ReadUInt16();
            var rangeShift = ReadUInt16();

            var endCode = new uint[segCount];
            for (var i = 0; i < segCount; i++)
            {
                endCode[i] = ReadUInt16();
            }

            var reservedPad = ReadUInt16();

            var startCode = new uint[segCount];
            for (var i = 0; i < segCount; i++)
            {
                startCode[i] = ReadUInt16();
            }

            var idDelta = new int[segCount];
            for (var i = 0; i < segCount; i++)
            {
                idDelta[i] = ReadInt16();
            }

            var idRangeOffsets = new uint[segCount];
            for (var i = 0; i < segCount; i++)
            {
                idRangeOffsets[i] = ReadUInt16();
            }


            for (var i = 0; i < segCount; i++)
            {
                if (idRangeOffsets[i] == 0)
                {
                    ranges.Add(new OpenTypeCMapRange(
                        startUnicode: startCode[i],
                        endUnicode: endCode[i],
                        startGlyphIndex: unchecked((ushort)(startCode[i] + idDelta[i])) // Modulo 65536
                        ));
                }
                else
                {
                    // TODO Skip for now, implement later if needed
                }
            }

            return ranges;
        }

        private List<OpenTypeCMapRange> ReadCMapFormat12()
        {
            var ranges = new List<OpenTypeCMapRange>();

            var reserved = ReadUInt16();
            var length = ReadUInt32();
            var language = ReadUInt32();
            var numGroups = ReadUInt32();

            for (var i = 0; i < numGroups; i++)
            {
                var startCharCode = ReadUInt32();
                var endCharCode = ReadUInt32();
                var startGlyphID = ReadUInt32();

                ranges.Add(new OpenTypeCMapRange(
                    startUnicode: startCharCode,
                    endUnicode: endCharCode,
                    startGlyphIndex: startGlyphID
                    ));
            }

            return ranges;
        }
    }
}
