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
    internal class TableDirectory
    {
        public SfntVersion SfntVersion;

        private static readonly ILookup<string?, TableFactory> tableFactories;

        static TableDirectory()
        {
            var factories = new[]
            {
                CMapTable.Factory,
                CMapTable.Factory,
                HeadTable.Factory,
                HheaTable.Factory,
                HmtxTable.Factory,
                MaxpTableV05.Factory,
                MaxpTableV10.Factory,
                NameTable.Factory,
                OS2Table.Factory,
                PostTableV1.Factory,
                PostTableV2.Factory,
                PostTableV25.Factory,
                PostTableV3.Factory,
                RawTable.Factory,
                GlyfTable.Factory,
                LocaTable.Factory,
            };

            tableFactories = factories.ToLookup(x => x.Tag);
        }

        public IBaseTable[] Tables = ArrayUtils.Empty<IBaseTable>();

        [DebuggerDisplay("{TableTag}")]
        internal class TableRecord
        {
            public string TableTag = "";
            public uint Checksum;
            public int Offset;
            public int Length;
        }

        public void Write(OpenTypeWriter writer)
        {
            const int TableRecordLength = 16;
            const int HeadChecksumAdjustmentOffset = 8;

            var isCffFont = Tables.Any(table => table.Tag == "CFF ");

            OptimalTableOrder.StorageSort(Tables, table => table.Tag, isCffFont);

            var numTables = (ushort)Tables.Length;
            var searchParams = new SearchParams(numTables, TableRecordLength);

            SfntVersion = isCffFont ? SfntVersion.Cff : SfntVersion.TrueType;

            writer.WriteUInt32((uint)SfntVersion);
            writer.WriteUInt16(numTables);
            writer.WriteUInt16(searchParams.SearchRange);
            writer.WriteUInt16(searchParams.EntrySelector);
            writer.WriteUInt16(searchParams.RangeShift);

            var recordsPosition = writer.Position;
            writer.Position += Tables.Length * TableRecordLength;

            var records = new TableRecord[Tables.Length];

            for (var i = 0; i < Tables.Length; i++)
            {
                var startPosition = writer.Position;

                Tables[i].Write(writer, Tables);

                var endPosition = writer.Position;

                var padding = 4 - (endPosition & 3);
                if (padding < 4)
                {
                    writer.Position += padding;
                }

                records[i] = new TableRecord
                {
                    TableTag = Tables[i].Tag,
                    Offset = startPosition,
                    Length = endPosition - startPosition,
                    Checksum = writer.Checksum(startPosition, endPosition),
                };
            }

            writer.Position = recordsPosition;

            OptimalTableOrder.DirectorySort(records, table => table.TableTag);

            foreach (var record in records)
            {
                writer.WritePaddedAscii(record.TableTag, 4);
                writer.WriteUInt32(record.Checksum);
                writer.WriteInt32(record.Offset);
                writer.WriteInt32(record.Length);
            }

            foreach (var record in records)
            {
                if (record.TableTag == "head")
                {
                    writer.Position = record.Offset + HeadChecksumAdjustmentOffset;
                    writer.WriteUInt32(0xB1B0AFBA - writer.Checksum(0, writer.Length));
                    break;
                }
            }
        }

        public static TableDirectory Read(byte[] data)
        {
            var reader = new OpenTypeReader(data, 0, data.Length);
            var result = new TableDirectory();

            result.SfntVersion = (SfntVersion)reader.ReadUInt32();

            if (result.SfntVersion != SfntVersion.TrueType &&
                result.SfntVersion != SfntVersion.Cff &&
                result.SfntVersion != SfntVersion.True &&
                result.SfntVersion != SfntVersion.Typ1)
            {
                throw new OpenTypeException("Unknown sfntVersion " + result.SfntVersion + ".");
            }

            if (result.SfntVersion == SfntVersion.True ||
                result.SfntVersion == SfntVersion.Typ1)
            {
                result.SfntVersion = SfntVersion.TrueType;
            }

            var numTables = reader.ReadUInt16();
            var searchRange = reader.ReadUInt16();
            var entrySelector = reader.ReadUInt16();
            var rangeShift = reader.ReadUInt16();

            var tableRecords = new List<TableRecord>();

            for (var i = 0; i < numTables; i++)
            {
                var table = new TableRecord();
                table.TableTag = reader.ReadAscii(4);
                table.Checksum = reader.ReadUInt32();
                table.Offset = reader.ReadInt32();
                table.Length = reader.ReadInt32();

                tableRecords.Add(table);
            }

            OptimalTableOrder.ReadSort(tableRecords, x => x.TableTag);

            var tables = new List<IBaseTable>(tableRecords.Count);

            for (var i = 0; i < tableRecords.Count; i++)
            {
                var record = tableRecords[i];
                var localReader = new OpenTypeReader(data, record.Offset, record.Length);
                var context = new OpenTypeReaderContext(record.TableTag, tables);

                var tagCandidates = new[] { record.TableTag, null };

                var table = tagCandidates
                    .SelectMany(tag => tableFactories[tag])
                    .Select(tableFactory =>
                    {
                        localReader.Position = 0;
                        return tableFactory.Create(localReader, context);
                    })
                    .FirstOrDefault(t => t != null);

                if (table == null)
                {
                    throw new OpenTypeException("Failed to parse table of type " + record.TableTag + ".");
                }

                tables.Add(table);
            }

            result.Tables = tables.ToArray();

            return result;
        }
    }
}
