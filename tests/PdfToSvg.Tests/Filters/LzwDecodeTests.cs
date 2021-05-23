using NUnit.Framework;
using PdfToSvg.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Filters
{
    public class LzwDecodeTests
    {
        [Test]
        public void Decode()
        {
            var sourceStream = new MemoryStream(new byte[]
            {
                0x80, 0x0b, 0x60, 0x50, 0x22, 0x0c, 0x0c, 0x85, 0x01
            });

            var decodeStream = new LzwDecodeStream(sourceStream, false);

            var decodedBuffer = new byte[2000];
            var decodedLength = decodeStream.Read(decodedBuffer, 0, decodedBuffer.Length);
            var decodedBufferRightLength = new byte[decodedLength];
            Buffer.BlockCopy(decodedBuffer, 0, decodedBufferRightLength, 0, decodedLength);

            var expectedResult = new byte[]
            {
                45, 45, 45, 45, 45, 65, 45, 45, 45, 66
            };

            Assert.AreEqual(expectedResult, decodedBufferRightLength);
        }
    }
}
