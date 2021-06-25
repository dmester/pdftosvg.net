// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.IO
{
    public class InputFileTests
    {
        private const int ThreadCount = 20;

        private static void InParallel(Action<int> body)
        {
            var threads = new Thread[ThreadCount];
            var exceptions = new ConcurrentBag<Exception>();

            using var singleThreadStartedEvent = new ManualResetEventSlim(false);
            using var allThreadsStartedEvent = new ManualResetEventSlim(false);

            for (var i = 0; i < threads.Length; i++)
            {
                var index = i;

                singleThreadStartedEvent.Reset();

                var thread = new Thread(_ =>
                {
                    singleThreadStartedEvent.Set();
                    try
                    {
                        allThreadsStartedEvent.Wait();
                        body(index);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });

                thread.Start();
                threads[i] = thread;

                singleThreadStartedEvent.Wait();
            }

            allThreadsStartedEvent.Set();

            for (var i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        private static void InParallel(Func<int, Task> body)
        {
            InParallel(i =>
            {
                body(i).Wait();
            });
        }

        [Test]
        public void CreateExclusiveReader()
        {
            using var stream = new MemoryStream(new byte[10000], false);
            using var file = new InputFile(stream, true);

            stream.Position = 9999;

            InParallel(i =>
            {
                var streamPosition = i * 100;

                using (var reader = file.CreateExclusiveReader(bufferSize: 10))
                {
                    reader.Position = streamPosition;

                    Thread.Sleep(50);

                    Assert.AreEqual(streamPosition, stream.Position);
                }
            });
        }

        [Test]
        public void CreateExclusiveReaderAsync()
        {
            using var stream = new MemoryStream(new byte[10000], false);
            using var file = new InputFile(stream, true);

            stream.Position = 9999;

            InParallel(async i =>
            {
                var streamPosition = i * 100;

                using (var reader = await file.CreateExclusiveReaderAsync(bufferSize: 10))
                {
                    reader.Position = streamPosition;

                    await Task.Delay(50);

                    Assert.AreEqual(streamPosition, stream.Position);
                }
            });
        }

        [Test]
        public void CreateExclusiveSliceReader()
        {
            using var stream = new MemoryStream(new byte[10000], false);
            using var file = new InputFile(stream, true);

            stream.Position = 9999;

            InParallel(i =>
            {
                var sliceStartPosition = i * 100;

                using (var reader = file.CreateExclusiveSliceReader(sliceStartPosition, 100, bufferSize: 10))
                {
                    reader.Position = 1;

                    Thread.Sleep(50);

                    Assert.AreEqual(sliceStartPosition + 1, stream.Position);
                }
            });
        }

        [Test]
        public void CreateExclusiveSliceReaderAsync()
        {
            using var stream = new MemoryStream(new byte[10000], false);
            using var file = new InputFile(stream, true);

            stream.Position = 9999;

            InParallel(async i =>
            {
                var sliceStartPosition = i * 500;

                using (var reader = await file.CreateExclusiveSliceReaderAsync(sliceStartPosition, 100, bufferSize: 10))
                {
                    reader.Position = 1;

                    await Task.Delay(50);

                    Assert.AreEqual(sliceStartPosition + 1, stream.Position);
                }
            });
        }
    }
}
