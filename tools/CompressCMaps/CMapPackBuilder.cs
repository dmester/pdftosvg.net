// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.Common;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressCMaps
{
    internal class CMapPackBuilder
    {
        private class CidTableEx : CMapCidTable
        {
            public byte[]? CharCodeData;
            public byte[]? CidData;
        }

        private class FileEx : CMapFile
        {
            public List<CidTableEx> CidTables = new();
            public List<CMapCodeSpaceRange> CodeSpaceRangesData = new();
        }

        public static byte[] Pack(string license, IEnumerable<CMapData> cmaps)
        {
            var outputStream = new MemoryStream();
            var outputWriter = new BinaryWriter(outputStream, Encoding.ASCII);

            var content = GetContent(cmaps, out var fileDataOffset);

            outputWriter.Write(license);
            outputWriter.Write(fileDataOffset);
            outputWriter.Write(Deflate.Compress(content));

            return outputStream.ToArray();
        }

        private static byte[] GetContent(IEnumerable<CMapData> cmaps, out int fileDataOffset)
        {
            var files = new List<FileEx>();

            foreach (var cmap in cmaps)
            {
                if (cmap.Name == null)
                {
                    throw new ArgumentException("Unnamed CMaps cannot be included in CMap packs.", nameof(cmaps));
                }

                var cidRanges = new List<CMapRange>(cmap.CidRanges.Count);
                var cidChars = new List<CMapChar>(cmap.CidChars.Count);

                CMapOptimizer.Optimize(cidRanges, cidChars, cmap.CidRanges, cmap.CidChars);

                var file = new FileEx();

                file.Name = cmap.Name;
                file.UseCMap = cmap.UseCMap;
                file.CodeSpaceRangesData.AddRange(cmap.CodeSpaceRanges);

                file.CidTables.AddRange(PackTables(CMapCidTableType.CidChars, cidChars));
                file.CidTables.AddRange(PackTables(CMapCidTableType.CidRanges, cidRanges));
                file.CidTables.AddRange(PackTables(CMapCidTableType.NotDefRanges, cmap.NotDefRanges));
                file.CidTables.AddRange(PackTables(CMapCidTableType.NotDefChars, cmap.NotDefChars));

                files.Add(file);
            }

            var blobWriter = new CMapWriter();

            foreach (var file in files)
            {
                file.CodeSpaceRangeOffset = (uint)blobWriter.BaseStream.Position;
                WriteCodeSpaceRanges(blobWriter, file.CodeSpaceRangesData);
            }

            var allCidTables = files
                .SelectMany(x => x.CidTables)
                .OfType<CidTableEx>()
                .OrderBy(x => x.Type)
                .ThenBy(x => x.CharCodeLength)
                .ToList();

            foreach (var table in allCidTables)
            {
                WriteBlob(blobWriter, ref table.CharCodeOffset, table.CharCodeData);
            }

            foreach (var table in allCidTables)
            {
                WriteBlob(blobWriter, ref table.CidOffset, table.CidData);
            }

            foreach (var file in files)
            {
                file.CidTablesOffset = (uint)blobWriter.BaseStream.Position;
                WriteCidTables(blobWriter, file.CidTables);
            }

            fileDataOffset = (int)blobWriter.BaseStream.Position;
            WriteFiles(blobWriter, files);

            return blobWriter.ToArray();
        }

        private static void WriteBlob(CMapWriter blobWriter, ref uint offset, byte[]? blob)
        {
            if (blob != null)
            {
                offset = (uint)blobWriter.BaseStream.Position;
                blobWriter.Write(blob);
            }
        }

        private static void WriteFiles(CMapWriter writer, IList<FileEx> files)
        {
            writer.Write((ushort)files.Count);

            foreach (var file in files)
            {
                writer.Write(file.Name);
                writer.Write(file.UseCMap ?? "");

                writer.WriteCompactUInt32(file.CodeSpaceRangeOffset);
                writer.WriteCompactUInt32(file.CidTablesOffset);
            }
        }

        private static void WriteCidTables(CMapWriter writer, IList<CidTableEx> tables)
        {
            writer.Write((ushort)tables.Count);

            foreach (var table in tables)
            {
                writer.Write((byte)table.Type);
                writer.Write((byte)table.CharCodeLength);
                writer.WriteCompactUInt32(table.EntryCount);
                writer.WriteCompactUInt32(table.CharCodeOffset);
                writer.WriteCompactUInt32(table.CidOffset);
            }
        }

        private static void WriteCodeSpaceRanges(CMapWriter writer, IList<CMapCodeSpaceRange> ranges)
        {
            writer.Write((ushort)ranges.Count);

            foreach (var range in ranges)
            {
                writer.Write((byte)range.CharCodeLength);
                writer.Write(range.FromCharCode);
                writer.Write(range.ToCharCode);
            }
        }

        private static IEnumerable<CidTableEx> PackTables(CMapCidTableType type, IEnumerable<CMapRange> ranges)
        {
            var groups = ranges
                .OrderBy(x => x.CharCodeLength)
                .ThenBy(x => x.FromCharCode)
                .PartitionBy(x => x.CharCodeLength);

            foreach (var group in groups)
            {
                using var charCodes = new CMapWriter();
                using var cids = new CMapWriter();

                CMapRange previous = default;

                foreach (var range in group)
                {
                    var fromCharCodeOffset = range.FromCharCode - previous.ToCharCode - 1;
                    var toCharCodeOffset = range.ToCharCode - range.FromCharCode;
                    var startValueOffset = range.StartValue - previous.StartValue;

                    charCodes.WriteCompactUInt32(fromCharCodeOffset);
                    charCodes.WriteCompactUInt32(toCharCodeOffset);
                    cids.WriteCompactUInt32(startValueOffset);

                    previous = range;
                }

                yield return new CidTableEx
                {
                    Type = type,
                    CharCodeLength = (uint)group.Key,
                    EntryCount = (uint)group.Count(),

                    CharCodeData = charCodes.ToArray(),
                    CidData = cids.ToArray(),
                };
            }
        }

        private static IEnumerable<CidTableEx> PackTables(CMapCidTableType type, IEnumerable<CMapChar> chars)
        {
            foreach (var group in chars
                .OrderBy(x => x.CharCodeLength)
                .ThenBy(x => x.CharCode)
                .PartitionBy(x => x.CharCodeLength))
            {
                using var charCodes = new CMapWriter();
                using var cids = new CMapWriter();

                CMapChar previous = default;

                foreach (var range in group)
                {
                    var charCodeOffset = range.CharCode - previous.CharCode - 1;
                    var cidOffset = range.Cid - previous.Cid;

                    charCodes.WriteCompactUInt32(charCodeOffset);
                    cids.WriteCompactUInt32(cidOffset);

                    previous = range;
                }

                yield return new CidTableEx
                {
                    Type = type,
                    CharCodeLength = (uint)group.Key,
                    EntryCount = (uint)group.Count(),

                    CharCodeData = charCodes.ToArray(),
                    CidData = cids.ToArray(),
                };
            }
        }
    }
}
