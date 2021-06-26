// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal class UnsupportedFilter : Filter
    {
        private readonly PdfName? filterName;

        public UnsupportedFilter(PdfName? filterName)
        {
            this.filterName = filterName;
        }

        public override Stream Decode(Stream encodedStream, PdfDictionary? decodeParms)
        {
            throw new NotSupportedException($"Filter '{filterName}' is not supported.");
        }

        public override string ToString()
        {
            return "Unsupported filter: " + filterName;
        }
    }
}
