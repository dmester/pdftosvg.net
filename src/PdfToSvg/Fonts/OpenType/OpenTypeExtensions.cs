// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal static class OpenTypeExtensions
    {
        public static T GetOrCreate<T>(this ICollection<IBaseTable> tables) where T : IBaseTable, new()
        {
            var table = tables.Get<T>();
            if (table == null)
            {
                table = new T();
                tables.Add(table);
            }

            return table;
        }

        public static T Get<T>(this ICollection<IBaseTable> tables) where T : IBaseTable
        {
            return tables.OfType<T>().FirstOrDefault();
        }

        public static bool Remove<T>(this ICollection<IBaseTable> tables) where T : IBaseTable
        {
            var table = tables.Get<T>();
            if (table != null)
            {
                tables.Remove(table);
                return true;
            }

            return false;
        }

        public static IEnumerable<OpenTypeCMap> OrderByPriority(this IEnumerable<OpenTypeCMap> source)
        {
            return source
                .OrderBy(cmap =>
                {
                    if (cmap.PlatformID == OpenTypePlatformID.Windows)
                    {
                        if (cmap.EncodingID == 10)
                        {
                            // Unicode full repertoire
                            return 1;
                        }

                        if (cmap.EncodingID == 1)
                        {
                            // Unicode BMP
                            return 3;
                        }

                        return 5;
                    }

                    if (cmap.PlatformID == OpenTypePlatformID.Unicode)
                    {
                        if (cmap.EncodingID == 4 || cmap.EncodingID == 6)
                        {
                            // Unicode full repertoire
                            return 2;
                        }

                        return 4;
                    }

                    if (cmap.PlatformID == OpenTypePlatformID.Macintosh)
                    {
                        if (cmap.EncodingID == 0)
                        {
                            // 7-bit ASCII
                            return 7;
                        }
                        else
                        {
                            return 6;
                        }
                    }

                    return 8;
                })

                // For deterministic order
                .ThenBy(x => x.PlatformID)
                .ThenBy(x => x.EncodingID);
        }
    }
}
