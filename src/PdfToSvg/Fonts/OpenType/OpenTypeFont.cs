// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Fonts.CompactFonts;
using PdfToSvg.Fonts.OpenType.Conversion;
using PdfToSvg.Fonts.OpenType.Tables;
using PdfToSvg.Fonts.OpenType.Utils;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    /// <summary>
    /// Parses .ttf and .otf files.
    /// </summary>
    /// <remarks>
    /// Specification:
    /// https://docs.microsoft.com/en-us/typography/opentype/spec/otff
    /// </remarks>
    internal class OpenTypeFont
    {
        private readonly List<IBaseTable> tables;
        private IList<OpenTypeCMap>? cmaps;

        public OpenTypeFont()
        {
            tables = new List<IBaseTable>();
            Names = new OpenTypeNames(tables);
        }

        public ICollection<IBaseTable> Tables => tables;

        public OpenTypeNames Names { get; }

        public IList<OpenTypeCMap> CMaps
        {
            get
            {
                if (cmaps == null)
                {
                    foreach (var table in tables)
                    {
                        if (table is CMapTable cmap)
                        {
                            cmaps = ReadCMaps(cmap);
                        }
                    }
                }

                return cmaps ?? ArrayUtils.Empty<OpenTypeCMap>();
            }
        }

        public static OpenTypeFont Parse(byte[] data)
        {
            var directory = TableDirectory.Read(data);

            var font = new OpenTypeFont();

            foreach (var table in directory.Tables)
            {
                font.tables.Add(table);
            }

            return font;
        }

        public static OpenTypeFont FromCompactFont(CompactFont font)
        {
            return CffToOtfConverter.Convert(font);
        }

        private static IList<OpenTypeCMap> ReadCMaps(CMapTable table)
        {
            var cmaps = new List<OpenTypeCMap>();

            foreach (var encoding in table.EncodingRecords)
            {
                List<OpenTypeCMapRange> ranges;

                if (encoding.Content is CMapFormat0 f0)
                {
                    ranges = GetCMap(f0);
                }
                else if (encoding.Content is CMapFormat4 f4)
                {
                    ranges = GetCMap(f4);
                }
                else if (encoding.Content is CMapFormat12 f12)
                {
                    ranges = GetCMap(f12);
                }
                else
                {
                    continue;
                }

                cmaps.Add(new OpenTypeCMap(encoding.PlatformID, encoding.EncodingID, ranges));
            }

            return cmaps;
        }

        private static List<OpenTypeCMapRange> GetCMap(CMapFormat0 cmap)
        {
            var ranges = new List<OpenTypeCMapRange>();

            for (var i = 0u; i < cmap.GlyphIdArray.Length; i++)
            {
                var glyphId = cmap.GlyphIdArray[i];
                if (glyphId != 0)
                {
                    ranges.Add(new OpenTypeCMapRange(
                        startUnicode: i,
                        endUnicode: i,
                        startGlyphIndex: glyphId
                        ));
                }
            }

            return ranges;
        }

        private static List<OpenTypeCMapRange> GetCMap(CMapFormat4 cmap)
        {
            var ranges = new List<OpenTypeCMapRange>();

            for (var i = 0; i < cmap.StartCode.Length; i++)
            {
                if (cmap.IdRangeOffsets[i] == 0)
                {
                    ranges.Add(new OpenTypeCMapRange(
                        startUnicode: cmap.StartCode[i],
                        endUnicode: cmap.EndCode[i],
                        startGlyphIndex: (ushort)(cmap.StartCode[i] + cmap.IdDelta[i]) // Modulo 65536
                        ));
                }
                else
                {
                    // TODO Skip for now, implement later if needed
                }
            }

            return ranges;
        }

        private static List<OpenTypeCMapRange> GetCMap(CMapFormat12 cmap)
        {
            var ranges = new List<OpenTypeCMapRange>();

            foreach (var group in cmap.Groups)
            {
                ranges.Add(new OpenTypeCMapRange(
                    startUnicode: group.StartCharCode,
                    endUnicode: group.EndCharCode,
                    startGlyphIndex: group.StartGlyphID
                    ));
            }

            return ranges;
        }

        public byte[] ToByteArray()
        {
            var writer = new OpenTypeWriter();

            var directory = new TableDirectory { Tables = tables.ToArray() };
            directory.Write(writer);

            return writer.ToArray();
        }
    }
}
