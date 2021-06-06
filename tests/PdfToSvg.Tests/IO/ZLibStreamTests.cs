// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.IO
{
    public class ZLibStreamTests
    {
        private byte[] Deflate(byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(memoryStream, CompressionMode.Compress))
                {
                    zlibStream.Write(data, 0, data.Length);
                }

                return memoryStream.ToArray();
            }
        }

        private async Task<byte[]> DeflateAsync(byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(memoryStream, CompressionMode.Compress))
                {
                    await zlibStream.WriteAsync(data, 0, data.Length);
                }

                return memoryStream.ToArray();
            }
        }

        private async Task<byte[]> DeflateAPM(byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(memoryStream, CompressionMode.Compress))
                {
                    await Task.Factory.FromAsync(
                        zlibStream.BeginWrite, zlibStream.EndWrite,
                        data, 0, data.Length, null);
                }

                return memoryStream.ToArray();
            }
        }

        private byte[] Inflate(byte[] data)
        {
            using (var inStream = new NoSeekMemoryStream(data))
            {
                using (var outStream = new MemoryStream())
                {
                    using (var zlibStream = new ZLibStream(inStream, CompressionMode.Decompress))
                    {
                        var buffer = new byte[4096];
                        int read;

                        do
                        {
                            read = zlibStream.Read(buffer, 0, buffer.Length);
                            outStream.Write(buffer, 0, read);
                        }
                        while (read > 0);
                    }

                    return outStream.ToArray();
                }
            }
        }

        private async Task<byte[]> InflateAsync(byte[] data)
        {
            using (var inStream = new NoSeekMemoryStream(data))
            {
                using (var outStream = new MemoryStream())
                {
                    using (var zlibStream = new ZLibStream(inStream, CompressionMode.Decompress))
                    {
                        var buffer = new byte[4096];
                        int read;

                        do
                        {
                            read = await zlibStream.ReadAsync(buffer, 0, buffer.Length);
                            outStream.Write(buffer, 0, read);
                        }
                        while (read > 0);
                    }

                    return outStream.ToArray();
                }
            }
        }

        private async Task<byte[]> InflateAPM(byte[] data)
        {
            using (var inStream = new NoSeekMemoryStream(data))
            {
                using (var outStream = new MemoryStream())
                {
                    using (var zlibStream = new ZLibStream(inStream, CompressionMode.Decompress))
                    {
                        var buffer = new byte[4096];
                        int read;

                        do
                        {
                            read = await Task<int>.Factory.FromAsync(
                                zlibStream.BeginRead, zlibStream.EndRead,
                                buffer, 0, buffer.Length, null);

                            outStream.Write(buffer, 0, read);
                        }
                        while (read > 0);
                    }

                    return outStream.ToArray();
                }
            }
        }

        [TestCase("ZLib_WindowBits8.bin")]
        [TestCase("ZLib_WindowBits15.bin")]
        public void Inflate(string compressedFileName)
        {
            var compressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", compressedFileName));
            var expectedUncompressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", "ZLib_Uncompressed.bmp"));
            var actualUncompressed = Inflate(compressed);

            Assert.AreEqual(actualUncompressed, expectedUncompressed);
        }

        [TestCase("ZLib_WindowBits8.bin")]
        [TestCase("ZLib_WindowBits15.bin")]
        public async Task InflateAsync(string compressedFileName)
        {
            var compressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", compressedFileName));
            var expectedUncompressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", "ZLib_Uncompressed.bmp"));
            var actualUncompressed = await InflateAsync(compressed);

            Assert.AreEqual(actualUncompressed, expectedUncompressed);
        }

        [TestCase("ZLib_WindowBits8.bin")]
        [TestCase("ZLib_WindowBits15.bin")]
        public async Task InflateAPM(string compressedFileName)
        {
            var compressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", compressedFileName));
            var expectedUncompressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", "ZLib_Uncompressed.bmp"));
            var actualUncompressed = await InflateAPM(compressed);

            Assert.AreEqual(actualUncompressed, expectedUncompressed);
        }

        [Test]
        public void Deflate()
        {
            var uncompressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", "ZLib_Uncompressed.bmp"));

            var compressed = Deflate(uncompressed);
            var actualUncompressed = Inflate(compressed);

            Assert.AreEqual(uncompressed, actualUncompressed);
        }

        [Test]
        public async Task DeflateAsync()
        {
            var uncompressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", "ZLib_Uncompressed.bmp"));

            var compressed = await DeflateAsync(uncompressed);
            var actualUncompressed = await InflateAsync(compressed);

            Assert.AreEqual(uncompressed, actualUncompressed);
        }

        [Test]
        public async Task DeflateAPM()
        {
            var uncompressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", "ZLib_Uncompressed.bmp"));

            var compressed = await DeflateAPM(uncompressed);
            var actualUncompressed = await InflateAPM(compressed);

            Assert.AreEqual(uncompressed, actualUncompressed);
        }

        [Test]
        public void IncorrectChecksum()
        {
            var compressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", "ZLib_WindowBits15.bin"));

            compressed[compressed.Length - 1] = (byte)(1 ^ compressed[compressed.Length - 1]);

            Assert.Throws<InvalidDataException>(() => Inflate(compressed));
        }

        [Test]
        public void IncorrectFcheck()
        {
            var compressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", "ZLib_WindowBits15.bin"));

            compressed[1] = (byte)(compressed[1] ^ 1);

            Assert.Throws<InvalidDataException>(() => Inflate(compressed));
        }


        [Test]
        public void IncorrectAlgorithm()
        {
            var compressed = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", "ZLib_WindowBits15.bin"));

            compressed[0]++;

            Assert.Throws<InvalidDataException>(() => Inflate(compressed));
        }

    }
}
