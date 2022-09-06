// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Common
{
    public class EnumerableExtensionsTests
    {
        [Test]
        public void PartitionBy_Many()
        {
            var source = new[]
            {
                new { Id = 1, Group = "A" },
                new { Id = 2, Group = "A" },
                new { Id = 3, Group = "B" },
                new { Id = 4, Group = "A" },
                new { Id = 5, Group = "A" },
                new { Id = 6, Group = "C" },
                new { Id = 7, Group = "C" },
                new { Id = 8, Group = "C" },
            };

            var actual = source
                .PartitionBy(x => x.Group)
                .Select(x => x.Key + ": " + string.Join(", ", x.Select(y => y.Id)))
                .ToArray();

            var expected = new[]
            {
                "A: 1, 2",
                "B: 3",
                "A: 4, 5",
                "C: 6, 7, 8",
            };

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void PartitionBy_Single()
        {
            var source = new[]
            {
                new { Id = 1, Group = "A" },
            };

            var actual = source
                .PartitionBy(x => x.Group)
                .Select(x => x.Key + ": " + string.Join(", ", x.Select(y => y.Id)))
                .ToArray();

            var expected = new[]
            {
                "A: 1"
            };

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void PartitionBy_Empty()
        {
            var actual = Enumerable
                .Empty<string>()
                .PartitionBy(x => x.Length)
                .ToArray();

            Assert.AreEqual(new IGrouping<int, string>[0], actual);
        }

        [Test]
        public void DistinctBy()
        {
            var items = new[]
            {
                new { Id = 1, Key = 1 },
                new { Id = 2, Key = 1 },
                new { Id = 3, Key = 1 },
                new { Id = 4, Key = 2 },
                new { Id = 5, Key = 2 },
                new { Id = 6, Key = 1 },
                new { Id = 7, Key = 3 },
            }.DistinctBy(x => x.Key);

            Assert.AreEqual(new[] { 1, 4, 7 }, items.Select(x => x.Id));
            Assert.AreEqual(new[] { 1, 4, 7 }, items.Select(x => x.Id));
        }
    }
}
