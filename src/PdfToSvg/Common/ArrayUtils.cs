// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal static class ArrayUtils
    {
        public static T[] Add<T>(T[] array, params T[] extraItems)
        {
            var result = new T[array.Length + extraItems.Length];
            Array.Copy(array, result, array.Length);
            Array.Copy(extraItems, 0, result, array.Length, extraItems.Length);
            return result;
        }

        public static T[] Remove<T>(T[] array, int index)
        {
            if (index < 0 || index >= array.Length) throw new ArgumentOutOfRangeException(nameof(index));

            var result = new T[array.Length - 1];
            Array.Copy(array, result, index);
            Array.Copy(array, index + 1, result, index, array.Length - index - 1);
            return result;
        }

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

        public static T[] Concat<T>(List<ArraySegment<T>> segments)
        {
            var totalLength = 0;

            for (var i = 0; i < segments.Count; i++)
            {
                totalLength += segments[i].Count;
            }

            var result = new T[totalLength];
            var cursor = 0;

            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Count > 0 && segment.Array != null)
                {
                    Array.Copy(segment.Array, segment.Offset, result, cursor, segment.Count);
                    cursor += segment.Count;
                }
            }

            return result;
        }

        public static bool StartsWith(byte[] data, int offset, int count, byte[] lookFor)
        {
            if (lookFor.Length > count)
            {
                return false;
            }

            for (var i = 0; i < lookFor.Length; i++)
            {
                if (data[offset + i] != lookFor[i])
                {
                    return false;
                }
            }

            return true;
        }

#if (NETFRAMEWORK || NETSTANDARD)
        public static void Fill<T>(T[] array, T value) where T : struct
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }
#else
        public static void Fill<T>(T[] array, T value) where T : struct
        {
            new Span<T>(array).Fill(value);
        }
#endif

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

#if (NETFRAMEWORK || NETSTANDARD)
        private class EmptyArraySegmentHolder<T>
        {
            public static readonly ArraySegment<T> Empty = new ArraySegment<T>(new T[0]);
        }

        public static ArraySegment<T> EmptySegment<T>()
        {
            return EmptyArraySegmentHolder<T>.Empty;
        }
#else
        public static ArraySegment<T> EmptySegment<T>()
        {
            return ArraySegment<T>.Empty;
        }
#endif
    }
}
