using NUnit.Framework;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Images
{
    class ComponentReaderTests
    {
        [Test]
        public void Read1bitValues()
        {
            var stream = new MemoryStream(new byte[] { 0b01010111, 0b10110111 });

            var reader = ComponentReader.Create(stream, 1, 2);
            var buffer = new float[18];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(16, read);
            Assert.AreEqual(new[] {
                0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f,
                1f, 0f, 1f, 1f, 0f, 1f, 1f, 1f,
                0f, 0f,
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read2bitValues()
        {
            var stream = new MemoryStream(new byte[] { 0b01010100, 0b10110111 });

            var reader = ComponentReader.Create(stream, 2, 2);
            var buffer = new float[10];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(8, read);
            Assert.AreEqual(new[] {
                1f, 1f, 1f, 0f,
                2f, 3f, 1f, 3,
                0f, 0f,
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read4bitValues()
        {
            var stream = new MemoryStream(new byte[] { 0b01010100, 0b10110111 });

            var reader = ComponentReader.Create(stream, 4, 2);
            var buffer = new float[10];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(4, read);
            Assert.AreEqual(new[] {
                5f, 4f, 11f, 7f,
                0f, 0f, 0f, 0f, 0f, 0f,
            }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read8bitValues()
        {
            var stream = new MemoryStream(new byte[] { 255, 0, 127, 129 });

            var reader = ComponentReader.Create(stream, 8, 2);
            var buffer = new float[5];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(4, read);
            Assert.AreEqual(new[] { 255f, 0f, 127f, 129f, 0f }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Read16bitValues()
        {
            var stream = new MemoryStream(new byte[] { 255, 255, 0, 0, 127, 129 });

            var reader = ComponentReader.Create(stream, 16, 2);
            var buffer = new float[4];
            var read = reader.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(3, read);
            Assert.AreEqual(new[] { 65535f, 0f, 32641f, 0f }, buffer);

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }
    }
}
