// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    public class PdfPageCollection : ReadOnlyCollection<PdfPage>
    {
        internal PdfPageCollection(IList<PdfPage> list) : base(list)
        {
        }
    }
}
