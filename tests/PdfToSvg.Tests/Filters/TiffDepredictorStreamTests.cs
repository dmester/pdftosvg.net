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
    public class TiffDepredictorStreamTests
    {
        [Test]
        public void Depredict8bit()
        {
            var encoded = new byte[]
            {
                255, 20, 30, 41, 236, 225, 30, 80, 0,
                74, 129, 241, 182, 127, 15, 255, 255, 255,
            };
            var expectedDecoded = new byte[]
            {
                255, 20, 30, 40, 0, 255, 70, 80, 255,
                74, 129, 241, 0, 0, 0, 255, 255, 255,
            };

            var sourceStream = new MemoryStream(encoded);

            var decodeStream = new TiffDepredictorStream(sourceStream, colors: 3, bitsPerComponent: 8, columns: 3, bufferSize: 1);

            var decodedBuffer = new byte[encoded.Length];
            var decodedLength = decodeStream.Read(decodedBuffer, 0, decodedBuffer.Length);

            Assert.AreEqual(encoded.Length, decodedLength);
            Assert.AreEqual(expectedDecoded, decodedBuffer);
        }

        [Test]
        public void Depredict1bit()
        {
            var encoded = new byte[]
            {
                0b_11100001, 0b_01111010,
                0b_11111110, 0b_00010010,
            };
            var expectedDecoded = new byte[]
            {
                0b_11111110, 0b_10101110,
                0b_11100010, 0b_01011000,
            };

            var sourceStream = new MemoryStream(encoded);

            var decodeStream = new TiffDepredictorStream(sourceStream, colors: 3, bitsPerComponent: 1, columns: 5, bufferSize: 1);

            var decodedBuffer = new byte[encoded.Length];
            var decodedLength = decodeStream.Read(decodedBuffer, 0, decodedBuffer.Length);

            Assert.AreEqual(encoded.Length, decodedLength);
            Assert.AreEqual(expectedDecoded, decodedBuffer);
        }

        private static byte[] Serialize16bit(ushort[] input)
        {
            var output = new byte[input.Length * 2];
            for (var i = 0; i < input.Length; i++)
            {
                output[i * 2 + 0] = unchecked((byte)(input[i] >> 8));
                output[i * 2 + 1] = unchecked((byte)input[i]);
            }
            return output;
        }

        [Test]
        public void Depredict16bit()
        {
            var encoded = Serialize16bit(new ushort[]
            {
                41254, 52142, 65535, 0, 13179, 1, 25527, 240, 255,
                0, 0, 0, 65534, 12, 4257, 2, 65524, 61279,
            });
            var expectedDecoded = Serialize16bit(new ushort[]
            {
                41254, 52142, 65535, 41254, 65321, 0, 1245, 25, 255,
                0, 0, 0, 65534, 12, 4257, 0, 0, 0,
            });

            var sourceStream = new MemoryStream(encoded);

            var decodeStream = new TiffDepredictorStream(sourceStream, colors: 3, bitsPerComponent: 16, columns: 3, bufferSize: 1);

            var decodedBuffer = new byte[encoded.Length];
            var decodedLength = decodeStream.Read(decodedBuffer, 0, decodedBuffer.Length);

            Assert.AreEqual(encoded.Length, decodedLength);
            Assert.AreEqual(expectedDecoded, decodedBuffer);
        }

    }
}
