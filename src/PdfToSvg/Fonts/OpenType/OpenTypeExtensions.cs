// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

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
    }
}
