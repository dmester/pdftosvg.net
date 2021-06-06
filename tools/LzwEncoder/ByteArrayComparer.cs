// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LzwEncoder
{
    internal class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            if (x == null) return y == null;
            if (y == null) return false;
            if (x.Length != y.Length) return false;

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i]) return false;
            }

            return true;
        }

        public int GetHashCode([DisallowNull] byte[] obj)
        {
            var hash = 0;

            for (var i = 0; i < obj.Length; i++)
            {
                hash = ((hash << 5) | (hash >> 27)) ^ obj[i];
            }

            return hash;
        }
    }
}
