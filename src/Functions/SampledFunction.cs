// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Functions
{
    internal class SampledFunction : Function
    {
        public SampledFunction(PdfDictionary dictionary)
        {

        }

        public override double[] Evaluate(params double[] arguments)
        {
            throw new NotImplementedException();
        }
    }
}
