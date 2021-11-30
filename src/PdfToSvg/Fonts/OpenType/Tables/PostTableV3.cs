// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    [DebuggerDisplay("post")]
    internal class PostTableV3 : IBaseTable
    {
        private const uint Version = 0x00030000;

        public string Tag => "post";

        public decimal ItalicAngle;
        public short UnderlinePosition;
        public short UnderlineThickness;
        public uint IsFixedPitch;
        public uint MinMemType42;
        public uint MaxMemType42;
        public uint MinMemType1;
        public uint MaxMemType1;

        void IBaseTable.Write(OpenTypeWriter writer)
        {
            writer.WriteUInt32(Version);
            writer.WriteFixed(ItalicAngle);
            writer.WriteInt16(UnderlinePosition);
            writer.WriteInt16(UnderlineThickness);
            writer.WriteUInt32(IsFixedPitch);
            writer.WriteUInt32(MinMemType42);
            writer.WriteUInt32(MaxMemType42);
            writer.WriteUInt32(MinMemType1);
            writer.WriteUInt32(MaxMemType1);
        }

        [OpenTypeTableReader("post")]
        public static IBaseTable? Read(OpenTypeReader reader)
        {
            var table = new PostTableV3();

            var version = reader.ReadUInt32();
            if (version != Version)
            {
                return null;
            }

            table.ItalicAngle = reader.ReadFixed();
            table.UnderlinePosition = reader.ReadInt16();
            table.UnderlineThickness = reader.ReadInt16();
            table.IsFixedPitch = reader.ReadUInt32();
            table.MinMemType42 = reader.ReadUInt32();
            table.MaxMemType42 = reader.ReadUInt32();
            table.MinMemType1 = reader.ReadUInt32();
            table.MaxMemType1 = reader.ReadUInt32();

            return table;
        }
    }
}
