// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.IO
{
    internal struct VariableBitReaderCursor
    {
        public int Cursor;
        public int BitCursor;

        public override string ToString()
        {
            return Cursor + ":" + BitCursor;
        }
    }
}
