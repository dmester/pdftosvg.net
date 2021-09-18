// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal static class ArrayUtils
    {
        public static T[] Concat<T>(params T[]?[] arrays)
        {
            var totalLength = 0;

            for (var i = 0; i < arrays.Length; i++)
            {
                var array = arrays[i];
                if (array != null)
                {
                    totalLength += array.Length;
                }
            }

            var result = new T[totalLength];
            var cursor = 0;

            for (var i = 0; i < arrays.Length; i++)
            {
                var array = arrays[i];
                if (array != null)
                {
                    Array.Copy(array, 0, result, cursor, array.Length);
                    cursor += array.Length;
                }
            }

            return result;
        }

#if (NET40 || NET45)
        private class EmptyArrayHolder<T>
        {
            public static readonly T[] Empty = new T[0];
        }

        public static T[] Empty<T>()
        {
            return EmptyArrayHolder<T>.Empty;
        }
#else
        public static T[] Empty<T>()
        {
            return Array.Empty<T>();
        }
#endif
    }
}
