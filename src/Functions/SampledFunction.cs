// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Functions
{
    internal class SampledFunction : Function
    {
        // PDF spec 1.7, 7.10.2, page 101
        private readonly double[] domain;
        private readonly double[] range;

        private readonly int[] size;
        private readonly double[] encode;
        private readonly double[] decode;

        private readonly int outputCount;

        private readonly uint[] samples;
        private readonly uint maxSampleValue;

        public SampledFunction(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetArray(Names.Domain, out domain!))
            {
                throw new ArgumentException($"Missing {Names.Domain}");
            }

            if (!dictionary.TryGetArray(Names.Range, out range!))
            {
                throw new ArgumentException($"Missing {Names.Range}");
            }

            outputCount = range.Length / 2;

            if (outputCount < 1)
            {
                throw new ArgumentException($"At least two elements expected in {Names.Range}");
            }

            if (!dictionary.TryGetArray(Names.Size, out size!))
            {
                throw new ArgumentException($"Missing {Names.Size}");
            }
            if (size.Length < 1)
            {
                throw new ArgumentException($"{Names.Size} cannot be empty.");
            }

            if (!dictionary.TryGetInteger(Names.BitsPerSample, out var bitsPerSample))
            {
                throw new ArgumentException($"Missing {Names.BitsPerSample}");
            }

            if (!dictionary.TryGetArray(Names.Encode, out encode!))
            {
                encode = new double[size.Length * 2];

                for (var i = 0; i < size.Length; i++)
                {
                    encode[i * 2 + 1] = size[i] - 1;
                }
            }

            if (!dictionary.TryGetArray(Names.Decode, out decode!))
            {
                decode = range;
            }

            var sampleCount = outputCount;
            for (var i = 0; i < size.Length; i++)
            {
                sampleCount *= size[i];
            }

            samples = new uint[sampleCount];

            if (dictionary.Stream != null)
            {
                using var stream = dictionary.Stream.OpenDecoded();
                using var reader = new BitReader(stream, bitsPerSample, bufferSize: 1024);

                reader.Read(samples, 0, samples.Length);
            }

            // Long is used since 1 << 32 will overflow UInt32
            maxSampleValue = (uint)((1L << bitsPerSample) - 1);

            domain = EnsureArrayLength(domain, size.Length * 2);
            encode = EnsureArrayLength(encode, size.Length * 2);
            decode = EnsureArrayLength(decode, outputCount * 2);
        }

        public override double[] Evaluate(params double[] arguments)
        {
            var input = new double[size.Length];
            var output = new double[outputCount];

            for (var i = 0; i < size.Length && i < arguments.Length; i++)
            {
                var value = arguments[i];

                // Clip to Domain
                value = MathUtils.Clamp(value, domain[i * 2], domain[i * 2 + 1]);

                // Encode
                value = MathUtils.Interpolate(value, domain[i * 2], domain[i * 2 + 1], encode[2 * i], encode[2 * i + 1]);

                // Clip to Size
                value = MathUtils.Clamp(value, 0, size[i] - 1);

                input[i] = value;
            }

            // PDF specification does not say anything about how multilinear interpolation should be implemented.
            //
            // We will implement it according to this paper:
            // https://rjwagner49.com/Mathematics/Interpolation.pdf
            //
            // This is the same algorithm as Pdf.js is using.
            // https://github.com/mozilla/pdf.js/blob/58e568fe624896c80804a1320c22c916864d2513/src/core/function.js#L326
            // 
            // Cubic spline interpolation is not implemented (none of PDF.js, PDFBox and Pdfium seems to have implemented it)

            var sampleCount = 1 << input.Length;
            var sampleIndexes = new int[sampleCount];

            var partitionMultiplier = new double[sampleCount]; // N in the paper
            
            for (var partitionIndex = 0; partitionIndex < partitionMultiplier.Length; partitionIndex++)
            {
                partitionMultiplier[partitionIndex] = 1d;
            }

            var sampleOffset = outputCount;

            for (var inputIndex = 0; inputIndex < input.Length; inputIndex++)
            {
                var xn2 = input[inputIndex];
                var xn0 = (int)xn2;

                if (xn0 >= size[inputIndex] - 1)
                {
                    xn0--;
                }

                var xn1 = xn0 + 1;

                for (var partitionIndex = 0; partitionIndex < partitionMultiplier.Length; partitionIndex++)
                {
                    if (((1 << inputIndex) & partitionIndex) != 0)
                    {
                        partitionMultiplier[partitionIndex] *= xn1 - xn2;
                        sampleIndexes[partitionIndex] += sampleOffset * Math.Max(0, xn0);
                    }
                    else
                    {
                        partitionMultiplier[partitionIndex] *= xn2 - xn0;
                        sampleIndexes[partitionIndex] += sampleOffset * xn1;
                    }
                }

                sampleOffset *= size[inputIndex];
            }

            for (var outputIndex = 0; outputIndex < outputCount; outputIndex++)
            {
                var value = 0d;

                for (var partitionIndex = 0; partitionIndex < partitionMultiplier.Length; partitionIndex++)
                {
                    value += samples[sampleIndexes[partitionIndex] + outputIndex] * partitionMultiplier[partitionIndex];
                }

                // Decode
                value = MathUtils.Interpolate(value, 0, maxSampleValue, decode[outputIndex * 2], decode[outputIndex * 2 + 1]);

                // Clip
                output[outputIndex] = MathUtils.Clamp(value, range[outputIndex * 2], range[outputIndex * 2 + 1]);
            }

            return output;
        }
    }
}
