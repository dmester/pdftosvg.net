// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpx.IO
{
    public class JpxDataReaderTests
    {
        [Test]
        public void ReadByte_Success()
        {
            var reader = new JpxDataReader([
                0x12, 0x13,
            ]);

            Assert.AreEqual((byte)0x12, reader.ReadByte());
            Assert.AreEqual((byte)0x13, reader.ReadByte());
        }

        [Test]
        public void ReadByte_Eof()
        {
            var reader = new JpxDataReader(new byte[0]);
            Assert.Throws<EndOfStreamException>(() => reader.ReadByte());
        }

        [Test]
        public void ReadUInt16_Success()
        {
            var reader = new JpxDataReader([
                0x12, 0x13, 0x14, 0x15,
            ]);

            Assert.AreEqual((ushort)0x1213, reader.ReadUInt16());
            Assert.AreEqual((ushort)0x1415, reader.ReadUInt16());
        }

        [Test]
        public void ReadUInt16_Eof()
        {
            var reader = new JpxDataReader([0x12]);
            Assert.Throws<EndOfStreamException>(() => reader.ReadUInt16());
        }

        [Test]
        public void ReadUInt32_Success()
        {
            var reader = new JpxDataReader([
                0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19,
            ]);

            Assert.AreEqual((uint)0x12131415, reader.ReadUInt32());
            Assert.AreEqual((uint)0x16171819, reader.ReadUInt32());
        }

        [Test]
        public void ReadUInt32_Eof()
        {
            var reader = new JpxDataReader([0x12, 0x13, 0x14]);
            Assert.Throws<EndOfStreamException>(() => reader.ReadUInt32());
        }

        [Test]
        public void ReadUInt64_Success()
        {
            var reader = new JpxDataReader([
                0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19,
                0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29,
            ]);

            Assert.AreEqual((ulong)0x1213141516171819, reader.ReadUInt64());
            Assert.AreEqual((ulong)0x2223242526272829, reader.ReadUInt64());
        }

        [Test]
        public void ReadUInt64_Eof()
        {
            var reader = new JpxDataReader([0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18]);
            Assert.Throws<EndOfStreamException>(() => reader.ReadUInt64());
        }

        [Test]
        public void SkipBytes_Success()
        {
            var reader = new JpxDataReader([
                0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19,
            ]);

            reader.SkipBytes(1);

            Assert.AreEqual(1, reader.Cursor);
            Assert.AreEqual((byte)0x13, reader.ReadByte());
        }

        [Test]
        public void SkipBytes_Full()
        {
            var reader = new JpxDataReader([
                0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19,
            ]);

            reader.SkipBytes(8);

            Assert.AreEqual(8, reader.Cursor);
        }

        [Test]
        public void SkipBytes_Eof()
        {
            var reader = new JpxDataReader([
                0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19,
            ]);

            Assert.Throws<EndOfStreamException>(() => reader.SkipBytes(9));
        }

        [Test]
        public void SkipBytes_Negative()
        {
            var reader = new JpxDataReader([
                0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19,
            ]);

            Assert.Throws<ArgumentOutOfRangeException>(() => reader.SkipBytes(-1));
        }

        [Test]
        public void ReadBytes_Success()
        {
            var reader = new JpxDataReader([
                0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19,
            ]);
            reader.SkipBytes(1);

            var segment = reader.ReadBytes(4);

            Assert.AreEqual(5, reader.Cursor);
            Assert.AreEqual(1, segment.Offset);
            Assert.AreEqual(4, segment.Count);
        }

        [Test]
        public void ReadBytes_Negative()
        {
            var reader = new JpxDataReader([
                0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19,
            ]);

            Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadBytes(-1));
        }

        [Test]
        public void ReadBytes_Eof()
        {
            var reader = new JpxDataReader([
                0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19,
            ]);

            Assert.Throws<EndOfStreamException>(() => reader.ReadBytes(9));
        }

        [Test]
        public void Slice_Negative()
        {
            var reader = new JpxDataReader([0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18]);
            reader.SkipBytes(2);

            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Slice(-1));
        }

        [Test]
        public void Slice_Empty()
        {
            var reader = new JpxDataReader([0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18]);

            var slice = reader.Slice(0);

            Assert.AreEqual(0, slice.Cursor);
            Assert.AreEqual(0, slice.Length);
        }

        [Test]
        public void Slice_Partial()
        {
            var reader = new JpxDataReader([0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18]);
            reader.SkipBytes(2);

            var slice = reader.Slice(4);

            Assert.AreEqual(0, slice.Cursor);
            Assert.AreEqual(4, slice.Length);
        }

        [Test]
        public void Slice_Full()
        {
            var reader = new JpxDataReader([0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18]);

            var slice = reader.Slice(7);

            Assert.AreEqual(0, slice.Cursor);
            Assert.AreEqual(7, slice.Length);
        }

        [Test]
        public void Slice_Eof()
        {
            var reader = new JpxDataReader([0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18]);

            Assert.Throws<EndOfStreamException>(() => reader.Slice(8));
        }
    }
}
