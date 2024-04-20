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
    public class StreamExtensionsTests
    {
        [Test]
        public void ReadCompactUInt32()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            for (var i = 0u; i < 300u; i++)
            {
                writer.WriteCompactUInt32(i);
            }

            for (var i = 16120u; i < 16200u; i++)
            {
                writer.WriteCompactUInt32(i);
            }

            writer.WriteCompactUInt32(uint.MaxValue);

            stream.Position = 0;
            var reader = new BinaryReader(stream);

            for (var i = 0u; i < 300u; i++)
            {
                Assert.AreEqual(i, reader.ReadCompactUInt32());
            }

            for (var i = 16120u; i < 16200u; i++)
            {
                Assert.AreEqual(i, reader.ReadCompactUInt32());
            }

            Assert.AreEqual(uint.MaxValue, reader.ReadCompactUInt32());
        }

        [Test]
        public void ReadAll()
        {
            var stream = new ByteByByteMemoryStream(1, 2, 3, 4, 5, 6, 7, 8, 9);

            var buffer = new byte[5];

            Assert.AreEqual(5, stream.ReadAll(buffer, 0, buffer.Length));
            Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, buffer);

            Assert.AreEqual(1, stream.ReadAll(buffer, 0, 1));
            Assert.AreEqual(new byte[] { 6, 2, 3, 4, 5 }, buffer);

            Assert.AreEqual(3, stream.ReadAll(buffer, 0, buffer.Length));
            Assert.AreEqual(new byte[] { 7, 8, 9, 4, 5 }, buffer);

            Assert.AreEqual(0, stream.ReadAll(buffer, 0, buffer.Length));
            Assert.AreEqual(new byte[] { 7, 8, 9, 4, 5 }, buffer);

            Assert.AreEqual(0, stream.ReadAll(buffer, 0, buffer.Length));
            Assert.AreEqual(new byte[] { 7, 8, 9, 4, 5 }, buffer);
        }

#if !NET40
        [Test]
        public async Task ReadAllAsync()
        {
            var stream = new ByteByByteMemoryStream(1, 2, 3, 4, 5, 6, 7, 8, 9);

            var buffer = new byte[5];

            Assert.AreEqual(5, await stream.ReadAllAsync(buffer, 0, buffer.Length));
            Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, buffer);

            Assert.AreEqual(1, await stream.ReadAllAsync(buffer, 0, 1));
            Assert.AreEqual(new byte[] { 6, 2, 3, 4, 5 }, buffer);

            Assert.AreEqual(3, await stream.ReadAllAsync(buffer, 0, buffer.Length));
            Assert.AreEqual(new byte[] { 7, 8, 9, 4, 5 }, buffer);

            Assert.AreEqual(0, await stream.ReadAllAsync(buffer, 0, buffer.Length));
            Assert.AreEqual(new byte[] { 7, 8, 9, 4, 5 }, buffer);

            Assert.AreEqual(0, await stream.ReadAllAsync(buffer, 0, buffer.Length));
            Assert.AreEqual(new byte[] { 7, 8, 9, 4, 5 }, buffer);
        }
#endif

        private class WrongLengthStream : MemoryStream
        {
            public WrongLengthStream(byte[] data) : base(data) { }
            public override long Length => 10;
        }

        [Test]
        public void ToMemoryStream_MemoryStream()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var stream = (Stream)new MemoryStream(data);
            stream.Position = 3;
            Assert.AreEqual(new byte[] { 4, 5, 6 }, stream.ToMemoryStream().ToArray());
        }

        [Test]
        public void ToMemoryStream_NonSeekableStream()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var stream = (Stream)new NoSeekMemoryStream(data);
            stream.Skip(2);
            Assert.AreEqual(new byte[] { 3, 4, 5, 6 }, stream.ToMemoryStream().ToArray());
        }

#if !NET40
        [Test]
        public async Task ToMemoryStreamAsync_MemoryStream()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var stream = (Stream)new MemoryStream(data);
            stream.Position = 3;
            Assert.AreEqual(new byte[] { 4, 5, 6 }, (await stream.ToMemoryStreamAsync()).ToArray());
        }

        [Test]
        public async Task ToMemoryStreamAsync_NonSeekableStream()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var stream = (Stream)new NoSeekMemoryStream(data);
            stream.Skip(2);
            Assert.AreEqual(new byte[] { 3, 4, 5, 6 }, (await stream.ToMemoryStreamAsync()).ToArray());
        }
#endif

        [Test]
        public void ToArray_MemoryStream()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var stream = (Stream)new MemoryStream(data);
            stream.Position = 3;
            Assert.AreEqual(data, stream.ToArray());
        }

        [Test]
        public void ToArray_WrongLength()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var stream = (Stream)new WrongLengthStream(data);
            stream.Position = 3;
            Assert.AreEqual(data, stream.ToArray());
        }

        [Test]
        public void ToArray_NonSeekableStream()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var stream = (Stream)new NoSeekMemoryStream(data);
            stream.Skip(2);
            Assert.AreEqual(new byte[] { 3, 4, 5, 6 }, stream.ToArray());
        }

        [Test]
        public void Skip()
        {
            var data = new byte[10000];

            var random = new Random(1);
            random.NextBytes(data);

            var stream = new NoSeekMemoryStream(data);

            stream.Skip(1);
            Assert.AreEqual(data[1], stream.ReadByte());
            Assert.AreEqual(data[2], stream.ReadByte());

            stream.Skip(1500);
            Assert.AreEqual(data[1503], stream.ReadByte());
            Assert.AreEqual(data[1504], stream.ReadByte());

            stream.Skip(9000);
            Assert.AreEqual(-1, stream.ReadByte());
        }

        [Test]
        public void WriteBigEndian()
        {
            var stream = new MemoryStream();

            stream.WriteBigEndian(0xfedca123u);

            Assert.AreEqual(new byte[] { 0xfe, 0xdc, 0xa1, 0x23 }, stream.ToArray());
        }
    }
}
