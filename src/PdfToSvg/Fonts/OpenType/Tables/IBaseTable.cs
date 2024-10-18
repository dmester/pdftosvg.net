// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    internal interface IBaseTable
    {
        string Tag { get; }

        void Write(OpenTypeWriter writer, IList<IBaseTable> tables);
    }
}
