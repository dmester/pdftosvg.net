// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Common
{
    public class StableIDTests
    {
        [Test]
        public void Numbers()
        {
            Assert.AreEqual(StableID.Generate("im", 1, 11, 3), StableID.Generate("im", "1", "11", "3"));
            Assert.AreNotEqual(StableID.Generate("im", 11, 1, 3), StableID.Generate("im", "1", "11", "3"));
        }

        [Test]
        public void Null()
        {
            Assert.AreEqual(StableID.Generate("im", null), StableID.Generate("im", null));
            Assert.AreEqual(StableID.Generate("im", new object[] { null, null }), StableID.Generate("im", new object[] { null, null }));

            Assert.AreNotEqual(StableID.Generate("im", 0), StableID.Generate("im", null));
            Assert.AreNotEqual(StableID.Generate("im", new object[] { null, null, null }), StableID.Generate("im", new object[] { null, null }));
            Assert.AreNotEqual(StableID.Generate("im", new object[] { null, null, 0 }), StableID.Generate("im", new object[] { null, null }));
        }

        [Test]
        public void Enumerables()
        {
            Assert.AreEqual(StableID.Generate("im", new int[] { 1, 2, 3 }), StableID.Generate("im", new int[] { 1, 2, 3 }));
            Assert.AreNotEqual(StableID.Generate("im", new int[] { 1, 2, 4 }), StableID.Generate("im", new int[] { 1, 2, 3 }));
            Assert.AreNotEqual(StableID.Generate("im", new int[] { 1, 2 }), StableID.Generate("im", new int[] { 1, 2, 3 }));
        }

        [Test]
        public void NestedEnumerables()
        {
            Assert.AreEqual(
                StableID.Generate("im", new object[] { new object[] { new int[] { 1, 2 }, "abc" } }),
                StableID.Generate("im", new object[] { new object[] { new int[] { 1, 2 }, "abc" } }));

            Assert.AreNotEqual(
                StableID.Generate("im", new object[] { new object[] { new int[] { 1, 2 }, "abc" } }),
                StableID.Generate("im", new object[] { new object[] { new int[] { 1, 3 }, "abc" } }));

            Assert.AreNotEqual(
                StableID.Generate("im", new object[] { new object[] { new int[] { 1, 2 }, "abc" } }),
                StableID.Generate("im", new object[] { new object[] { new int[] { 1 }, "abc" } }));

            Assert.AreNotEqual(
                StableID.Generate("im", new object[] { new object[] { new int[] { 1, 2 }, "abc" } }),
                StableID.Generate("im", new object[] { new object[] { new int[] { 1, 3 }, "abC" } }));
        }

        [Test]
        public void Stream()
        {
            var bytes = new byte[10000];
            var random = new Random(1);
            random.NextBytes(bytes);

            var otherBytes = (byte[])bytes.Clone();

            var stream = new MemoryStream(bytes);

            stream.Position = 0;
            Assert.AreEqual(StableID.Generate("im", otherBytes), StableID.Generate("im", stream));

            stream.Position = 0;

            otherBytes[4215] = unchecked((byte)(otherBytes[4215] - 1));
            Assert.AreNotEqual(StableID.Generate("im", otherBytes), StableID.Generate("im", stream));
        }
    }
}
