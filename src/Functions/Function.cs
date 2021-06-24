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
    internal abstract class Function
    {
        public static Function Parse(object? functionDefinition)
        {
            if (functionDefinition is PdfDictionary dict &&
                dict.TryGetInteger(Names.FunctionType, out var functionType))
            {
                try
                {
                    switch (functionType)
                    {
                        case 0: return new SampledFunction(dict);
                        case 2: return new ExponentialFunction(dict);
                        case 3: return new StitchingFunction(dict);
                        case 4: return new PostScriptFunction(dict);
                        default:
                            Log.WriteLine("Unknown function type {0}.", functionType);
                            break;
                    }
                }
                catch (ArgumentException ex)
                {
                    Log.WriteLine("Invalid function type {0}. {1}", functionType, ex.Message);
                }
            }
            else
            {
                Log.WriteLine($"Missing {Names.FunctionType}.");
            }

            return new IdentityFunction();
        }

        protected static double[] Clip(double[]? range, double[] values)
        {
            if (range != null)
            {
                for (var i = 0; i < values.Length && i * 2 < range.Length; i++)
                {
                    var min = range[i * 2];
                    if (values[i] <= min)
                    {
                        values[i] = min;
                    }
                    else if (i * 2 + 1 < range.Length)
                    {
                        var max = range[i * 2 + 1];
                        if (values[i] >= max)
                        {
                            values[i] = max;
                        }
                    }
                }
            }

            return values;
        }

        protected static double[] ImmutableClip(double[]? range, double[] values)
        {
            var clipped = (double[])values.Clone();
            return Clip(range, clipped);
        }

        protected double[] EnsureArrayLength(double[] input, int expectedLength)
        {
            if (input.Length != expectedLength)
            {
                var result = new double[expectedLength];

                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = input[Math.Min(input.Length - 1, i)];
                }

                return result;
            }

            return input;
        }

        public abstract double[] Evaluate(params double[] arguments);
    }
}
