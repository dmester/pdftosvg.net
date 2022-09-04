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
    internal class PostTableV2 : PostTable
    {
        private const int NameMaxLength = 63;
        private const uint Version = 0x00020000;

        protected override void Write(OpenTypeWriter writer)
        {
            var numGlyphs = (ushort)Math.Min(ushort.MaxValue, GlyphNames.Length);

            writer.WriteUInt32(Version);
            WriteHeader(writer);
            writer.WriteUInt16(numGlyphs);

            var nameLookup = new Dictionary<string, ushort>();

            for (var i = (ushort)0; i < MacintoshNames.Length; i++)
            {
                nameLookup[MacintoshNames[i]] = i;
            }

            var stringData = new List<string>();

            for (var i = 0; i < numGlyphs; i++)
            {
                var glyphName = GlyphNames[i] ?? "";

                if (glyphName.Length > NameMaxLength)
                {
                    glyphName = glyphName.Substring(0, NameMaxLength);
                }

                if (!nameLookup.TryGetValue(glyphName, out var glyphIndex))
                {
                    if (string.IsNullOrEmpty(glyphName))
                    {
                        glyphIndex = 0;
                    }
                    else
                    {
                        glyphIndex = (ushort)nameLookup.Count;
                        nameLookup[glyphName] = glyphIndex;
                        stringData.Add(glyphName);
                    }
                }

                writer.WriteUInt16(glyphIndex);
            }

            foreach (var str in stringData)
            {
                writer.WriteUInt8((byte)str.Length);
                writer.WriteAscii(str);
            }
        }

        [OpenTypeTableReader("post")]
        public static IBaseTable? Read(OpenTypeReader reader)
        {
            var version = reader.ReadUInt32();
            if (version != Version)
            {
                return null;
            }

            var table = new PostTableV2();
            table.ReadHeader(reader);

            var numGlyphs = reader.ReadUInt16();

            var glyphNameIndexes = new ushort[numGlyphs];
            for (var i = 0; i < glyphNameIndexes.Length; i++)
            {
                glyphNameIndexes[i] = reader.ReadUInt16();
            }

            var stringData = new List<string>();

            while (reader.Position < reader.Length)
            {
                var stringLength = reader.ReadUInt8();
                var str = reader.ReadAscii(stringLength);
                stringData.Add(str);
            }



            table.GlyphNames = new string[glyphNameIndexes.Length];

            for (var i = 0; i < table.GlyphNames.Length; i++)
            {
                var glyphNameIndex = glyphNameIndexes[i];

                if (glyphNameIndex < MacintoshNames.Length)
                {
                    table.GlyphNames[i] = MacintoshNames[glyphNameIndex];
                    continue;
                }

                glyphNameIndex -= (ushort)MacintoshNames.Length;

                if (glyphNameIndex < stringData.Count)
                {
                    table.GlyphNames[i] = stringData[glyphNameIndex];
                    continue;
                }

                table.GlyphNames[i] = MacintoshNames[0];
            }

            return table;
        }
    }
}
