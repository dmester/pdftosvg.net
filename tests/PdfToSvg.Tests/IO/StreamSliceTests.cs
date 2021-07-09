// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.IO
{
    public class StreamSliceTests
    {
        [Test]
        public void Seeking()
        {
            var data = new byte[100];
            var random = new Random(1);
            random.NextBytes(data);

            var baseStream = new MemoryStream(data, false);
            var slice = new StreamSlice(baseStream, 10, 20);

            Assert.AreEqual(10, baseStream.Position);
            Assert.AreEqual(0, slice.Position);

            slice.Seek(-5, SeekOrigin.End);
            Assert.AreEqual(25, baseStream.Position);
            Assert.AreEqual(15, slice.Position);

            slice.Seek(5, SeekOrigin.End);
            Assert.AreEqual(30, baseStream.Position);
            Assert.AreEqual(20, slice.Position);

            slice.Seek(-5, SeekOrigin.Begin);
            Assert.AreEqual(10, baseStream.Position);
            Assert.AreEqual(0, slice.Position);

            slice.Seek(5, SeekOrigin.Begin);
            Assert.AreEqual(15, baseStream.Position);
            Assert.AreEqual(5, slice.Position);

            slice.Seek(1, SeekOrigin.Current);
            Assert.AreEqual(16, baseStream.Position);
            Assert.AreEqual(6, slice.Position);

            slice.Seek(100, SeekOrigin.Current);
            Assert.AreEqual(30, baseStream.Position);
            Assert.AreEqual(20, slice.Position);
        }

        [Test]
        public void ConstrainedReading()
        {
            var data = new byte[100];
            var random = new Random(1);
            random.NextBytes(data);

            var baseStream = new MemoryStream(data, false);
            var slice = new StreamSlice(baseStream, 10, 20);

            slice.Position = 15;

            var readBuffer = new byte[100];

            var expectedBuffer = new byte[100];
            Array.Copy(data, 25, expectedBuffer, 0, 5);

            var read = slice.Read(readBuffer, 0, 100);

            Assert.AreEqual(5, read);
            Assert.AreEqual(expectedBuffer, readBuffer);
            Assert.AreEqual(20, slice.Position);
        }

        [Test]
        public void UnconstrainedReading()
        {
            var data = new byte[100];
            var random = new Random(1);
            random.NextBytes(data);

            var baseStream = new MemoryStream(data, false);
            var slice = new StreamSlice(baseStream, 10, 20);

            slice.Position = 5;

            var readBuffer = new byte[100];

            var expectedBuffer = new byte[100];
            Array.Copy(data, 15, expectedBuffer, 0, 10);

            var read = slice.Read(readBuffer, 0, 10);

            Assert.AreEqual(10, read);
            Assert.AreEqual(expectedBuffer, readBuffer);
            Assert.AreEqual(15, slice.Position);
        }

        [Test]
        public void DisposedLeaveOpen()
        {
            var data = new byte[100];
            var baseStream = new MemoryStream(data, false);

            var slice = new StreamSlice(baseStream, 10, 20, true);

            slice.Dispose();

            Assert.Throws<ObjectDisposedException>(() => slice.Read(data, 0, 10));
            Assert.Throws<ObjectDisposedException>(() => slice.Seek(0, SeekOrigin.Begin));
            Assert.Throws<ObjectDisposedException>(() => slice.Position = 4);

            // Should not throw
            baseStream.Read(data, 0, 10);
        }

        [Test]
        public void Disposed()
        {
            var data = new byte[100];
            var baseStream = new MemoryStream(data, false);

            var slice = new StreamSlice(baseStream, 10, 20);

            slice.Dispose();

            Assert.Throws<ObjectDisposedException>(() => slice.Read(data, 0, 10));
            Assert.Throws<ObjectDisposedException>(() => slice.Seek(0, SeekOrigin.Begin));
            Assert.Throws<ObjectDisposedException>(() => slice.Position = 4);

            Assert.Throws<ObjectDisposedException>(() => baseStream.Read(data, 0, 10));
        }
    }
}
