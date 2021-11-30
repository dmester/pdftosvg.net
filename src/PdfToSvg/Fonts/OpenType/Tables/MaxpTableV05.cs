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
    [DebuggerDisplay("maxp")]
    internal class MaxpTableV05 : IBaseTable
    {
        private const uint Version = 0x00005000;

        public string Tag => "maxp";

        public ushort NumGlyphs;

        void IBaseTable.Write(OpenTypeWriter writer)
        {
            writer.WriteUInt32(Version);
            writer.WriteUInt16(NumGlyphs);
        }

        [OpenTypeTableReader("maxp")]
        public static IBaseTable? Read(OpenTypeReader reader)
        {
            var version = reader.ReadUInt32();
            if (version != Version)
            {
                return null;
            }

            var table = new MaxpTableV05();
            table.NumGlyphs = reader.ReadUInt16();
            return table;
        }
    }
}
