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
    internal class PostTableV1 : PostTable
    {
        private const uint Version = 0x00010000;

        protected override void Write(OpenTypeWriter writer)
        {
            writer.WriteUInt32(Version);
            WriteHeader(writer);
        }

        [OpenTypeTableReader("post")]
        public static IBaseTable? Read(OpenTypeReader reader)
        {
            var version = reader.ReadUInt32();
            if (version != Version)
            {
                return null;
            }

            var table = new PostTableV1();

            table.ReadHeader(reader);
            table.GlyphNames = (string[])MacintoshNames.Clone();

            return table;
        }
    }
}
