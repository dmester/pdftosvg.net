// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PdfToSvg.Tests.Security
{
    public class ArcFourTests
    {
        [Test]
        public void TransformBlock_InvalidParams()
        {
            var buffer = new byte[10];
            var transform = new ArcFourTransform(new byte[10]);

            Assert.Throws<ArgumentNullException>(() => transform.TransformBlock(null, 0, 10, buffer, 0));
            Assert.Throws<ArgumentNullException>(() => transform.TransformBlock(buffer, 0, 10, null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => transform.TransformBlock(buffer, -1, 10, buffer, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => transform.TransformBlock(buffer, 0, 11, buffer, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => transform.TransformBlock(buffer, 0, -1, buffer, 0));
        }

        [Test]
        public void TransformFinalBlock_InvalidParams()
        {
            var buffer = new byte[10];
            var transform = new ArcFourTransform(new byte[10]);

            Assert.Throws<ArgumentNullException>(() => transform.TransformFinalBlock(null, 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => transform.TransformFinalBlock(buffer, -1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => transform.TransformFinalBlock(buffer, 0, 11));
            Assert.Throws<ArgumentOutOfRangeException>(() => transform.TransformFinalBlock(buffer, 0, -1));
        }

        // Test vectors from:
        // https://en.wikipedia.org/wiki/RC4#Test_vectors
        [TestCase("Key", "Plaintext", "BBF316E8D940AF0AD3")]
        [TestCase("Wiki", "pedia", "1021BF0420")]
        [TestCase("Secret", "Attack at dawn", "45A01F645FC35B383552544B9BF5")]
        public void Transform(string key, string plaintext, string ciphertext)
        {
            var binaryKey = Encoding.ASCII.GetBytes(key);
            var binaryPlaintext = Encoding.ASCII.GetBytes(plaintext);
            var binaryCiphertext = new byte[ciphertext.Length / 2];

            for (var i = 0; i < ciphertext.Length; i += 2)
            {
                binaryCiphertext[i / 2] = byte.Parse(ciphertext.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            var encrypted = Transform(binaryKey, binaryPlaintext);
            var decrypted = Transform(binaryKey, binaryCiphertext);

            Assert.AreEqual(binaryCiphertext, encrypted);
            Assert.AreEqual(binaryPlaintext, decrypted);
        }

        private byte[] Transform(byte[] key, byte[] input)
        {
            using var arcFour = new ArcFour();
            arcFour.Key = key;

            using var stream = new MemoryStream();
            using (var transformStream = new CryptoStream(stream, arcFour.CreateEncryptor(), CryptoStreamMode.Write))
            {
                transformStream.Write(input, 0, input.Length);
            }

            return stream.ToArray();
        }
    }
}
