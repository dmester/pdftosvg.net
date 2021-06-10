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

            Assert.AreEqual(new[] { -0.5d, 5.25d }, function.Evaluate(0.5d));
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

            Assert.AreEqual(new[] { 1.8d, 1.8d }, function.Evaluate(1));
            Assert.AreEqual(new[] { 1.5d, 1.5d }, function.Evaluate(0.5));
            Assert.AreEqual(new[] { 1.1d, 1.1d }, function.Evaluate(0));
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

            Assert.AreEqual(new[] { 1.1d }, function.Evaluate(0));
            Assert.AreEqual(new[] { 1.5d }, function.Evaluate(0.5));
            Assert.AreEqual(new[] { 1.8d }, function.Evaluate(1));
        }
    }
}
