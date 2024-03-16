// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    [DebuggerDisplay("{Tag,nq}")]
    internal class RawTable : IBaseTable
    {
        public static TableFactory Factory => new(null, Read);
        private string tag = "    ";

        public string Tag
        {
            get => tag;
            set => tag = value.PadRight(4).Substring(0, 4);
        }

        public byte[]? Content { get; set; }

        void IBaseTable.Write(OpenTypeWriter writer)
        {
            if (Content != null)
            {
                writer.WriteBytes(Content);
            }
        }

        private static IBaseTable? Read(OpenTypeReader reader, OpenTypeReaderContext context)
        {
            var table = new RawTable();

            table.tag = context.Tag;
            table.Content = reader.ReadBytes(reader.Length - reader.Position);

            return table;
        }
    }
}
