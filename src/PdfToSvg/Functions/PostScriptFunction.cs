// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Functions.PostScript;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PdfToSvg.Functions
{
    /// <threadsafety instance="true" />
    internal class PostScriptFunction : Function
    {
        private readonly double[] domain;
        private readonly double[] range;
        private readonly PostScriptExpression expression;

        private volatile PostScriptStack? pooledStack;

        public override int InputCount { get; }
        public override int OutputCount { get; }


        private readonly PostScriptCompiledExpression? compiledExpression;

        public PostScriptFunction(PdfDictionary dictionary, CancellationToken cancellationToken = default)
            : this(dictionary, enableCompilation: true, cancellationToken)
        {
        }

        public PostScriptFunction(
            PdfDictionary dictionary, bool enableCompilation, CancellationToken cancellationToken = default)
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
                    if (enableCompilation)
                    {
                        compiledExpression = PostScriptCompiler.Compile(expression, domain, range);
                    }
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
            var compiledExpression = this.compiledExpression;

            if (compiledExpression != null)
            {
                compiledExpression.Execute(input, inputOffset, output, outputOffset);
            }
            else
            {
                var stack = RentStack();
                try
                {
                    for (var i = 0; i < InputCount; i++)
                    {
                        var value = (double)input[inputOffset + i];

                        if (i * 2 < domain.Length)
                        {
                            var min = domain[i * 2];
                            if (value <= min)
                            {
                                value = min;
                            }
                            else if (i * 2 + 1 < domain.Length)
                            {
                                var max = domain[i * 2 + 1];
                                if (value >= max)
                                {
                                    value = max;
                                }
                            }
                        }

                        stack.Push(value);
                    }

                    expression.Execute(ref stack);

                    stack.CopyTo(output, outputOffset, range.Length / 2);
                    Clip(range, output, outputOffset);
                }
                finally
                {
                    ReturnStack(stack);
                }
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private PostScriptStack RentStack()
        {
            var stack = Interlocked.Exchange(ref pooledStack, null);
            return stack ?? new PostScriptStack();
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private void ReturnStack(PostScriptStack stack)
        {
            stack.Reset();
            pooledStack = stack;
        }
    }
}
