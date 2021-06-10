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
    internal class StitchingFunction : Function
    {
        private readonly double[] domain;
        private readonly double[]? range;

        private readonly Function[] functions;
        private readonly double[] bounds;
        private readonly double[] encode;


        public StitchingFunction(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetArray(Names.Domain, out domain!))
            {
                throw new ArgumentException($"Missing {Names.Domain}");
            }

            dictionary.TryGetArray(Names.Range, out range);

            if (!dictionary.TryGetArray(Names.Functions, out var funcDicts))
            {
                throw new ArgumentException($"Missing {Names.Functions}");
            }

            if (!dictionary.TryGetArray(Names.Bounds, out bounds!))
            {
                throw new ArgumentException($"Missing {Names.Bounds}");
            }

            if (!dictionary.TryGetArray(Names.Encode, out encode!))
            {
                throw new ArgumentException($"Missing {Names.Encode}");
            }

            this.functions = new Function[funcDicts.Length];

            for (var i = 0; i < functions.Length; i++)
            {
                functions[i] = Parse(funcDicts[i]);
            }

            this.domain = EnsureArrayLength(this.domain, 2);
            this.bounds = EnsureArrayLength(this.bounds, this.functions.Length - 1);
            this.encode = EnsureArrayLength(this.encode, this.functions.Length * 2);
        }

        public override double[] Evaluate(params double[] arguments)
        {
            arguments = ImmutableClip(domain, arguments);

            var value = arguments.Length < 1 ? 0d : arguments[0];

            var funcIndexMin = 0;
            var funcIndexMax = bounds.Length - 1;

            while (funcIndexMin <= funcIndexMax)
            {
                var middleIndex = (funcIndexMin + funcIndexMax) / 2;
                var middle = bounds[middleIndex];

                if (value < middle)
                {
                    funcIndexMax = middleIndex - 1;
                }
                else
                {
                    funcIndexMin = middleIndex + 1;
                }
            }

            var funcIndex = funcIndexMin;

            var bounds0 = funcIndex == 0 ? domain[0] : bounds[funcIndex - 1];
            var bounds1 = funcIndex < bounds.Length ? bounds[funcIndex] : domain[1];

            var encoded = MathUtils.Interpolate(value, bounds0, bounds1, encode[2 * funcIndex], encode[2 * funcIndex + 1]);
            var output = functions[funcIndex].Evaluate(encoded);

            return Clip(range, output);
        }
    }
}
