// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Fonts.OpenType.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    [DebuggerDisplay("head")]
    internal class HeadTable : IBaseTable
    {
        public string Tag => "head";

        public ushort MajorVersion = 1;
        public ushort MinorVersion = 0;
        public decimal FontRevision;
        public ushort Flags;
        public ushort UnitsPerEm;
        public DateTime Created;
        public DateTime Modified;
        public short MinX;
        public short MinY;
        public short MaxX;
        public short MaxY;
        public MacStyle MacStyle;
        public ushort LowestRecPPEM;
        public short FontDirectionHint = 2;
        public short IndexToLocFormat;
        public short GlyphDataFormat;

        void IBaseTable.Write(OpenTypeWriter writer)
        {
            writer.WriteUInt16(MajorVersion);
            writer.WriteUInt16(MinorVersion);
            writer.WriteFixed(FontRevision);
            writer.WriteUInt32(0);
            writer.WriteUInt32(0x5F0F3CF5); // magicNumber
            writer.WriteUInt16(Flags);
            writer.WriteUInt16(UnitsPerEm);
            writer.WriteDateTime(Created);
            writer.WriteDateTime(Modified);
            writer.WriteInt16(MinX);
            writer.WriteInt16(MinY);
            writer.WriteInt16(MaxX);
            writer.WriteInt16(MaxY);
            writer.WriteUInt16((ushort)MacStyle);
            writer.WriteUInt16(LowestRecPPEM);
            writer.WriteInt16(FontDirectionHint);
            writer.WriteInt16(IndexToLocFormat);
            writer.WriteInt16(GlyphDataFormat);
        }

        [OpenTypeTableReader("head")]
        public static IBaseTable? Read(OpenTypeReader reader)
        {
            var table = new HeadTable();

            table.MajorVersion = reader.ReadUInt16();
            table.MinorVersion = reader.ReadUInt16();
            table.FontRevision = reader.ReadFixed();
            reader.ReadUInt32(); // ChecksumAdjustment
            reader.ReadUInt32(); // MagicNumber
            table.Flags = reader.ReadUInt16();
            table.UnitsPerEm = reader.ReadUInt16();
            table.Created = reader.ReadDateTime();
            table.Modified = reader.ReadDateTime();
            table.MinX = reader.ReadInt16();
            table.MinY = reader.ReadInt16();
            table.MaxX = reader.ReadInt16();
            table.MaxY = reader.ReadInt16();
            table.MacStyle = (MacStyle)reader.ReadUInt16();
            table.LowestRecPPEM = reader.ReadUInt16();
            table.FontDirectionHint = reader.ReadInt16();
            table.IndexToLocFormat = reader.ReadInt16();
            table.GlyphDataFormat = reader.ReadInt16();

            return table;
        }
    }
}
