// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts
{
    internal class FontException : PdfException
    {
        public FontException(string message, Exception? innerException) : base(message + " " + innerException?.Message, innerException)
        {
        }
    }
}
