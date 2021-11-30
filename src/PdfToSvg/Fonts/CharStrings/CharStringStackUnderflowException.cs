// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharStringStackUnderflowException : CharStringException
    {
        public CharStringStackUnderflowException() : base("Too few values available on the char string stack.")
        {
        }
    }
}
