// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharStringSubRoutine
    {
        public CharStringSubRoutine(ArraySegment<byte> content) => Content = content;
        public CharStringSubRoutine(byte[] content) => Content = new ArraySegment<byte>(content);

        public ArraySegment<byte> Content { get; }

        public bool Used { get; set; }
    }
}
