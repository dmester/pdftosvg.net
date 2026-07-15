// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if NETFRAMEWORK || NETSTANDARD
namespace PdfToSvg
{
    internal static class ArraySegmentExtensions
    {
        public static T[] ToArray<T>(this ArraySegment<T> segment)
        {
            if (segment.Array == null)
            {
                throw new InvalidOperationException();
            }
            var array = new T[segment.Count];
            Array.Copy(segment.Array!, segment.Offset, array, 0, segment.Count);
            return array;
        }

        public static void CopyTo<T>(this ArraySegment<T> segment, T[] destination)
        {
            if (segment.Array == null)
            {
                throw new InvalidOperationException();
            }
            Array.Copy(segment.Array, segment.Offset, destination, 0, segment.Count);
        }

        public static void CopyTo<T>(this ArraySegment<T> segment, T[] destination, int destinationIndex)
        {
            if (segment.Array == null)
            {
                throw new InvalidOperationException();
            }
            Array.Copy(segment.Array, segment.Offset, destination, destinationIndex, segment.Count);
        }
    }
}
#endif
