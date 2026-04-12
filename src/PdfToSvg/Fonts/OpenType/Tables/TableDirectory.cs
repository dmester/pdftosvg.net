// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Utils;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
                CffTable.Factory,
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

            // Update TrueType offsets
            if (!isCffFont)
            {
                var glyfTable = Tables.Get<GlyfTable>();
                var headTable = Tables.Get<HeadTable>();
                if (glyfTable != null)
                {
                    var locaTable = Tables.GetOrCreate<LocaTable>();
                    UpdateLoca(locaTable, headTable, glyfTable);
                }
            }

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

        private void UpdateLoca(LocaTable locaTable, HeadTable? headTable, GlyfTable glyfTable)
        {
            var glyphs = glyfTable.Glyphs;

            if (glyphs.Length == 0)
            {
                locaTable.Offsets = ArrayUtils.Empty<uint>();
            }
            else
            {
                var offsets = new uint[glyphs.Length + 1];
                offsets[0] = 0;

                var cursor = 0;
                for (var i = 0; i < glyphs.Length; i++)
                {
                    ref var glyph = ref glyphs[i];
                    cursor += glyph.Data.Count;
                    offsets[i + 1] = (uint)cursor;
                }

                locaTable.Offsets = offsets;

                if (headTable != null)
                {
                    var lastOffset = offsets[offsets.Length - 1];
                    headTable.IndexToLocFormat = lastOffset > LocaTable.MaxShortOffset
                        ? LocaTable.LongFormat
                        : LocaTable.ShortFormat;
                }
            }
        }

        private const int HeaderSize = 12;
        private const int TableRecordSize = 4 * 4;

        private static void ReadHeader(OpenTypeReader reader, out SfntVersion sfntVersion, out int numTables)
        {
            sfntVersion = (SfntVersion)reader.ReadUInt32();

            if (sfntVersion != SfntVersion.TrueType &&
                sfntVersion != SfntVersion.Cff &&
                sfntVersion != SfntVersion.True &&
                sfntVersion != SfntVersion.Typ1)
            {
                throw new OpenTypeException("Unknown sfntVersion " + sfntVersion + ".");
            }

            if (sfntVersion == SfntVersion.True ||
                sfntVersion == SfntVersion.Typ1)
            {
                sfntVersion = SfntVersion.TrueType;
            }

            numTables = reader.ReadUInt16();
            var searchRange = reader.ReadUInt16();
            var entrySelector = reader.ReadUInt16();
            var rangeShift = reader.ReadUInt16();
        }

        private static void ReadTableDirectory(OpenTypeReader reader, int numTables, out TableRecord[] tableRecords)
        {
            tableRecords = new TableRecord[numTables];

            for (var i = 0; i < numTables; i++)
            {
                var table = new TableRecord();
                table.TableTag = reader.ReadAscii(4);
                table.Checksum = reader.ReadUInt32();
                table.Offset = reader.ReadInt32();
                table.Length = reader.ReadInt32();

                tableRecords[i] = table;
            }
        }

        private static void ReadTable(OpenTypeReader reader, TableRecord record, List<IBaseTable> tables)
        {
            var context = new OpenTypeReaderContext(record.TableTag, tables);

            var tagCandidates = new[] { record.TableTag, null };

            var table = tagCandidates
                .SelectMany(tag => tableFactories[tag])
                .Select(tableFactory =>
                {
                    reader.Position = 0;
                    return tableFactory.Create(reader, context);
                })
                .FirstOrDefault(t => t != null);

            if (table == null)
            {
                throw new OpenTypeException("Failed to parse table of type " + record.TableTag + ".");
            }

            tables.Add(table);
        }

        public static TableDirectory Read(byte[] data, Func<string, bool>? tableFilter = null)
        {
            var reader = new OpenTypeReader(data, 0, data.Length);
            var result = new TableDirectory();

            ReadHeader(reader, out result.SfntVersion, out var numTables);
            ReadTableDirectory(reader, numTables, out var tableRecords);

            OptimalTableOrder.ReadSort(tableRecords, x => x.TableTag);

            var tables = new List<IBaseTable>(tableRecords.Length);

            for (var i = 0; i < tableRecords.Length; i++)
            {
                var record = tableRecords[i];

                if (tableFilter != null && tableFilter(record.TableTag) == false)
                {
                    continue;
                }

                var tableReader = new OpenTypeReader(data, record.Offset, record.Length);
                ReadTable(tableReader, record, tables);
            }

            result.Tables = tables.ToArray();

            return result;
        }

        public static TableDirectory Read(Stream data, Func<string, bool>? tableFilter, CancellationToken cancellationToken)
        {
            var buffer = new byte[10240];

            void EnsureBufferCapacity(int desiredCapacity)
            {
                if (buffer.Length < desiredCapacity)
                {
                    buffer = new byte[desiredCapacity * 2];
                }
            }

            var headerSize = data.ReadAll(buffer, 0, HeaderSize, cancellationToken);

            var reader = new OpenTypeReader(buffer, 0, headerSize);
            var result = new TableDirectory();

            ReadHeader(reader, out result.SfntVersion, out var numTables);

            EnsureBufferCapacity(numTables * TableRecordSize);

            var tableDirectorySize = data.ReadAll(buffer, 0, numTables * TableRecordSize, cancellationToken);
            reader = new OpenTypeReader(buffer, 0, tableDirectorySize);

            ReadTableDirectory(reader, numTables, out var tableRecords);

            OptimalTableOrder.ReadSort(tableRecords, x => x.TableTag);

            var tables = new List<IBaseTable>(tableRecords.Length);

            for (var i = 0; i < tableRecords.Length; i++)
            {
                var record = tableRecords[i];

                if (tableFilter != null && tableFilter(record.TableTag) == false)
                {
                    continue;
                }

                data.Position = record.Offset;

                EnsureBufferCapacity(record.Length);

                var tableSize = data.ReadAll(buffer, 0, record.Length, cancellationToken);
                var tableReader = new OpenTypeReader(buffer, 0, tableSize);

                ReadTable(tableReader, record, tables);
            }

            result.Tables = tables.ToArray();

            return result;
        }

