// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Common
{
    public class ListExtensionsTests
    {
        [TestCase("[1 2 3 4 5 6 7]", 5, 2, "[1 2 6 7 3 4 5]")]
        [TestCase("[1 2 3 4 5 6 7]", 5, -2, "[1 2 5 6 7 3 4]")]
        [TestCase("[1 2 3 4 5 6 7]", 5, 7, "[1 2 6 7 3 4 5]")]
        [TestCase("[1 2 3 4 5 6 7]", 5, 0, "[1 2 3 4 5 6 7]")]
        [TestCase("[1 2 3 4 5 6 7]", 5, 5, "[1 2 3 4 5 6 7]")]
        [TestCase("[1 2 3 4 5 6 7]", 5, -5, "[1 2 3 4 5 6 7]")]
        public void Roll(string input, int windowSize, int shiftAmount, string expectedOutput)
        {
            var stack = input
                .Trim('[', ']')
                .Split(' ')
                .Select(n => double.Parse(n, CultureInfo.InvariantCulture))
                .ToList();

            stack.RollEnd(windowSize, shiftAmount);

            var actualOutput = "[" + string.Join(" ", stack
                .Select(n => n.ToString("0", CultureInfo.InvariantCulture))
                ) + "]";

            Assert.AreEqual(expectedOutput, actualOutput);
        }
    }
}
