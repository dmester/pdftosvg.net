// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Functions
{
    internal class ExponentialFunction : Function
    {
        public readonly double[] Domain;
        public readonly double[]? Range;

        public readonly double[] C0;
        public readonly double[] C1;
        public readonly double N;

        public override int InputCount { get; }
        public override int OutputCount { get; }

        public ExponentialFunction(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetArray(Names.Domain, out Domain!))
            {
                throw new ArgumentException($"Missing {Names.Domain}");
            }

            dictionary.TryGetArray(Names.Range, out Range);

            if (!dictionary.TryGetArray(Names.C0, out C0!))
            {
                C0 = new[] { 0d };
            }

            if (!dictionary.TryGetArray(Names.C1, out C1!))
            {
                C1 = new[] { 1d };
            }

            if (!dictionary.TryGetNumber(Names.N, out N))
            {
                N = 1;
            }

            InputCount = Domain.Length / 2;
            OutputCount = Math.Min(C0.Length, C1.Length);
        }

        public override void Evaluate(float[] input, int inputOffset, float[] output, int outputOffset)
        {
            var value = MathUtils.Clamp(input[inputOffset], Domain[0], Domain[1]);
            var raisedValue = Math.Pow(value, N);

            for (var i = 0; i < OutputCount; i++)
            {
                output[outputOffset + i] = (float)(C0[i] + raisedValue * (C1[i] - C0[i]));
            }

            Clip(Range, output, outputOffset);
        }
    }
}
