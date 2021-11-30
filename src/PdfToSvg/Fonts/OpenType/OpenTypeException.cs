// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal class OpenTypeException : PdfException
    {
        public OpenTypeException(string message) : base(message)
        {
        }
    }
}
