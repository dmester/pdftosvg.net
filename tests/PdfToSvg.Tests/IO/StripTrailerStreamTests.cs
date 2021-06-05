// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.IO
{
    public class StripTrailerStreamTests
    {
        [Test]
        public void ReadSmallData()
        {
            var sourceData = new byte[] { 1, 2 };
            var readData = new byte[1000];

            var sourceStream = new NoSeekMemoryStream(sourceData);
            var trailerStream = new StripTrailerStream(sourceStream, 4);

            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(0, trailerStream.Read(readData, 0, i));
            }

            Assert.AreEqual(0, trailerStream.Read(readData, 0, 1000));

            Assert.AreEqual(sourceData, trailerStream.GetTrailer());
        }

        [Test]
        public async Task ReadSmallDataAsync()
        {
            var sourceData = new byte[] { 1, 2 };
            var readData = new byte[1000];

            var sourceStream = new NoSeekMemoryStream(sourceData);
            var trailerStream = new StripTrailerStream(sourceStream, 4);

            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(0, await trailerStream.ReadAsync(readData, 0, i));
            }

            Assert.AreEqual(0, await trailerStream.ReadAsync(readData, 0, 1000));

            Assert.AreEqual(sourceData, trailerStream.GetTrailer());
        }

        [Test]
        public void ReadEmptyData()
        {
            var sourceData = new byte[0];
            var readData = new byte[1000];

            var sourceStream = new NoSeekMemoryStream(sourceData);
            var trailerStream = new StripTrailerStream(sourceStream, 4);

            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(0, trailerStream.Read(readData, 0, i));
            }

            Assert.AreEqual(0, trailerStream.Read(readData, 0, 1000));

            Assert.AreEqual(new byte[0], trailerStream.GetTrailer());
        }

        [Test]
        public async Task ReadEmptyDataAsync()
        {
            var sourceData = new byte[0];
            var readData = new byte[1000];

            var sourceStream = new NoSeekMemoryStream(sourceData);
            var trailerStream = new StripTrailerStream(sourceStream, 4);

            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(0, await trailerStream.ReadAsync(readData, 0, i));
            }

            Assert.AreEqual(0, await trailerStream.ReadAsync(readData, 0, 1000));

            Assert.AreEqual(new byte[0], trailerStream.GetTrailer());
        }

        private async Task ReadLargeDataAsync(string readPatternString, Func<StripTrailerStream, byte[], int, int, Task<int>> readImpl)
        {
            const int DataSize = 10000;
            const int TrailerSize = 4;

            var random = new Random(0);
            var sourceData = new byte[DataSize + TrailerSize];
            random.NextBytes(sourceData);

            var sourceStream = new NoSeekMemoryStream(sourceData);
            var trailerStream = new StripTrailerStream(sourceStream, TrailerSize);

            var readBuffer = new byte[DataSize];
            var totalRead = 0;
            int read;

            var readPattern = readPatternString
                .Split(',')
                .Select(x => int.Parse(x, CultureInfo.InvariantCulture))
                .ToArray();

            var readPatternIndex = 0;

            Assert.AreEqual(0, trailerStream.Read(readBuffer, 0, 0));

            do
            {
                read = await readImpl(trailerStream, 
                    readBuffer, totalRead,
                    Math.Min(readPattern[readPatternIndex], readBuffer.Length - totalRead));

                readPatternIndex = (readPatternIndex + 1) % readPattern.Length;
                totalRead += read;

                Assert.AreEqual(totalRead, trailerStream.Position);
                Assert.AreEqual(sourceData.Slice(totalRead, TrailerSize), trailerStream.GetTrailer(), "Trailer, totalRead {0}", totalRead);
            }
            while (read != 0);

            Assert.AreEqual(DataSize, totalRead, "Read data size");
            Assert.AreEqual(sourceData.Slice(0, DataSize), readBuffer, "Read data");
        }

        [TestCase("3,3,1,1,4,4,3,3,1000,1000")]
        [TestCase("1000,4,4,3,3")]
        public Task ReadLargeDataSync(string readPatternString)
        {
            return ReadLargeDataAsync(readPatternString, (stream, buffer, offset, count) =>
            {
                return Task.FromResult(stream.Read(buffer, offset, count));
            });
        }

        [TestCase("3,3,1,1,4,4,3,3,1000,1000")]
        [TestCase("1000,4,4,3,3")]
        public Task ReadLargeDataAsync(string readPatternString)
        {
            return ReadLargeDataAsync(readPatternString, (stream, buffer, offset, count) =>
            {
                return stream.ReadAsync(buffer, offset, count);
            });
        }

        [TestCase("3,3,1,1,4,4,3,3,1000,1000")]
        [TestCase("1000,4,4,3,3")]
        public Task ReadLargeDataAPM(string readPatternString)
        {
            return ReadLargeDataAsync(readPatternString, (stream, buffer, offset, count) =>
            {
                return Task<int>.Factory.FromAsync(
                    (callback, state) => stream.BeginRead(buffer, offset, count, callback, state),
                    asyncResult => stream.EndRead(asyncResult),
                    null);
            });
        }
    }
}
