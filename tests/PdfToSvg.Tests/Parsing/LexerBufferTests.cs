// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Parsing;
using System.Collections.Generic;

namespace PdfToSvg.Tests.Parsing
{
    public class LexerBufferTests
    {
        [Test]
        public void WriteByte_WithinInitialCapacity()
        {
            var buffer = new LexerBuffer(10);

            buffer.WriteByte((byte)'A');
            buffer.WriteByte((byte)'B');
            buffer.WriteByte((byte)'C');

            Assert.AreEqual("ABC", buffer.ToString());
            Assert.AreEqual("ABC", buffer.ToPdfString().ToString());
        }

        [Test]
        public void WriteChar_WithinInitialCapacity()
        {
            var buffer = new LexerBuffer(10);

            buffer.WriteChar('A');
            buffer.WriteChar('B');
            buffer.WriteChar('C');

            Assert.AreEqual("ABC", buffer.ToString());
            Assert.AreEqual("ABC", buffer.ToPdfString().ToString());
        }

        [Test]
        public void WriteByte_BeyondInitialCapacity()
        {
            var buffer = new LexerBuffer(1);

            var chars = new List<char>();

            for (var i = 0; i < 100; i++)
            {
                for (var ch = 'A'; ch <= 'Z'; ch++)
                {
                    chars.Add(ch);
                    buffer.WriteByte((byte)ch);
                }
            }

            var expected = new string(chars.ToArray());
            Assert.AreEqual(expected, buffer.ToString());
            Assert.AreEqual(expected, buffer.ToPdfString().ToString());
        }

        [Test]
        public void WriteChar_BeyondInitialCapacity()
        {
            var buffer = new LexerBuffer(1);

            var chars = new List<char>();

            for (var i = 0; i < 100; i++)
            {
                for (var ch = 'A'; ch <= 'Z'; ch++)
                {
                    chars.Add(ch);
                    buffer.WriteChar(ch);
                }
            }

            var expected = new string(chars.ToArray());
            Assert.AreEqual(expected, buffer.ToString());
            Assert.AreEqual(expected, buffer.ToPdfString().ToString());
        }
    }
}
