// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

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
    public class RunLengthDecodeTests
    {
        [Test]
        public void DetectLength()
        {
            var sourceStream = new MemoryStream(new byte[]
            {
                // Literal
                0, 42,
                3, 1, 2, 3, 4,

                // Repeated bytes
                255, 127,
                129, 77,

                // EOD
                128,

                // Not part of the stream
                1, 2, 3, 4, 5, 6,
            });

            Assert.AreEqual(12, Filter.RunLengthDecode.DetectStreamLength(sourceStream));
        }

        [Test]
        public void Decode()
        {
            var sourceStream = new MemoryStream(new byte[]
            {
                // Literal
                0, 42,

                4, 11, 12, 13, 14, 15,

                127,
                254, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                1, 2, 3, 4, 5, 6, 7, 255,

                // Repeated bytes
                255, 127,
                129, 77,

                // Literal
                0, 255,

                // EOD
                128,

                123 // Should be ignored
            });

            var decodeStream = new RunLengthDecodeStream(sourceStream);

            var decodedBuffer = new byte[2000];
            var decodedLength = decodeStream.Read(decodedBuffer, 0, decodedBuffer.Length);
            var decodedBufferRightLength = new byte[decodedLength];
            Buffer.BlockCopy(decodedBuffer, 0, decodedBufferRightLength, 0, decodedLength);

            var expectedResult = new byte[]
            {
                // Literal
                42,

                11, 12, 13, 14, 15,

                254, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                1, 2, 3, 4, 5, 6, 7, 255,

                127, 127,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77, 77, 77,
                77, 77, 77, 77, 77, 77, 77, 77,

                255,
            };

            Assert.AreEqual(expectedResult, decodedBufferRightLength);
        }

        [Test]
        public void InvalidCopy()
        {
            var sourceStream = new MemoryStream(new byte[]
            {
                // Incomplete copy range
                4, 11, 12, 13,
            });

            var decodeStream = new RunLengthDecodeStream(sourceStream);

            Assert.Throws<FilterException>(() =>
            {
                var buffer = new byte[1000];
                decodeStream.Read(buffer, 0, 1000);
            });
        }

        [Test]
        public void InvalidRepeat()
        {
            var sourceStream = new MemoryStream(new byte[]
            {
                // Missing byte to repeat
                255
            });

            var decodeStream = new RunLengthDecodeStream(sourceStream);

            Assert.Throws<FilterException>(() =>
            {
                var buffer = new byte[1000];
                decodeStream.Read(buffer, 0, 1000);
            });
        }
    }
}
