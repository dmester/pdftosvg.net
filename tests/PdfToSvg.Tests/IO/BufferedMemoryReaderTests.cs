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
    public class BufferedMemoryReaderTests
    {
        private static BufferedMemoryReader Create(string content)
        {
            var binaryContent = Encoding.ASCII.GetBytes("!!!" + content + "!!!");
            return new BufferedMemoryReader(binaryContent, 3, content.Length);
        }

        [Test]
        public void ReadingRanges()
        {
            var buffer = Create("abcdefghijklmn");

            var readBytes = new byte[6];
            Assert.AreEqual('a', buffer.PeekChar());
            Assert.AreEqual(0, buffer.Position);

            Assert.AreEqual(1, buffer.Read(readBytes, 0, 1));
            Assert.AreEqual((byte)'a', readBytes[0]);
            Assert.AreEqual(1, buffer.Position);

            Assert.AreEqual(6, buffer.Read(readBytes, 0, 6));
            Assert.AreEqual(7, buffer.Position);
        }

        [Test]
        public void Seeking()
        {
            var buffer = Create("abcdefghij");

            Assert.AreEqual(0, buffer.Position);

            Assert.AreEqual('a', buffer.ReadChar());

            Assert.AreEqual(1, buffer.Position);

            Assert.AreEqual('b', buffer.ReadChar());
            Assert.AreEqual('c', buffer.ReadChar());
            Assert.AreEqual('d', buffer.ReadChar());

            Assert.AreEqual(4, buffer.Position);

            // Seek
            Assert.AreEqual(1, buffer.Seek(1, SeekOrigin.Begin));
            Assert.AreEqual(1, buffer.Position);
            Assert.AreEqual('b', buffer.ReadChar());
            Assert.AreEqual('c', buffer.ReadChar());

            Assert.AreEqual(2, buffer.Seek(-8, SeekOrigin.End));
            Assert.AreEqual(2, buffer.Position);
            Assert.AreEqual('c', buffer.ReadChar());

            Assert.AreEqual(2, buffer.Seek(-1, SeekOrigin.Current));
            Assert.AreEqual(2, buffer.Position);

            Assert.AreEqual(0, buffer.Seek(-100, SeekOrigin.Current));
            Assert.AreEqual(0, buffer.Position);
        }
    }
}
