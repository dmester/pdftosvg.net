// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Functions.PostScript;
using System;
using System.Threading;

namespace PdfToSvg.Functions
{
    internal class PostScriptFunction : Function
    {
        private readonly double[] domain;
        private readonly double[] range;
        private readonly PostScriptExpression expression;

        public override int InputCount { get; }
        public override int OutputCount { get; }

        public PostScriptFunction(PdfDictionary dictionary, CancellationToken cancellationToken = default)
        {
            if (!dictionary.TryGetArray(Names.Domain, out domain!))
            {
                throw new ArgumentException($"Missing {Names.Domain}");
            }

            if (!dictionary.TryGetArray(Names.Range, out range!))
            {
                throw new ArgumentException($"Missing {Names.Range}");
            }

            InputCount = domain.Length / 2;
            OutputCount = range.Length / 2;

            if (dictionary.Stream != null)
            {
                using var stream = dictionary.Stream.OpenDecoded(cancellationToken);

                try
                {
                    expression = PostScriptParser.Parse(stream);
                }
                catch (PostScriptFunctionException ex)
                {
                    throw new ArgumentException("Invalid PostScript function.", ex);
                }
            }
            else
            {
                throw new ArgumentException($"Missing PostScript function implementation.");
            }
        }

        public override void Evaluate(float[] input, int inputOffset, float[] output, int outputOffset)
        {
            var arguments = ImmutableClip(domain, input, inputOffset, InputCount);

            var stack = new PostScriptStack();

            for (var i = 0; i < arguments.Length; i++)
            {
                stack.Push((double)arguments[i]);
            }

            expression.Execute(stack);

            if (range.Length != stack.Count * 2)
            {
                throw new PostScriptFunctionException(
                    $"Expected a resulting PostScript stack of {range.Length / 2} elements but found {stack.Count}.");
            }

            stack.CopyTo(output, outputOffset, OutputCount);
            Clip(range, output, outputOffset);
        }
    }
}
