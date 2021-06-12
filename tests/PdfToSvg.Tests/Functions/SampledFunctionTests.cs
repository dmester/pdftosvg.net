// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Functions
{
    public class SampledFunctionTests
    {
        [TestCase(0d, 0d)]
        [TestCase(0.25d, 0.5d)]
        [TestCase(0.5d, 1d)]
        [TestCase(0.75d, 0.5d)]
        [TestCase(1d, 0d)]
        public void MissingSamples(double input, double expectedOutput)
        {
            var dict = new PdfDictionary
            {
                { Names.Domain, new object[] { 0d, 1d } },
                { Names.Size, new object[] { 3 } },
                { Names.Encode, new object[] { 0, 2 } },
                { Names.BitsPerSample, 8 },
                { Names.Range, new object[] { 0d, 1d } },
            };

            var samples = new byte[]
            {
                0, 255
            };
            dict.MakeIndirectObject(new PdfObjectId(), new PdfMemoryStream(dict, samples, samples.Length));

            var function = new SampledFunction(dict);

            Assert.That(function.Evaluate(input), Is.EqualTo(new[] { expectedOutput }).Within(0.01d));
        }

        [TestCase(0.000d, /* -> */ 0d)]
        [TestCase(0.001d, /* -> */ 0d)]
        [TestCase(0.500d, /* -> */ 0d)]
        [TestCase(0.750d, /* -> */ 0.5d)]
        [TestCase(0.875d, /* -> */ 0.75d)]
        [TestCase(1.000d, /* -> */ 1d)]
        [TestCase(2.000d, /* -> */ 1d)]
        public void Linear(double input1, params double[] expectedOutput)
        {
            var dict = new PdfDictionary
            {
                { Names.Domain, new object[] { 0d, 1d } },
                { Names.Size, new object[] { 3 } },
                { Names.Encode, new object[] { 0, 2 } },
                { Names.BitsPerSample, 8 },
                { Names.Range, new object[] { 0d, 1d } },
            };

            var samples = new byte[]
            {
                0, 0, 255
            };
            dict.MakeIndirectObject(new PdfObjectId(), new PdfMemoryStream(dict, samples, samples.Length));

            var function = new SampledFunction(dict);

            Assert.That(function.Evaluate(input1), Is.EqualTo(expectedOutput).Within(0.01d));
        }

        [TestCase(0.000d, 0d, /* -> */ 0d / 255, 197d / 255, 1d)]
        [TestCase(0.999d, 0d, /* -> */ 210d / 255, 1d, 1d)]
        [TestCase(1.001d, 0d, /* -> */ 210d / 255, 1d, 1d)]
        [TestCase(0d, 0.499d, /* -> */ 0.5d, 0.5d, 1d)]
        [TestCase(0d, 0.500d, /* -> */ 0.5d, 0.5d, 1d)]
        [TestCase(0d, 0.501d, /* -> */ 0.5d, 0.5d, 1d)]
        [TestCase(1d, 0.501d, /* -> */ 0d, 53d / 255, 152d / 255)]
        [TestCase(0.5d, 0.75d, /* -> */ 368d / 255 / 4, 533d / 255 / 4, 482d / 255 / 4)]
        [TestCase(1.0d, 1.00d, /* -> */ 241d / 255, 98d / 255, 75d / 255)]
        public void Bilinear(double input1, double input2, params double[] expectedOutput)
        {
            var dict = new PdfDictionary
            {
                { Names.Domain, new object[] { 0d, 1d, 0d, 1d } },
                { Names.Size, new object[] { 2, 3 } },
                { Names.Encode, new object[] { 0, 1, 0, 2 } },
                { Names.BitsPerSample, 8 },
                { Names.Decode, new object[] { 0d, 1d, 0d, 1d, 0d, 1d } },
                { Names.Range, new object[] { 0d, 1d, 0d, 1d, 0d, 1d } },
            };

            var samples = new byte[]
            {
                0, 197, 255,   // f(0, 0)
                210, 255, 255, // f(1, 0)

                127, 127, 255, // f(0, 1)
                0, 53, 152,    // f(1, 1)

                0, 255, 0,     // f(0, 2)
                241, 98, 75,   // f(1, 2)
            };
            dict.MakeIndirectObject(new PdfObjectId(), new PdfMemoryStream(dict, samples, samples.Length));

            var function = new SampledFunction(dict);

            Assert.That(function.Evaluate(input1, input2), Is.EqualTo(expectedOutput).Within(0.01d));
        }

        [TestCase(-2d, /* -> */ 0.5d, 0d, 1d)]
        [TestCase(+0d, /* -> */ 0.5d, 0d, 1d)]
        [TestCase(+2d, /* -> */ 0.5d, 0d, 1d)]
        public void SingleValue(double input, params double[] expectedOutput)
        {
            var dict = new PdfDictionary
            {
                { Names.Domain, new object[] { 0d, 1d } },
                { Names.Size, new object[] { 1 } },
                { Names.BitsPerSample, 32 },
                { Names.Decode, new object[] { 0d, 1d, 0d, 1d, 0d, 1d } },
                { Names.Range, new object[] { 0d, 1d, 0d, 1d, 0d, 1d } },
            };

            var samples = new byte[]
            {
                127, 255, 255, 255,
                0, 0, 0, 0,
                255, 255, 255, 255,
            };
            dict.MakeIndirectObject(new PdfObjectId(), new PdfMemoryStream(dict, samples, samples.Length));

            var function = new SampledFunction(dict);

            Assert.That(function.Evaluate(input), Is.EqualTo(expectedOutput).Within(0.01d));
        }
    }
}
