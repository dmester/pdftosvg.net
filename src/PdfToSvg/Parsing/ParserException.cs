﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal class ParserException : PdfException
    {
        public ParserException(string message, long position) : base(message)
        {
            Position = position;
        }

        public long Position { get; }
    }
}
