// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Functions
{
    internal static class FunctionExtensions
    {
        public static float[] EvaluateForTest(this Function function, params float[] arguments)
        {
            var output = new float[function.OutputCount];
            function.Evaluate(arguments, 0, output, 0);
            return output;
        }
    }
}
