// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal class CMapPack
    {
        private readonly byte[] data;
        private readonly Dictionary<string, CMapFile> files;

        public string License { get; }

        public CMapPack(Stream stream)
        {
            var wrapperReader = new BinaryReader(stream, Encoding.ASCII);

            License = wrapperReader.ReadString();
            var fileTableOffset = wrapperReader.ReadInt32();

            using (var uncompressed = new DeflateStream(stream, CompressionMode.Decompress))
            {
                this.data = uncompressed.ToArray();
                files = ReadFileTable(fileTableOffset);
            }
        }

        private BinaryReader GetReader()
        {
            var stream = new MemoryStream(data);
            return new BinaryReader(stream);
        }

        private Dictionary<string, CMapFile> ReadFileTable(int offset)
        {
            var reader = GetReader();
            reader.BaseStream.Position = offset;

            var numFiles = reader.ReadUInt16();
            var files = new Dictionary<string, CMapFile>(numFiles);

            for (int i = 0; i < numFiles; i++)
            {
                var file = new CMapFile();

                file.Name = reader.ReadString();
                file.UseCMap = reader.ReadString();

                if (file.UseCMap == "")
                {
                    file.UseCMap = null;
                }

                file.CodeSpaceRangeOffset = reader.ReadCompactUInt32();
                file.CidTablesOffset = reader.ReadCompactUInt32();

                files[file.Name] = file;
            }

            return files;
        }

        private CMapData ReadCMap(CMapFile file)
        {
            var cmap = new CMapData();
            var reader = GetReader();

            cmap.Name = file.Name;
            cmap.UseCMap = file.UseCMap;
            cmap.IsUnicodeCMap = file.Name.Contains("UCS2") || file.Name.Contains("UTF16");

            reader.BaseStream.Position = file.CodeSpaceRangeOffset;
            ReadCodeSpaceRanges(reader, cmap);

            reader.BaseStream.Position = file.CidTablesOffset;
            var cidTableCount = reader.ReadUInt16();

            var tables = new List<CMapCidTable>(cidTableCount);

            for (var i = 0; i < cidTableCount; i++)
            {
                var table = new CMapCidTable();

                table.Type = (CMapCidTableType)reader.ReadByte();
                table.CharCodeLength = reader.ReadByte();
                table.EntryCount = reader.ReadCompactUInt32();
                table.CharCodeOffset = reader.ReadCompactUInt32();
                table.CidOffset = reader.ReadCompactUInt32();

                tables.Add(table);

            }

            foreach (var table in tables)
            {
                ReadCidTable(reader, table, cmap);
            }

            return cmap;
        }

        private void ReadCodeSpaceRanges(BinaryReader reader, CMapData cmap)
        {
            var count = reader.ReadUInt16();

            for (var i = 0; i < count; i++)
            {
                var charCodeLength = reader.ReadByte();
                var fromCharCode = reader.ReadUInt32();
                var toCharCode = reader.ReadUInt32();

                cmap.CodeSpaceRanges.Add(new CMapCodeSpaceRange(fromCharCode, toCharCode, charCodeLength));
            }
        }

        private void ReadCidTable(BinaryReader reader, CMapCidTable table, CMapData cmap)
        {
            switch (table.Type)
            {
                case CMapCidTableType.CidChars:
                    ReadChars(reader, table, cmap.CidChars);
                    break;

                case CMapCidTableType.CidRanges:
                    ReadRanges(reader, table, cmap.CidRanges);
                    break;

                case CMapCidTableType.NotDefChars:
                    ReadChars(reader, table, cmap.NotDefChars);
                    break;

                case CMapCidTableType.NotDefRanges:
                    ReadRanges(reader, table, cmap.NotDefRanges);
                    break;
            }
        }

        private uint[] ReadCids(BinaryReader reader, uint offset, uint entryCount)
        {
            var cids = new uint[entryCount];

            reader.BaseStream.Position = offset;

            for (var i = 0; i < cids.Length; i++)
            {
                var cidDiff = reader.ReadCompactUInt32();
                cids[i] = (i == 0 ? 0 : cids[i - 1]) + cidDiff;
            }

            return cids;
        }

        private void ReadRanges(BinaryReader reader, CMapCidTable table, List<CMapRange> output)
        {
            var cids = ReadCids(reader, table.CidOffset, table.EntryCount);

            reader.BaseStream.Position = table.CharCodeOffset;

            CMapRange range = default;

            for (var i = 0; i < cids.Length; i++)
            {
                var fromCharCodeDiff = reader.ReadCompactUInt32();
                var toCharCodeDiff = reader.ReadCompactUInt32();

                var fromCharCode = range.ToCharCode + fromCharCodeDiff + 1;
                var toCharCode = fromCharCode + toCharCodeDiff;

                range = new CMapRange(fromCharCode, toCharCode, (int)table.CharCodeLength, cids[i]);
                output.Add(range);
            }
        }

        private void ReadChars(BinaryReader reader, CMapCidTable table, List<CMapChar> output)
        {
            var cids = ReadCids(reader, table.CidOffset, table.EntryCount);

            reader.BaseStream.Position = table.CharCodeOffset;

            CMapChar ch = default;

            for (var i = 0; i < cids.Length; i++)
            {
                var charCodeDiff = reader.ReadCompactUInt32();

                var charCode = ch.CharCode + charCodeDiff + 1;

                ch = new CMapChar(charCode, (int)table.CharCodeLength, cids[i]);
                output.Add(ch);
            }
        }

        public CMapData? GetCMap(string name)
        {
            return files.TryGetValue(name, out var file) ? ReadCMap(file) : null;
        }
    }
}
