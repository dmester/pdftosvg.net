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
        public static TableFactory Factory => new("post", Read);

        private const uint Version = 0x00010000;

        public PostTableV1()
        {
            GlyphNames = (string[])MacintoshNames.Clone();
        }

        protected override void Write(OpenTypeWriter writer)
        {
            writer.WriteUInt32(Version);
            WriteHeader(writer);
        }

        private static IBaseTable? Read(OpenTypeReader reader)
        {
            var version = reader.ReadUInt32();
            if (version != Version)
            {
                return null;
            }

            var table = new PostTableV1();
            table.ReadHeader(reader);
            return table;
        }
    }
}
