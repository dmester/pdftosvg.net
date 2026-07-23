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

        public override int InputCount { get; }
        public override int OutputCount { get; }

        public StitchingFunction(PdfDictionary dictionary, int depth = 0, CancellationToken cancellationToken = default)
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
                Functions[i] = Parse(funcDicts[i], depth + 1, cancellationToken);
            }

            this.Domain = EnsureArrayLength(this.Domain, 2);
            this.Bounds = EnsureArrayLength(this.Bounds, this.Functions.Length - 1);
            this.Encode = EnsureArrayLength(this.Encode, this.Functions.Length * 2);

            InputCount = this.Functions[0].InputCount;
            OutputCount = this.Functions[0].OutputCount;
        }

        public override void Evaluate(float[] input, int inputOffset, float[] output, int outputOffset)
        {
            var arguments = ImmutableClip(Domain, input, inputOffset, InputCount);

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

            var funcInput = new float[1] { (float)encoded };
            Functions[funcIndex].Evaluate(funcInput, 0, output, outputOffset);

            Clip(Range, output, outputOffset);
        }
    }
}
