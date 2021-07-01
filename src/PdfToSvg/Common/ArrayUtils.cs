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
