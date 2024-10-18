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
    internal class PostTableV3 : PostTable
    {
        public static TableFactory Factory => new("post", Read);

        private const uint Version = 0x00030000;

        protected override void Write(OpenTypeWriter writer, IList<IBaseTable> _)
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

            var table = new PostTableV3();
            table.ReadHeader(reader);
            return table;
        }
    }
}
