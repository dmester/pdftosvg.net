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
    internal class ExponentialFunction : Function
    {
        private readonly double[] domain;
        private readonly double[]? range;

        private readonly double[] c0;
        private readonly double[] c1;
        private readonly int outputCount;
        private readonly double n;

        public ExponentialFunction(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetArray(Names.Domain, out domain!))
            {
                throw new ArgumentException($"Missing {Names.Domain}");
            }

            dictionary.TryGetArray(Names.Range, out range);

            if (!dictionary.TryGetArray(Names.C0, out c0!))
            {
                c0 = new[] { 0d };
            }

            if (!dictionary.TryGetArray(Names.C1, out c1!))
            {
                c1 = new[] { 1d };
            }

            if (!dictionary.TryGetNumber(Names.N, out n))
            {
                n = 1;
            }

            outputCount = Math.Min(c0.Length, c1.Length);
        }

        public override double[] Evaluate(params double[] arguments)
        {
            arguments = ImmutableClip(domain, arguments);

            var value = arguments.Length < 1 ? 0d : arguments[0];
            var raisedValue = Math.Pow(value, n);

            var output = new double[outputCount];

            for (var i = 0; i < output.Length; i++)
            {
                output[i] = c0[i] + raisedValue * (c1[i] - c0[i]);
            }

            return Clip(range, output);
        }
    }
}
