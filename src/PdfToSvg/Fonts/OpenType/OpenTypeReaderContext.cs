// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal class OpenTypeReaderContext
    {
        public OpenTypeReaderContext(string tag, IList<IBaseTable> readTables)
        {
            Tag = tag;
            ReadTables = readTables;
        }

        public string Tag { get; }

        public IList<IBaseTable> ReadTables { get; }
    }
}
