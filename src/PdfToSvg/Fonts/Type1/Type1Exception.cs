// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.Type1
{
    internal class Type1Exception : FontException
    {
        public Type1Exception(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
