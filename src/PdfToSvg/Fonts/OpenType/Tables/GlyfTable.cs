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
    internal class GlyfTable : IBaseTable
    {
        public static TableFactory Factory => new("glyf", Read);
        public string Tag => "glyf";

        public byte[] Content = ArrayUtils.Empty<byte>();

        void IBaseTable.Write(OpenTypeWriter writer, IList<IBaseTable> _)
        {
            writer.WriteBytes(Content);
        }

        private static IBaseTable? Read(OpenTypeReader reader, OpenTypeReaderContext context)
        {
            return new GlyfTable
            {
                Content = reader.ReadBytes(reader.Length - reader.Position),
            };
        }
    }
}
