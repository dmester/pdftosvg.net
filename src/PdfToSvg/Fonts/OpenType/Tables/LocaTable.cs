// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CompactFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    internal class LocaTable : IBaseTable
    {
        public static TableFactory Factory => new("loca", Read);
        public string Tag => "loca";

        public uint[] Offsets = ArrayUtils.Empty<uint>();

        public void Write(OpenTypeWriter writer, IList<IBaseTable> tables)
        {
            var headTable = tables.Get<HeadTable>();
            var indexToLocFormat = headTable == null ? 0 : headTable.IndexToLocFormat;

            if (indexToLocFormat == 0)
            {
                foreach (var offset in Offsets)
                {
                    writer.WriteUInt16((ushort)(offset / 2));
                }
            }
            else
            {
                foreach (var offset in Offsets)
                {
                    writer.WriteUInt32(offset);
                }
            }
        }

        private static IBaseTable? Read(OpenTypeReader reader, OpenTypeReaderContext context)
        {
            var headTable = context.ReadTables.Get<HeadTable>();
            var indexToLocFormat = headTable == null ? 0 : headTable.IndexToLocFormat;

            var offsets = new List<uint>();

            if (indexToLocFormat == 0)
            {
                while (reader.Position + 2 <= reader.Length)
                {
                    var offset = reader.ReadUInt16();
                    offsets.Add(offset * 2u);
                }
            }
            else
            {
                while (reader.Position + 4 <= reader.Length)
                {
                    var offset = reader.ReadUInt32();
                    offsets.Add(offset);
                }
            }

            return new LocaTable { Offsets = offsets.ToArray() };
        }
    }
}
