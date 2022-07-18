// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    [DebuggerDisplay("maxp")]
    internal abstract class MaxpTable : IBaseTable
    {
        public string Tag => "maxp";

        public ushort NumGlyphs;

        void IBaseTable.Write(OpenTypeWriter writer)
        {
            Write(writer);
        }

        protected abstract void Write(OpenTypeWriter writer);
    }
}
