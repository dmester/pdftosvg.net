// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CompactFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    internal class CffTable : IBaseTable
    {
        public static TableFactory Factory => new("CFF ", Read);
        public string Tag => "CFF ";

        public CompactFontSet? Content;

        private static IBaseTable? Read(OpenTypeReader reader)
        {
            var binaryCff = reader.ReadBytes(reader.Length - reader.Position);

            try
            {
                var cff = CompactFontParser.Parse(binaryCff, maxFontCount: 1);

                return new CffTable
                {
                    Content = cff,
                };
            }
            catch
            {
                return new RawTable
                {
                    Tag = "CFF ",
                    Content = binaryCff,
                };
            }
        }

        public void Write(OpenTypeWriter writer, IList<IBaseTable> _)
        {
            if (Content != null)
            {
                var data = CompactFontBuilder.Build(Content);
                writer.WriteBytes(data);
            }
        }
    }
}
