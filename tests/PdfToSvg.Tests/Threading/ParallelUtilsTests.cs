// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Threading
{
    internal class ParallelUtilsTests
    {
#if !NET40
        [Test]
        public async Task Success()
        {
            var items = Enumerable.Range(0, 20).ToList();
            var start = DateTime.Now;
            var executed = new List<int>();

            await ParallelUtils.ForEachAsync(items, async (item, index) =>
            {
                lock (executed) executed.Add(item);

                Assert.AreEqual(index, item);

                if (item >= 10)
                {
                    await Task.Delay((item - 10) * 500);
                }
            }, maxConcurrentTasks: 4);

            var end = DateTime.Now;

            var duration = end - start;
            var expectedDuration = TimeSpan.FromSeconds(7.5);

            const int AllowedDiffSeconds = 2;

            if (Math.Abs(expectedDuration.TotalSeconds - duration.TotalSeconds) > AllowedDiffSeconds)
            {
                Assert.AreEqual(expectedDuration, duration);
            }

            executed.Sort();
            Assert.AreEqual(items, executed);
        }

        [Test]
        public void Cancel()
        {
            var items = Enumerable.Range(0, 20);
            var start = DateTime.Now;
            var executed = new List<int>();

            var cts = new CancellationTokenSource();

            Assert.ThrowsAsync<OperationCanceledException>(() => ParallelUtils.ForEachAsync(items, async (item, index) =>
            {
                if (item == 3)
                {
                    cts.Cancel();
                }

                await Task.Delay(100);
            }, cancellationToken: cts.Token));
        }

        [Test]
        public void Throws()
        {
            var items = Enumerable.Range(0, 20);
            var start = DateTime.Now;
            var executed = new List<int>();

            var ex = Assert.ThrowsAsync<AggregateException>(() => ParallelUtils.ForEachAsync(items, async (item, index) =>
            {
                if (item == 14)
                {
                    throw new Exception("Oh no");
                }

                await Task.Delay(100);
            }));

            Assert.AreEqual("Oh no", ex.InnerException.Message);
        }
#endif
    }
}
