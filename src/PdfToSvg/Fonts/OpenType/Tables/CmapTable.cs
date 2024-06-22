// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    [DebuggerDisplay("cmap")]
    internal class CMapTable : IBaseTable
    {
        public static TableFactory Factory => new("cmap", Read);
        public string Tag => "cmap";

        public CMapEncodingRecord[] EncodingRecords = ArrayUtils.Empty<CMapEncodingRecord>();

        private static IBaseTable? Read(OpenTypeReader reader)
        {
            var table = new CMapTable();

            var version = reader.ReadUInt16();
            var numTables = reader.ReadUInt16();

            var records = new RecordHolder<CMapEncodingRecord>[numTables];
            var readRecords = new List<CMapEncodingRecord>();

            for (var i = 0; i < records.Length; i++)
            {
                var holder = records[i] = new RecordHolder<CMapEncodingRecord>();

                holder.Record.PlatformID = (OpenTypePlatformID)reader.ReadUInt16();
                holder.Record.EncodingID = reader.ReadUInt16();
                holder.Offset = reader.ReadInt32();
            }

            foreach (var record in records)
            {
                if (record.Offset >= reader.Length)
                {
                    continue;
                }

                var localReader = reader.Slice(record.Offset, reader.Length - record.Offset);
                var format = localReader.ReadUInt16();

                var content = format switch
                {
                    0 => ReadFormat0(localReader),
                    4 => ReadFormat4(localReader),
                    6 => ReadFormat6(localReader),
                    10 => ReadFormat10(localReader),
                    12 => ReadFormat12(localReader),
                    _ => (ICMapFormat?)null,
                };

                if (content != null)
                {
                    record.Record.Content = content;
                    readRecords.Add(record.Record);
                }
            }

            table.EncodingRecords = readRecords.ToArray();

            return table;
        }

        private static CMapFormat0? ReadFormat0(OpenTypeReader reader)
        {
            const int MinSize = 262;
            if (reader.Length < MinSize)
            {
                return null;
            }

            var format = new CMapFormat0();

            reader.ReadUInt16(); // Other readers seem to ignore the length
            format.Language = reader.ReadUInt16();
            format.GlyphIdArray = reader.ReadBytes(256);

            return format;
        }

        private static CMapFormat4? ReadFormat4(OpenTypeReader reader)
        {
            const int HeaderSize = 14;
            if (reader.Length < HeaderSize)
            {
                return null;
            }

            var format = new CMapFormat4();

            var length = reader.ReadUInt16();
            format.Language = reader.ReadUInt16();

            var segCountX2 = reader.ReadUInt16();
            reader.ReadUInt16(); // searchRange
            reader.ReadUInt16(); // entrySelector
            reader.ReadUInt16(); // rangeShift

            var segCount = segCountX2 / 2;

            if (reader.Length < HeaderSize + 3 * segCountX2 + 2)
            {
                return null;
            }

            format.StartCode = new ushort[segCount];
            format.EndCode = new ushort[segCount];
            format.IdDelta = new short[segCount];
            format.IdRangeOffsets = new ushort[segCount];

            for (var i = 0; i < segCount; i++)
            {
                format.EndCode[i] = reader.ReadUInt16();
            }

            reader.ReadUInt16(); // reservedPad

            for (var i = 0; i < segCount; i++)
            {
                format.StartCode[i] = reader.ReadUInt16();
            }

            for (var i = 0; i < segCount; i++)
            {
                format.IdDelta[i] = reader.ReadInt16();
            }

            // We will consider these optional
            for (var i = 0; i < segCount && reader.Position + 2 <= reader.Length; i++)
            {
                format.IdRangeOffsets[i] = reader.ReadUInt16();
            }

            // Some PDFs contain fonts where the length exceeds the size of the table
            var glyphIdArraySize = Math.Min(length, reader.Length) - reader.Position;
            format.GlyphIdArray = new ushort[glyphIdArraySize / 2];

            for (var i = 0; i < format.GlyphIdArray.Length; i++)
            {
                format.GlyphIdArray[i] = reader.ReadUInt16();
            }

            return format;
        }

        private static CMapFormat6? ReadFormat6(OpenTypeReader reader)
        {
            const int MinSize = 10;
            if (reader.Length < MinSize)
            {
                return null;
            }

            var format = new CMapFormat6();

            reader.ReadUInt16(); // Other readers seem to ignore the length

            format.Language = reader.ReadUInt16();
            format.FirstCode = reader.ReadUInt16();
            var entryCount = reader.ReadUInt16();

            format.GlyphIdArray = new ushort[entryCount];

            for (var i = 0; i < format.GlyphIdArray.Length && reader.Position + 2 <= reader.Length; i++)
            {
                format.GlyphIdArray[i] = reader.ReadUInt16();
            }

            return format;
        }

        private static CMapFormat10? ReadFormat10(OpenTypeReader reader)
        {
            const int MinSize = 20;
            if (reader.Length < MinSize)
            {
                return null;
            }

            var format = new CMapFormat10();

            reader.ReadUInt16(); // Reserved
            reader.ReadUInt32(); // Other readers seem to ignore the length

            format.Language = reader.ReadUInt32();
            format.StartCharCode = reader.ReadUInt32();
            var entryCount = reader.ReadUInt32();

            format.GlyphIdArray = new ushort[entryCount];

            for (var i = 0; i < format.GlyphIdArray.Length && reader.Position + 2 <= reader.Length; i++)
            {
                format.GlyphIdArray[i] = reader.ReadUInt16();
            }

            return format;
        }

        private static CMapFormat12? ReadFormat12(OpenTypeReader reader)
        {
            const int MinSize = 16;
            if (reader.Length < MinSize)
            {
                return null;
            }

            var format = new CMapFormat12();

            reader.ReadUInt16(); // reserved
            reader.ReadInt32(); // Other readers seem to ignore the length

            format.Language = reader.ReadUInt32();
            var numGroups = reader.ReadUInt32();

            const int EntrySize = 3 * 4;
            var maxGroups = (uint)((reader.Length - reader.Position) / EntrySize);
            numGroups = Math.Min(numGroups, maxGroups);

            format.Groups = new CMapFormat12Group[numGroups];

            for (var i = 0; i < format.Groups.Length; i++)
            {
                var group = format.Groups[i] = new CMapFormat12Group();

                group.StartCharCode = reader.ReadUInt32();
                group.EndCharCode = reader.ReadUInt32();
                group.StartGlyphID = reader.ReadUInt32();
            }

            return format;
        }

        private static void WriteFormat0(OpenTypeWriter writer, OpenTypePlatformID platform, CMapFormat0 format0)
        {
            var startPos = writer.Position;

            writer.WriteUInt16(0);
            writer.WriteField16(out var lengthField);
            writer.WriteUInt16(platform == OpenTypePlatformID.Macintosh ? format0.Language : (ushort)0);

            var takeGlyphs = Math.Min(256, format0.GlyphIdArray.Length);

            writer.WriteBytes(format0.GlyphIdArray, 0, takeGlyphs);
            writer.WriteBytes(new byte[256 - takeGlyphs]);

            lengthField.WriteUInt16((ushort)(writer.Position - startPos));
        }

        private static void WriteFormat4(OpenTypeWriter writer, OpenTypePlatformID platform, CMapFormat4 format4)
        {
            var startPos = writer.Position;

            var segCount = format4.StartCode.Length;
            var segCountX2 = (ushort)(segCount * 2);
            var searchParams = new SearchParams(segCount, itemSize: 2);

            writer.WriteUInt16(4);
            writer.WriteField16(out var lengthField);
            writer.WriteUInt16(platform == OpenTypePlatformID.Macintosh ? format4.Language : (ushort)0);
            writer.WriteUInt16(segCountX2);
            writer.WriteUInt16(searchParams.SearchRange);
            writer.WriteUInt16(searchParams.EntrySelector);
            writer.WriteUInt16(searchParams.RangeShift);

            foreach (var endCode in format4.EndCode)
            {
                writer.WriteUInt16(endCode);
            }

            writer.WriteUInt16(0);

            foreach (var startCode in format4.StartCode)
            {
                writer.WriteUInt16(startCode);
            }

            foreach (var idDelta in format4.IdDelta)
            {
                writer.WriteInt16(idDelta);
            }

            foreach (var idRangeOffset in format4.IdRangeOffsets)
            {
                writer.WriteUInt16(idRangeOffset);
            }

            foreach (var glyphId in format4.GlyphIdArray)
            {
                writer.WriteUInt16(glyphId);
            }

            lengthField.WriteUInt16((ushort)(writer.Position - startPos));
        }

        private static void WriteFormat6(OpenTypeWriter writer, OpenTypePlatformID platform, CMapFormat6 format6)
        {
            var startPos = writer.Position;

            var entryCount = (ushort)format6.GlyphIdArray.Length;

            writer.WriteUInt16(6);
            writer.WriteField16(out var lengthField);
            writer.WriteUInt16(platform == OpenTypePlatformID.Macintosh ? format6.Language : (ushort)0);
            writer.WriteUInt16(format6.FirstCode);
            writer.WriteUInt16(entryCount);

            foreach (var glyphId in format6.GlyphIdArray)
            {
                writer.WriteUInt16(glyphId);
            }

            lengthField.WriteUInt16((ushort)(writer.Position - startPos));
        }

        private static void WriteFormat10(OpenTypeWriter writer, OpenTypePlatformID platform, CMapFormat10 format10)
        {
            var startPos = writer.Position;

            var entryCount = (ushort)format10.GlyphIdArray.Length;

            writer.WriteUInt16(10);
            writer.WriteUInt16(0); // Reserved
            writer.WriteField32(out var lengthField);
            writer.WriteUInt32(platform == OpenTypePlatformID.Macintosh ? format10.Language : 0);
            writer.WriteUInt32(format10.StartCharCode);
            writer.WriteUInt32(entryCount);

            foreach (var glyphId in format10.GlyphIdArray)
            {
                writer.WriteUInt16(glyphId);
            }

            lengthField.WriteInt32(writer.Position - startPos);
        }

        private static void WriteFormat12(OpenTypeWriter writer, OpenTypePlatformID platform, CMapFormat12 format12)
        {
            var startPos = writer.Position;

            var numGroups = (uint)format12.Groups.Length;

            writer.WriteUInt16(12);
            writer.WriteUInt16(0); // Reserved
            writer.WriteField32(out var lengthField);
            writer.WriteUInt32(platform == OpenTypePlatformID.Macintosh ? format12.Language : 0);
            writer.WriteUInt32(numGroups);

            foreach (var group in format12.Groups)
            {
                writer.WriteUInt32(group.StartCharCode);
                writer.WriteUInt32(group.EndCharCode);
                writer.WriteUInt32(group.StartGlyphID);
            }

            lengthField.WriteInt32(writer.Position - startPos);
        }

        void IBaseTable.Write(OpenTypeWriter writer)
        {
            var tableStartPos = writer.Position;

            var numTables = (ushort)EncodingRecords.Length;

            writer.WriteUInt16(0); // Version
            writer.WriteUInt16(numTables);

            var encodingOffsetFields = new OpenTypeWriterField32[numTables];

            for (var i = 0; i < EncodingRecords.Length; i++)
            {
                var encoding = EncodingRecords[i];

                writer.WriteUInt16((ushort)encoding.PlatformID);
                writer.WriteUInt16(encoding.EncodingID);
                writer.WriteField32(out encodingOffsetFields[i]);
            }

            for (var i = 0; i < EncodingRecords.Length; i++)
            {
                var encoding = EncodingRecords[i];

                encodingOffsetFields[i].WriteInt32(writer.Position - tableStartPos);

                switch (encoding.Content)
                {
                    case CMapFormat0 format0:
                        WriteFormat0(writer, encoding.PlatformID, format0);
                        break;

                    case CMapFormat4 format4:
                        WriteFormat4(writer, encoding.PlatformID, format4);
                        break;

                    case CMapFormat6 format6:
                        WriteFormat6(writer, encoding.PlatformID, format6);
                        break;

                    case CMapFormat10 format10:
                        WriteFormat10(writer, encoding.PlatformID, format10);
                        break;

                    case CMapFormat12 format12:
                        WriteFormat12(writer, encoding.PlatformID, format12);
                        break;

                    case null:
                        throw new ArgumentException("CMap encoding lacked content");

                    default:
                        throw new NotSupportedException(
                            "Unsupported OpenType CMap table format type " + encoding.Content?.GetType().FullName);
                }
            }
        }
    }

    internal class CMapEncodingRecord
    {
        public OpenTypePlatformID PlatformID;
        public ushort EncodingID;
        public ICMapFormat Content = new CMapFormat12();
    }

    internal interface ICMapFormat { }

    internal class CMapFormat0 : ICMapFormat
    {
        public ushort Language;
        public byte[] GlyphIdArray = new byte[256];
    }

    internal class CMapFormat4 : ICMapFormat
    {
        public ushort Language;
        public ushort[] EndCode = ArrayUtils.Empty<ushort>(); // Len = segCount
        public ushort[] StartCode = ArrayUtils.Empty<ushort>(); // Len = segCount
        public short[] IdDelta = ArrayUtils.Empty<short>(); // Len = segCount
        public ushort[] IdRangeOffsets = ArrayUtils.Empty<ushort>(); // Len = segCount
        public ushort[] GlyphIdArray = ArrayUtils.Empty<ushort>(); // Len = arbitrary
    }

    internal class CMapFormat6 : ICMapFormat
    {
        public ushort Language;
        public ushort FirstCode;
        public ushort[] GlyphIdArray = ArrayUtils.Empty<ushort>(); // Len = entryCount
    }

    internal class CMapFormat10 : ICMapFormat
    {
        public uint Language;
        public uint StartCharCode;
        public ushort[] GlyphIdArray = ArrayUtils.Empty<ushort>(); // Len = entryCount
    }

    internal class CMapFormat12 : ICMapFormat
    {
        public uint Language;
        public CMapFormat12Group[] Groups = ArrayUtils.Empty<CMapFormat12Group>();
    }

    [DebuggerDisplay("{" + nameof(StartCharCode) + "}-{" + nameof(EndCharCode) + "} => {" + nameof(StartGlyphID) + "}")]
    internal class CMapFormat12Group
    {
        public uint StartCharCode;
        public uint EndCharCode;
        public uint StartGlyphID;
    }
}
