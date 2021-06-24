// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Functions;
using PdfToSvg.Functions.PostScript;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Functions
{
    public class PostScriptStackTests
    {
        [TestCase("[1 2 3 4 5 6 7]", 5, 2, "[1 2 6 7 3 4 5]")]
        [TestCase("[1 2 3 4 5 6 7]", 5, -2, "[1 2 5 6 7 3 4]")]
        [TestCase("[1 2 3 4 5 6 7]", 5, 7, "[1 2 6 7 3 4 5]")]
        [TestCase("[1 2 3 4 5 6 7]", 5, 0, "[1 2 3 4 5 6 7]")]
        [TestCase("[1 2 3 4 5 6 7]", 5, 5, "[1 2 3 4 5 6 7]")]
        [TestCase("[1 2 3 4 5 6 7]", 5, -5, "[1 2 3 4 5 6 7]")]
        public void Roll(string input, int windowSize, int shiftAmount, string expectedOutput)
        {
            var parsedInput = input
                .Trim('[', ']')
                .Split(' ')
                .Select(n => double.Parse(n, CultureInfo.InvariantCulture))
                .ToArray();

            var stack = new PostScriptStack();

            foreach (var n in parsedInput)
            {
                stack.Push(n);
            }

            stack.Roll(windowSize, shiftAmount);

            var actualOutput = "[" + string.Join(" ", stack
                .ToDoubleArray()
                .Select(n => n.ToString("0", CultureInfo.InvariantCulture))
                ) + "]";

            Assert.AreEqual(expectedOutput, actualOutput);
        }
    }
}
