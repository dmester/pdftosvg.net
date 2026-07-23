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
        [TestCase(0f, 0f)]
        [TestCase(0.25f, 0.5f)]
        [TestCase(0.5f, 1f)]
        [TestCase(0.75f, 0.5f)]
        [TestCase(1f, 0f)]
        public void MissingSamples(float input, float expectedOutput)
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

            Assert.That(function.EvaluateForTest(input), Is.EqualTo(new[] { expectedOutput }).Within(0.01d));
        }

        [TestCase(0.000f, /* -> */ 0f)]
        [TestCase(0.001f, /* -> */ 0f)]
        [TestCase(0.500f, /* -> */ 0f)]
        [TestCase(0.750f, /* -> */ 0.5f)]
        [TestCase(0.875f, /* -> */ 0.75f)]
        [TestCase(1.000f, /* -> */ 1f)]
        [TestCase(2.000f, /* -> */ 1f)]
        public void Linear(float input1, params float[] expectedOutput)
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

            Assert.That(function.EvaluateForTest(input1), Is.EqualTo(expectedOutput).Within(0.01d));
        }

        [TestCase(0.000f, 0f, /* -> */ 0f / 255, 197f / 255, 1f)]
        [TestCase(0.999f, 0f, /* -> */ 210f / 255, 1f, 1f)]
        [TestCase(1.001f, 0f, /* -> */ 210f / 255, 1f, 1f)]
        [TestCase(0f, 0.499f, /* -> */ 0.5f, 0.5f, 1f)]
        [TestCase(0f, 0.500f, /* -> */ 0.5f, 0.5f, 1f)]
        [TestCase(0f, 0.501f, /* -> */ 0.5f, 0.5f, 1f)]
        [TestCase(1f, 0.501f, /* -> */ 0f, 53f / 255, 152f / 255)]
        [TestCase(0.5f, 0.75f, /* -> */ 368f / 255 / 4, 533f / 255 / 4, 482f / 255 / 4)]
        [TestCase(1.0f, 1.00f, /* -> */ 241f / 255, 98f / 255, 75f / 255)]
        public void Bilinear(float input1, float input2, params float[] expectedOutput)
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

            Assert.That(function.EvaluateForTest(input1, input2), Is.EqualTo(expectedOutput).Within(0.01d));
        }

        [TestCase(-2f, /* -> */ 0.5f, 0f, 1f)]
        [TestCase(+0f, /* -> */ 0.5f, 0f, 1f)]
        [TestCase(+2f, /* -> */ 0.5f, 0f, 1f)]
        public void SingleValue(float input, params float[] expectedOutput)
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

            Assert.That(function.EvaluateForTest(input), Is.EqualTo(expectedOutput).Within(0.01d));
        }
    }
}
