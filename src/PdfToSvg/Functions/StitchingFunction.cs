// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Functions
{
    internal class StitchingFunction : Function
    {
        public readonly double[] Domain;
        public readonly double[]? Range;

        public readonly Function[] Functions;
        public readonly double[] Bounds;
        public readonly double[] Encode;


        public StitchingFunction(PdfDictionary dictionary, CancellationToken cancellationToken = default)
        {
            if (!dictionary.TryGetArray(Names.Domain, out Domain!))
            {
                throw new ArgumentException($"Missing {Names.Domain}");
            }

            dictionary.TryGetArray(Names.Range, out Range);

            if (!dictionary.TryGetArray(Names.Functions, out var funcDicts))
            {
                throw new ArgumentException($"Missing {Names.Functions}");
            }

            if (!dictionary.TryGetArray(Names.Bounds, out Bounds!))
            {
                throw new ArgumentException($"Missing {Names.Bounds}");
            }

            if (!dictionary.TryGetArray(Names.Encode, out Encode!))
            {
                throw new ArgumentException($"Missing {Names.Encode}");
            }

            this.Functions = new Function[funcDicts.Length];

            for (var i = 0; i < Functions.Length; i++)
            {
                Functions[i] = Parse(funcDicts[i], cancellationToken);
            }

            this.Domain = EnsureArrayLength(this.Domain, 2);
            this.Bounds = EnsureArrayLength(this.Bounds, this.Functions.Length - 1);
            this.Encode = EnsureArrayLength(this.Encode, this.Functions.Length * 2);
        }

        public override double[] Evaluate(params double[] arguments)
        {
            arguments = ImmutableClip(Domain, arguments);

            var value = arguments.Length < 1 ? 0d : arguments[0];

            var funcIndexMin = 0;
            var funcIndexMax = Bounds.Length - 1;

            while (funcIndexMin <= funcIndexMax)
            {
                var middleIndex = (funcIndexMin + funcIndexMax) / 2;
                var middle = Bounds[middleIndex];

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

            var bounds0 = funcIndex == 0 ? Domain[0] : Bounds[funcIndex - 1];
            var bounds1 = funcIndex < Bounds.Length ? Bounds[funcIndex] : Domain[1];

            var encoded = MathUtils.Interpolate(value, bounds0, bounds1, Encode[2 * funcIndex], Encode[2 * funcIndex + 1]);
            var output = Functions[funcIndex].Evaluate(encoded);

            return Clip(Range, output);
        }
    }
}