#if HAVE_ASYNC
        public static async Task<TableDirectory> ReadAsync(Stream data, Func<string, bool>? tableFilter, CancellationToken cancellationToken)
        {
            var buffer = new byte[10240];

            void EnsureBufferCapacity(int desiredCapacity)
            {
                if (buffer.Length < desiredCapacity)
                {
                    buffer = new byte[desiredCapacity * 2];
                }
            }

            var headerSize = await data.ReadAllAsync(buffer, 0, HeaderSize, cancellationToken).ConfigureAwait(false);

            var reader = new OpenTypeReader(buffer, 0, headerSize);
            var result = new TableDirectory();

            ReadHeader(reader, out result.SfntVersion, out var numTables);

            EnsureBufferCapacity(numTables * TableRecordSize);

            var tableDirectorySize = await data.ReadAllAsync(buffer, 0, numTables * TableRecordSize, cancellationToken).ConfigureAwait(false);
            reader = new OpenTypeReader(buffer, 0, tableDirectorySize);
            ReadTableDirectory(reader, numTables, out var tableRecords);

            OptimalTableOrder.ReadSort(tableRecords, x => x.TableTag);

            var tables = new List<IBaseTable>(tableRecords.Length);

            for (var i = 0; i < tableRecords.Length; i++)
            {
                var record = tableRecords[i];

                if (tableFilter != null && tableFilter(record.TableTag) == false)
                {
                    continue;
                }

                data.Position = record.Offset;

                EnsureBufferCapacity(record.Length);

                var tableSize = await data.ReadAllAsync(buffer, 0, record.Length, cancellationToken).ConfigureAwait(false);
                var tableReader = new OpenTypeReader(buffer, 0, tableSize);

                ReadTable(tableReader, record, tables);
            }

            result.Tables = tables.ToArray();

            return result;
        }
#endif
    }
}
