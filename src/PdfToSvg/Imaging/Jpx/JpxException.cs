// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpx
{
    internal class JpxException : Exception
    {
        public JpxException(string message) : base(message)
        {
        }

        public JpxException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
