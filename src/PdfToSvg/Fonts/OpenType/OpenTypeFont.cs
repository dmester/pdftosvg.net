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
                            cmaps = cmap.EncodingRecords
                                .Select(encoding => OpenTypeCMapDecoder.GetCMap(encoding))
                                .WhereNotNull()
                                .ToList();
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

        public byte[] ToByteArray()
        {
            var writer = new OpenTypeWriter();

            var directory = new TableDirectory { Tables = tables.ToArray() };
            directory.Write(writer);

            return writer.ToArray();
        }
    }
}
