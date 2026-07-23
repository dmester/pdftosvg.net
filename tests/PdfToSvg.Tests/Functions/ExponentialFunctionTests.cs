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
    public class ExponentialFunctionTests
    {
        [Test]
        public void NonClippedInput()
        {
            var function = new ExponentialFunction(new PdfDictionary
            {
                { Names.Domain, new object[]{ 0d, 1d } },
                { Names.C0, new object[]{ -1, 5 } },
                { Names.C1, new object[]{ 1, 6 } },
                { Names.N, 2d },
            });

            Assert.AreEqual(new[] { -0.5f, 5.25f }, function.EvaluateForTest(0.5f));
        }

        [Test]
        public void ClipInput()
        {
            var function = new ExponentialFunction(new PdfDictionary
            {
                { Names.Domain, new object[]{ 0.1, 0.8 } },
                { Names.C0, new object[]{ 1, 1 } },
                { Names.C1, new object[]{ 2, 2 } },
                { Names.N, 1 },
            });

            Assert.AreEqual(new[] { 1.8f, 1.8f }, function.EvaluateForTest(1));
            Assert.AreEqual(new[] { 1.5f, 1.5f }, function.EvaluateForTest(0.5f));
            Assert.AreEqual(new[] { 1.1f, 1.1f }, function.EvaluateForTest(0));
        }

        [Test]
        public void ClipOutput()
        {
            var function = new ExponentialFunction(new PdfDictionary
            {
                { Names.Domain, new object[]{ 0d, 1d } },
                { Names.Range, new object[]{ 1.1, 1.8 } },
                { Names.C0, new object[]{ 1 } },
                { Names.C1, new object[]{ 2 } },
                { Names.N, 1 },
            });

            Assert.AreEqual(new[] { 1.1f }, function.EvaluateForTest(0));
            Assert.AreEqual(new[] { 1.5f }, function.EvaluateForTest(0.5f));
            Assert.AreEqual(new[] { 1.8f }, function.EvaluateForTest(1));
        }
    }
}
