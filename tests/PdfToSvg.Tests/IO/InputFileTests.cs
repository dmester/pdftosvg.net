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

                using (var reader = file.CreateReader(bufferSize: 10, cancellationToken: default))
                {
                    reader.Position = streamPosition;

                    Thread.Sleep(50);

                    Assert.AreEqual(streamPosition, stream.Position);
                }
            });
        }

#if !NET40
        [Test]
        public void CreateExclusiveReaderAsync()
        {
            using var stream = new MemoryStream(new byte[10000], false);
            using var file = new InputFile(stream, true);

            stream.Position = 9999;

            InParallel(async i =>
            {
                var streamPosition = i * 100;

                using (var reader = await file.CreateReaderAsync(bufferSize: 10, cancellationToken: default))
                {
                    reader.Position = streamPosition;

                    await Task.Delay(50);

                    Assert.AreEqual(streamPosition, stream.Position);
                }
            });
        }
#endif

        [Test]
        public void CreateExclusiveSliceReader()
        {
            using var stream = new MemoryStream(new byte[10000], false);
            using var file = new InputFile(stream, true);

            stream.Position = 9999;

            InParallel(i =>
            {
                var sliceStartPosition = i * 100;

                using (var reader = file.CreateSliceReader(sliceStartPosition, 100, bufferSize: 10, cancellationToken: default))
                {
                    reader.Position = 1;

                    Thread.Sleep(50);

                    Assert.AreEqual(sliceStartPosition + 1, stream.Position);
                }
            });
        }

#if !NET40
        [Test]
        public void CreateExclusiveSliceReaderAsync()
        {
            using var stream = new MemoryStream(new byte[10000], false);
            using var file = new InputFile(stream, true);

            stream.Position = 9999;

            InParallel(async i =>
            {
                var sliceStartPosition = i * 500;

                using (var reader = await file.CreateSliceReaderAsync(sliceStartPosition, 100, bufferSize: 10, cancellationToken: default))
                {
                    reader.Position = 1;

                    await Task.Delay(50);

                    Assert.AreEqual(sliceStartPosition + 1, stream.Position);
                }
            });
        }
#endif

#if !NETFRAMEWORK
        [Test]
        public void CreateExclusiveReader_FromMemoryStream()
        {
            var data = new byte[255];
            for (byte i = 0; i < 255; i++) data[i] = i;

            using var stream = new MemoryStream(data, index: 2, count: 100, writable: false, publiclyVisible: true);
            using var file = new InputFile(stream, true);

            using (var reader = file.CreateReader(cancellationToken: default))
            {
                Assert.IsInstanceOf(typeof(BufferedMemoryReader), reader);

                Assert.AreEqual(0, reader.Position);
                Assert.AreEqual(2, reader.ReadByte());

                reader.Position = 5;

                Assert.AreEqual(5, reader.Position);
                Assert.AreEqual(7, reader.ReadByte());
            }
        }

        [Test]
        public async Task CreateExclusiveReaderAsync_FromMemoryStream()
        {
            var data = new byte[255];
            for (byte i = 0; i < 255; i++) data[i] = i;

            using var stream = new MemoryStream(data, index: 2, count: 100, writable: false, publiclyVisible: true);
            using var file = new InputFile(stream, true);

            using (var reader = await file.CreateReaderAsync(cancellationToken: default))
            {
                Assert.IsInstanceOf(typeof(BufferedMemoryReader), reader);

                Assert.AreEqual(0, reader.Position);
                Assert.AreEqual(2, reader.ReadByte());

                reader.Position = 5;

                Assert.AreEqual(5, reader.Position);
                Assert.AreEqual(7, reader.ReadByte());
            }
        }

        [Test]
        public void CreateExclusiveSliceReader_FromMemoryStream()
        {
            var data = new byte[255];
            for (byte i = 0; i < 255; i++) data[i] = i;

            using var stream = new MemoryStream(data, index: 2, count: 100, writable: false, publiclyVisible: true);
            using var file = new InputFile(stream, true);

            using (var reader = file.CreateSliceReader(2, 10, cancellationToken: default))
            {
                Assert.IsInstanceOf(typeof(BufferedMemoryReader), reader);

                Assert.AreEqual(0, reader.Position);
                Assert.AreEqual(4, reader.ReadByte());

                reader.Position = 5;

                Assert.AreEqual(5, reader.Position);
                Assert.AreEqual(9, reader.ReadByte());
            }
        }

        [Test]
        public async Task CreateExclusiveSliceReaderAsync_FromMemoryStream()
        {
            var data = new byte[255];
            for (byte i = 0; i < 255; i++) data[i] = i;

            using var stream = new MemoryStream(data, index: 2, count: 100, writable: false, publiclyVisible: true);
            using var file = new InputFile(stream, true);

            using (var reader = await file.CreateSliceReaderAsync(2, 10, cancellationToken: default))
            {
                Assert.IsInstanceOf(typeof(BufferedMemoryReader), reader);

                Assert.AreEqual(0, reader.Position);
                Assert.AreEqual(4, reader.ReadByte());

                reader.Position = 5;

                Assert.AreEqual(5, reader.Position);
                Assert.AreEqual(9, reader.ReadByte());
            }
        }
#endif
    }
}
