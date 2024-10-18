// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Utils
{
    internal static class OptimalTableOrder
    {
        // See: https://docs.microsoft.com/en-us/typography/opentype/spec/recom#optimized-table-ordering

        private static readonly Dictionary<string, int> ttfOrder = new()
        {
            { "head", 0 },
            { "hhea", 1 },
            { "maxp", 2 },
            { "OS/2", 3 },
            { "hmtx", 4 },
            { "LTSH", 5 },
            { "VDMX", 6 },
            { "hdmx", 7 },
            { "cmap", 8 },
            { "fpgm", 9 },
            { "prep", 10 },
            { "cvt ", 11 },
            { "loca", 12 },
            { "glyf", 13 },
            { "kern", 14 },
            { "name", 15 },
            { "post", 16 },
            { "gasp", 17 },
            { "PCLT", 18 },
            { "DSIG", 19 },
        };

        private static readonly Dictionary<string, int> cffOrder = new()
        {
            { "head", 0 },
            { "hhea", 1 },
            { "maxp", 2 },
            { "OS/2", 3 },
            { "name", 4 },
            { "cmap", 5 },
            { "post", 6 },
            { "CFF ", 7 },
        };

        private static readonly Dictionary<string, int> readOrder = new()
        {
            { "head", 0 },
            { "hhea", 1 },
            { "hmtx", 2 }, // Has dependencies to hhea
            { "loca", 3 }, // Has dependencies to head
        };

        private class StorageComparerImpl<T> : IComparer<T>
        {
            private readonly Func<T, string?> tagSelector;
            private readonly Dictionary<string, int> order;

            public StorageComparerImpl(Func<T, string?> tagSelector, Dictionary<string, int> order)
            {
                this.order = order;
                this.tagSelector = tagSelector;
            }

            public int Compare(T? x, T? y)
            {
                var tagx = x is null ? null : tagSelector(x);
                var tagy = y is null ? null : tagSelector(y);

                if (tagx == null)
                {
                    return tagy == null ? 0 : 1;
                }

                if (tagy == null)
                {
                    return -1;
                }

                if (!order.TryGetValue(tagx, out var orderx))
                {
                    orderx = order.Count;
                }

                if (!order.TryGetValue(tagy, out var ordery))
                {
                    ordery = order.Count;
                }

                if (orderx != ordery)
                {
                    return orderx < ordery ? -1 : 1;
                }

                return StringComparer.Ordinal.Compare(tagx, tagy);
            }
        }

        private class DirectoryComparerImpl<T> : IComparer<T>
        {
            private readonly Func<T, string?> tagSelector;

            public DirectoryComparerImpl(Func<T, string?> tagSelector)
            {
                this.tagSelector = tagSelector;
            }

            public int Compare(T? x, T? y)
            {
                var tagx = x is null ? null : tagSelector(x);
                var tagy = y is null ? null : tagSelector(y);
                return StringComparer.Ordinal.Compare(tagx, tagy);
            }
        }

        public static IComparer<T> StorageComparer<T>(Func<T, string?> tagSelector, bool isCff)
        {
            return new StorageComparerImpl<T>(tagSelector, isCff ? cffOrder : ttfOrder);
        }

        public static IComparer<T> DirectoryComparer<T>(Func<T, string?> tagSelector)
        {
            return new DirectoryComparerImpl<T>(tagSelector);
        }

        public static IComparer<T> ReadComparer<T>(Func<T, string?> tagSelector)
        {
            return new StorageComparerImpl<T>(tagSelector, readOrder);
        }

        public static void StorageSort<T>(T[] array, Func<T, string?> tagSelector, bool isCff)
        {
            Array.Sort(array, StorageComparer(tagSelector, isCff));
        }

        public static void DirectorySort<T>(T[] array, Func<T, string?> tagSelector)
        {
            Array.Sort(array, DirectoryComparer(tagSelector));
        }

        public static void ReadSort<T>(T[] array, Func<T, string?> tagSelector)
        {
            Array.Sort(array, ReadComparer(tagSelector));
        }

        public static void StorageSort<T>(List<T> list, Func<T, string?> tagSelector, bool isCff)
        {
            list.Sort(StorageComparer(tagSelector, isCff));
        }

        public static void DirectorySort<T>(List<T> list, Func<T, string?> tagSelector)
        {
            list.Sort(DirectoryComparer(tagSelector));
        }

        public static void ReadSort<T>(List<T> list, Func<T, string?> tagSelector)
        {
            list.Sort(ReadComparer(tagSelector));
        }

    }
}
