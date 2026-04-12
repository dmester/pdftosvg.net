// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Fonts.OpenType.Glyf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    internal class GlyfTable : IBaseTable
    {
        public static TableFactory Factory => new("glyf", Read);
        public string Tag => "glyf";

        public GlyfRecord[] Glyphs = ArrayUtils.Empty<GlyfRecord>();

        public GlyfStats Stats = new GlyfStats();

        void IBaseTable.Write(OpenTypeWriter writer, IList<IBaseTable> tables)
        {
            var glyphs = Glyphs;

            for (var glyphIndex = 0; glyphIndex < glyphs.Length; glyphIndex++)
            {
                writer.WriteBytes(glyphs[glyphIndex].Data);
            }
        }

        private static IBaseTable? Read(OpenTypeReader reader, OpenTypeReaderContext context)
        {
            var glyfTable = new GlyfTable();

            var offsets = context.ReadTables.Get<LocaTable>()?.Offsets;
            if (offsets != null && offsets.Length > 1)
            {
                var fullGlyphs = GlyfParser.Read(reader, offsets);
                GlyfSanitizer.Sanitize(fullGlyphs, glyfTable.Stats);
                glyfTable.Glyphs = GlyfBuilder.Write(fullGlyphs, reader.Length);
            }

            return glyfTable;
        }
    }

    internal struct GlyfRecord
    {
        public ArraySegment<byte> Data;
        public short XMin;
        public short XMax;
        public short YMin;
        public short YMax;
    }

    internal class GlyfStats
    {
        public ushort MaxPoints;
        public ushort MaxContours;
        public ushort MaxComponentDepth;
        public ushort MaxCompositePoints;
        public ushort MaxCompositeContours;
        public ushort MaxComponentElements;
        public ushort MaxSizeOfInstructions;
    }
}
