// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Utils
{
    internal class RecordHolder<T> where T : new()
    {
        public T Record = new T();

        public int Offset;
        public int Length;
    }
}
