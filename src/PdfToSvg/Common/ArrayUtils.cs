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
        /// <summary>
        /// Alternative to <see cref="Array.CreateInstance(Type, int)"/> that can be used with Native AOT.
        /// </summary>
        public static Array CreateInstance(Type elementType, int length)
        {
            if (elementType == typeof(float))
            {
                return new float[length];
            }

            if (elementType == typeof(double))
            {
                return new double[length];
            }

            if (elementType == typeof(int))
            {
                return new int[length];
            }

            if (elementType == typeof(bool))
            {
                return new bool[length];
            }

            if (elementType == typeof(string))
            {
                return new string[length];
            }

            if (elementType == typeof(PdfName))
            {
                return new PdfName[length];
            }

            if (elementType == typeof(PdfString))
            {
                return new PdfString[length];
            }

            throw new ArgumentException("Unknown array element type " + elementType.FullName + ".", nameof(elementType));
        }

        public static T[] Add<T>(T[] array, params T[] extraItems)
        {
            var result = new T[array.Length + extraItems.Length];
            Array.Copy(array, result, array.Length);
            Array.Copy(extraItems, 0, result, array.Length, extraItems.Length);
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
