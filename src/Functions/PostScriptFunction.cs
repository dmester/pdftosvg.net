// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Functions.PostScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Functions
{
    internal class PostScriptFunction : Function
    {
        private readonly double[] domain;
        private readonly double[] range;
        private readonly PostScriptExpression expression;

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

        public override double[] Evaluate(params double[] arguments)
        {
            arguments = ImmutableClip(domain, arguments);

            var stack = new PostScriptStack();

            for (var i = 0; i < arguments.Length; i++)
            {
                stack.Push(arguments[i]);
            }

            expression.Execute(stack);

            if (range.Length != stack.Count * 2)
            {
                throw new PostScriptFunctionException($"The resulting PostScript stack had an unexpected length.");
            }

            var result = stack.ToDoubleArray();
            return Clip(range, result);
        }
    }
}
