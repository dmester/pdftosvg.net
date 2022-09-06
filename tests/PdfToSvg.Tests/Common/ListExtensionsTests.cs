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

        [Test]
        public void Sort1Key()
        {
            var items = new[]
            {
                new { Id = 4, Key = 4 },
                new { Id = 1, Key = 1 },
                new { Id = 5, Key = 5 },
                new { Id = 3, Key = 3 },
                new { Id = 2, Key = 2 },
            }.ToList();

            items.Sort(x => x.Key);

            Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, items.Select(x => x.Id));
        }

        [Test]
        public void Sort2Keys()
        {
            var items = new[]
            {
                new { Id = 5, Key1 = 3, Key2 = 5 },
                new { Id = 3, Key1 = 2, Key2 = 3 },
                new { Id = 1, Key1 = 1, Key2 = 1 },
                new { Id = 4, Key1 = 2, Key2 = 4 },
                new { Id = 2, Key1 = 1, Key2 = 2 },
            }.ToList();

            items.Sort(x => x.Key1, x => x.Key2);

            Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, items.Select(x => x.Id));
        }

        [Test]
        public void Sort3Keys()
        {
            var items = new[]
            {
                new { Id = 3, Key1 = 1, Key2 = 2, Key3 = 3 },
                new { Id = 2, Key1 = 1, Key2 = 1, Key3 = 2 },
                new { Id = 5, Key1 = 2, Key2 = 3, Key3 = 5 },
                new { Id = 1, Key1 = 1, Key2 = 1, Key3 = 1 },
                new { Id = 4, Key1 = 2, Key2 = 2, Key3 = 4 },
            }.ToList();

            items.Sort(x => x.Key1, x => x.Key2, x => x.Key3);

            Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, items.Select(x => x.Id));
        }
    }
}
