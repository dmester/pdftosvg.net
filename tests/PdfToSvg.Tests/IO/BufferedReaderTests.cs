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
    class BufferedReaderTests
    {
        private static BufferedStreamReader Create(string content, int bufferSize)
        {
            var binaryContent = Encoding.ASCII.GetBytes(content);
            var streamContent = new MemoryStream(binaryContent);
            return new BufferedStreamReader(streamContent, bufferSize: bufferSize);
        }

        [Test]
        public void ReadingRanges()
        {
            var buffer = Create("abcdefghijklmn", 4);

            var readBytes = new byte[6];
            Assert.AreEqual('a', buffer.PeekChar());
            Assert.AreEqual(0, buffer.Position);
            Assert.AreEqual(4, buffer.BaseStream.Position);

            Assert.AreEqual(1, buffer.Read(readBytes, 0, 1));
            Assert.AreEqual((byte)'a', readBytes[0]);
            Assert.AreEqual(1, buffer.Position);
            Assert.AreEqual(4, buffer.BaseStream.Position);

            Assert.AreEqual(6, buffer.Read(readBytes, 0, 6));
            Assert.AreEqual(7, buffer.Position);
            Assert.AreEqual(8, buffer.BaseStream.Position);
        }

        [Test]
        public void Peeking()
        {
            var buffer = Create("abcdefghijklmn", 6);
            Assert.AreEqual(0, buffer.Position);
            Assert.AreEqual(0, buffer.BaseStream.Position);

            // Peek within buffer
            Assert.AreEqual('a', buffer.PeekChar());
            Assert.AreEqual(0, buffer.Position);
            Assert.AreEqual(6, buffer.BaseStream.Position);

            Assert.AreEqual('a', buffer.PeekChar(1));
            Assert.AreEqual('b', buffer.PeekChar(2));
            Assert.AreEqual('c', buffer.PeekChar(3));
            Assert.AreEqual(0, buffer.Position);
            Assert.AreEqual(6, buffer.BaseStream.Position);

            // Peek outside buffer
            buffer.Skip(3);
            Assert.AreEqual('d', buffer.ReadChar());
            Assert.AreEqual(4, buffer.Position);
            Assert.AreEqual(6, buffer.BaseStream.Position);

            Assert.AreEqual('g', buffer.PeekChar(3));
            Assert.AreEqual(4, buffer.Position);
            Assert.AreEqual(10, buffer.BaseStream.Position);

            // Peek outside file
            buffer.Seek(-2, SeekOrigin.End);
            Assert.AreEqual('m', buffer.ReadChar());

            Assert.AreEqual(BufferedReader.EndOfStreamMarker, buffer.PeekChar(3));
            Assert.AreEqual(BufferedReader.EndOfStreamMarker, buffer.PeekChar(2));
            Assert.AreEqual('n', buffer.PeekChar(1));

            Assert.AreEqual(14, buffer.BaseStream.Position);
        }

        [Test]
        public void Seeking()
        {
            var buffer = Create("abcdefghij", 4);

            Assert.AreEqual(0, buffer.Position);
            Assert.AreEqual(0, buffer.BaseStream.Position);

            Assert.AreEqual('a', buffer.ReadChar());

            Assert.AreEqual(1, buffer.Position);
            Assert.AreEqual(4, buffer.BaseStream.Position);

            Assert.AreEqual('b', buffer.ReadChar());
            Assert.AreEqual('c', buffer.ReadChar());
            Assert.AreEqual('d', buffer.ReadChar());

            Assert.AreEqual(4, buffer.Position);
            Assert.AreEqual(4, buffer.BaseStream.Position);

            // Seek within buffer
            Assert.AreEqual(1, buffer.Seek(1, SeekOrigin.Begin));
            Assert.AreEqual(1, buffer.Position);
            Assert.AreEqual(4, buffer.BaseStream.Position);
            Assert.AreEqual('b', buffer.ReadChar());
            Assert.AreEqual('c', buffer.ReadChar());

            Assert.AreEqual(2, buffer.Seek(-8, SeekOrigin.End));
            Assert.AreEqual(2, buffer.Position);
            Assert.AreEqual(4, buffer.BaseStream.Position);
            Assert.AreEqual('c', buffer.ReadChar());

            Assert.AreEqual(2, buffer.Seek(-1, SeekOrigin.Current));
            Assert.AreEqual(2, buffer.Position);
            Assert.AreEqual(4, buffer.BaseStream.Position);

            Assert.AreEqual(0, buffer.Seek(-100, SeekOrigin.Current));
            Assert.AreEqual(0, buffer.Position);
            Assert.AreEqual(4, buffer.BaseStream.Position);

            // Seek outside buffer
            Assert.AreEqual(5, buffer.Seek(5, SeekOrigin.Begin));
            Assert.AreEqual(5, buffer.Position);
            Assert.AreEqual(5, buffer.BaseStream.Position);

            Assert.AreEqual('f', buffer.ReadChar());
            Assert.AreEqual(6, buffer.Position);
            Assert.AreEqual(9, buffer.BaseStream.Position);

            Assert.AreEqual(3, buffer.Seek(3, SeekOrigin.Begin));
            Assert.AreEqual(3, buffer.Position);
            Assert.AreEqual(3, buffer.BaseStream.Position);

            Assert.AreEqual('d', buffer.ReadChar());
            Assert.AreEqual(4, buffer.Position);
            Assert.AreEqual(7, buffer.BaseStream.Position);
        }
    }
}
