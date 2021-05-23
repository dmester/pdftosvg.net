using NUnit.Framework;
using PdfToSvg;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.IO
{
    public class ForwardReadBufferTests
    {
        [Test]
        public void Read()
        {
            var data = new[] { 1, 2, 3, 4, 5 };
            var dataCursor = 0;

            var buffer = new ForwardReadBuffer<int>(() => data[dataCursor++], 2);

            Assert.AreEqual(1, buffer.Read());
            Assert.AreEqual(2, buffer.Read());
            Assert.AreEqual(3, buffer.Read());
            Assert.AreEqual(4, buffer.Read());
            Assert.AreEqual(5, buffer.Read());
        }

        [Test]
        public void Peek()
        {
            var data = new[] { 1, 2, 3, 4, 5 };
            var dataCursor = 0;

            var buffer = new ForwardReadBuffer<int>(() => data[dataCursor++], 2);

            Assert.AreEqual(2, buffer.Peek(2));
            Assert.AreEqual(1, buffer.Peek());
            Assert.AreEqual(1, buffer.Read());
            Assert.AreEqual(2, buffer.Peek());
            Assert.AreEqual(2, buffer.Read());
            Assert.AreEqual(3, buffer.Read());
        }
    }
}
