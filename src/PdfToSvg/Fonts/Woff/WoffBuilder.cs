// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.Woff
{
    internal static class WoffBuilder
    {
        [DebuggerDisplay("{TableTag}")]
        internal class TableDirectoryEntry
        {
            public string Tag = "";

            public int Offset;
            public int CompLength;
            public int OrigOffset;
            public int OrigLength;
            public uint OrigChecksum;

            public byte[]? CompressedContent;
        }

        private static int Pad(int value)
        {
            return (value + 3) & unchecked((int)0xFFFFFFFC);
        }

        public static byte[] FromOpenType(byte[] binaryOtf)
        {
            var reader = new OpenTypeReader(binaryOtf, 0, binaryOtf.Length);

            var sfntVersion = reader.ReadUInt32();
            var numTables = reader.ReadUInt16();
            var searchRange = reader.ReadUInt16();
            var entrySelector = reader.ReadUInt16();
            var rangeShift = reader.ReadUInt16();

            var tables = new TableDirectoryEntry[numTables];
            var tablesStorageOrder = new TableDirectoryEntry[numTables];

            for (var i = 0; i < tables.Length; i++)
            {
                var tableEntry = new TableDirectoryEntry();

                tableEntry.Tag = reader.ReadAscii(4);
                tableEntry.OrigChecksum = reader.ReadUInt32();
                tableEntry.OrigOffset = reader.ReadInt32();
                tableEntry.OrigLength = reader.ReadInt32();

                tables[i] = tableEntry;
                tablesStorageOrder[i] = tableEntry;
            }

            Array.Sort(tablesStorageOrder, (a, b) => Comparer<int>.Default.Compare(a.OrigOffset, b.OrigOffset));

            const int WoffHeaderLength = 44;
            const int TableDirectoryEntryLength = 20;

            var fontTablesOffset = WoffHeaderLength + TableDirectoryEntryLength * tables.Length;
            var length = fontTablesOffset;
            var totalSfntSize = 12 + 16 * tables.Length;

            foreach (var table in tablesStorageOrder)
            {
                using (var compressedStream = new MemoryStream(table.OrigLength))
                {
                    using (var zlibStream = new ZLibStream(compressedStream, CompressionMode.Compress, leaveOpen: true))
                    {
                        zlibStream.Write(binaryOtf, table.OrigOffset, table.OrigLength);
                    }

                    if (compressedStream.Length < table.OrigLength)
                    {
                        table.CompressedContent = compressedStream.GetBufferOrArray();
                        table.CompLength = (int)compressedStream.Length;
                    }
                    else
                    {
                        table.CompLength = table.OrigLength;
                    }

                    table.Offset = length;

                    totalSfntSize += Pad(table.OrigLength);
                    length += Pad(table.CompLength);
                }
            }

            // WOFFHeader
            var writer = new OpenTypeWriter(binaryOtf.Length);
            writer.WriteAscii("wOFF");
            writer.WriteUInt32(sfntVersion);
            writer.WriteUInt32((uint)length); // Length
            writer.WriteUInt16((ushort)tables.Length);
            writer.WriteUInt16(0); // Reserved
            writer.WriteUInt32((uint)totalSfntSize);
            writer.WriteUInt16(1); // majorVersion
            writer.WriteUInt16(0); // majorVersion
            writer.WriteUInt32(0); // metaOffset
            writer.WriteUInt32(0); // metaLength
            writer.WriteUInt32(0); // metaOrigLength
            writer.WriteUInt32(0); // privOffset
            writer.WriteUInt32(0); // privLength

            // WOFF TableDirectoryEntry
            foreach (var tableEntry in tables)
            {
                writer.WriteAscii(tableEntry.Tag);
                writer.WriteUInt32((uint)tableEntry.Offset);
                writer.WriteUInt32((uint)tableEntry.CompLength);
                writer.WriteUInt32((uint)tableEntry.OrigLength);
                writer.WriteUInt32(tableEntry.OrigChecksum);
            }

            // FontTables
            foreach (var tableEntry in tablesStorageOrder)
            {
                writer.Position = tableEntry.Offset;

                if (tableEntry.CompressedContent == null)
                {
                    writer.WriteBytes(binaryOtf, tableEntry.OrigOffset, tableEntry.OrigLength);
                }
                else
                {
                    writer.WriteBytes(tableEntry.CompressedContent, 0, tableEntry.CompLength);
                }

                var padding = 4 - (tableEntry.CompLength % 4);
                if (padding < 4)
                {
                    writer.Position += padding;
                }
            }

            return writer.ToArray();
        }
    }
}
