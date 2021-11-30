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
    [DebuggerDisplay("name")]
    internal class NameTable : IBaseTable
    {
        public string Tag => "name";

        public ushort Version;
        public NameRecord[] NameRecords = ArrayUtils.Empty<NameRecord>();
        public LangTagRecord[] LangTagRecords = ArrayUtils.Empty<LangTagRecord>();

        void IBaseTable.Write(OpenTypeWriter writer)
        {
            if (NameRecords == null)
            {
                NameRecords = new NameRecord[0];
            }

            var startPos = writer.Position;
            var count = (ushort)NameRecords.Length;

            writer.WriteUInt16(Version);
            writer.WriteUInt16(count);
            writer.WriteField16(out var storageOffsetField);

            var storageCursor = (ushort)0;

            foreach (var name in NameRecords)
            {
                var length = (ushort)name.Content.Length;

                writer.WriteUInt16((ushort)name.PlatformID);
                writer.WriteUInt16(name.EncodingID);
                writer.WriteUInt16(name.LanguageID);
                writer.WriteUInt16((ushort)name.NameID);
                writer.WriteUInt16(length);
                writer.WriteUInt16(storageCursor);

                storageCursor += length;
            }

            if (Version > 0)
            {
                writer.WriteUInt16((ushort)LangTagRecords.Length);

                foreach (var tag in LangTagRecords)
                {
                    var length = (ushort)tag.Content.Length;

                    writer.WriteUInt16(length);
                    writer.WriteUInt16(storageCursor);

                    storageCursor += length;
                }
            }

            storageOffsetField.WriteUInt16((ushort)(writer.Position - startPos));

            foreach (var name in NameRecords)
            {
                writer.WriteBytes(name.Content);
            }

            if (Version > 0)
            {
                foreach (var tag in LangTagRecords)
                {
                    writer.WriteBytes(tag.Content);
                }
            }
        }

        [OpenTypeTableReader("name")]
        public static IBaseTable? Read(OpenTypeReader reader)
        {
            var table = new NameTable();

            table.Version = reader.ReadUInt16();
            var count = reader.ReadUInt16();
            var storageOffset = reader.ReadUInt16();

            table.NameRecords = new NameRecord[count];

            var nameRecords = new RecordHolder<NameRecord>[count];

            for (var i = 0; i < table.NameRecords.Length; i++)
            {
                var holder = nameRecords[i] = new RecordHolder<NameRecord>();
                var record = table.NameRecords[i] = holder.Record;

                record.PlatformID = (OpenTypePlatformID)reader.ReadUInt16();
                record.EncodingID = reader.ReadUInt16();
                record.LanguageID = reader.ReadUInt16();
                record.NameID = (OpenTypeNameID)reader.ReadUInt16();
                holder.Length = reader.ReadUInt16();
                holder.Offset = reader.ReadUInt16();
            }

            var langTagRecords = ArrayUtils.Empty<RecordHolder<LangTagRecord>>();

            if (table.Version > 0)
            {
                var langTagCount = reader.ReadUInt16();

                table.LangTagRecords = new LangTagRecord[langTagCount];
                langTagRecords = new RecordHolder<LangTagRecord>[langTagCount];

                for (var i = 0; i < table.LangTagRecords.Length; i++)
                {
                    var holder = langTagRecords[i] = new RecordHolder<LangTagRecord>();
                    table.LangTagRecords[i] = holder.Record;

                    holder.Length = reader.ReadUInt16();
                    holder.Offset = reader.ReadUInt16();
                }
            }

            foreach (var name in nameRecords)
            {
                reader.Position = storageOffset + name.Offset;
                name.Record.Content = reader.ReadBytes(name.Length);
            }

            foreach (var langTag in langTagRecords)
            {
                reader.Position = storageOffset + langTag.Offset;
                langTag.Record.Content = reader.ReadBytes(langTag.Length);
            }

            return table;
        }
    }

    internal class LangTagRecord
    {
        public byte[] Content = ArrayUtils.Empty<byte>();
    }

    internal class NameRecord
    {
        public OpenTypePlatformID PlatformID;
        public ushort EncodingID;
        public ushort LanguageID;
        public OpenTypeNameID NameID;

        public byte[] Content = ArrayUtils.Empty<byte>();
    }
}
