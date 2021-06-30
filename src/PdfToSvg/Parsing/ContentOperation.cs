// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal class ContentOperation
    {
        public ContentOperation(string operatorName, params object?[] operands)
        {
            Operator = operatorName;
            Operands = operands;
        }

        public string Operator { get; }
        public object?[] Operands { get; }

        public override string ToString()
        {
            return Operator;
        }
    }
}
