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
    internal static class ArrayExtensions
    {
        public static T[] Slice<T>(this T[] source, int offset, int length)
        {
            var result = new T[length];
            Array.Copy(source, offset, result, 0, length);
            return result;
        }

        public static int IndexOf(this byte[] stack, byte[] needle)
        {
            return IndexOf(stack, needle, 0, stack.Length);
        }

        public static int IndexOf(this byte[] stack, byte[] needle, int startIndex, int length)
        {
            if (stack == null) throw new ArgumentNullException(nameof(stack));
            if (needle == null) throw new ArgumentNullException(nameof(needle));
            if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (length < 0 || startIndex + length > stack.Length) throw new ArgumentOutOfRangeException(nameof(length));

            var searchLength = length - needle.Length;

            for (var i = 0; i <= searchLength; i++)
            {
                for (var j = 0; j < needle.Length; j++)
                {
                    if (needle[j] != stack[startIndex + i + j])
                    {
                        goto NoMatch;
                    }
                }
                return startIndex + i;
            NoMatch:;
            }

            return -1;
        }
    }
}
