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
    internal class PostTableV25 : PostTable
    {
        public static TableFactory Factory => new("post", Read);

        private const uint Version = 0x00025000;

        protected override void Write(OpenTypeWriter writer)
        {
            // This format is deprecated => convert to format 2

            var postTableV2 = new PostTableV2();

            postTableV2.ItalicAngle = ItalicAngle;
            postTableV2.UnderlinePosition = UnderlinePosition;
            postTableV2.UnderlineThickness = UnderlineThickness;
            postTableV2.IsFixedPitch = IsFixedPitch;
            postTableV2.MinMemType42 = MinMemType42;
            postTableV2.MaxMemType42 = MaxMemType42;
            postTableV2.MinMemType1 = MinMemType1;
            postTableV2.MaxMemType1 = MaxMemType1;
            postTableV2.GlyphNames = GlyphNames;

            ((IBaseTable)postTableV2).Write(writer);
        }

        private static IBaseTable? Read(OpenTypeReader reader)
        {
            var version = reader.ReadUInt32();
            if (version != Version)
            {
                return null;
            }

            var table = new PostTableV25();

            table.ReadHeader(reader);

            var numGlyphs = reader.ReadUInt16();

            var glyphNames = new string[numGlyphs];
            for (var i = 0; i < glyphNames.Length; i++)
            {
                var offset = reader.ReadInt8();
                var glyphIndex = i + offset;

                glyphNames[i] =
                    glyphIndex > 0 && glyphIndex < MacintoshNames.Length
                        ? MacintoshNames[glyphIndex]
                        : MacintoshNames[0];
            }

            table.GlyphNames = glyphNames;

            return table;
        }
    }
}
