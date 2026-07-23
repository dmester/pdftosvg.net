// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Functions
{
    internal class IdentityFunction : Function
    {
        public override int InputCount { get; } = 1;
        public override int OutputCount { get; } = 1;

        public override void Evaluate(float[] input, int inputOffset, float[] output, int outputOffset)
        {
            output[outputOffset] = input[inputOffset];
        }
    }
}
