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
    public class AsciiHexDecodeTests
    {
        const string Compressed =
            " 4D616E2069732064697374696E677569736865642C206E6F74206F6E6C79206279206869" +
            "7320726561736F6E2C2062757420627920746869732073696E67756C6172207061737369  " +
            "6F6E2066726F6D206F7468657220616E696D616C732C2077686963682069732061206C757" +
            "374206f6620746865206d69 6e642c2074\t6861742062792061207065727365766572616e636" +
            "5206f662064656c69676874 20696e2074\t686520636f6e74696e75656420616e6420696e646" +
            "5666174696761626c652067 656e65726174\n696f6\re206f66206b6e6f776c656467652c206578" +
            "63656564732074686520736 86F727420766568656D656E6365206F6620616E79206361726E" +
            "616C20706C6561737572652E 00 4 >";

        const string Raw =
            "Man is distinguished, not only by his reason, but by this singular passion from other animals, " +
            "which is a lust of the mind, that by a perseverance of delight in the continued and " +
            "indefatigable generation of knowledge, exceeds the short vehemence of any carnal pleasure.\0@";

        [Test]
        public void DetectLengthEodMarker()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes("1234567 89abcdef ABCDEF >ABC"));
            Assert.AreEqual(25, Filter.AsciiHexDecode.DetectStreamLength(sourceStream));
        }

        [Test]
        public void DetectLengthEndOfStream()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes("  123 456 789 "));
            Assert.AreEqual(14, Filter.AsciiHexDecode.DetectStreamLength(sourceStream));
        }

        [Test]
        public void DetectLengthMissingEodMarker()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes("A BC DEF { this is not part of the stream"));
            Assert.AreEqual(9, Filter.AsciiHexDecode.DetectStreamLength(sourceStream));
        }

        [Test]
        public void DecodeSmallReadBuffer()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes(Compressed));

            var decodeStream = new AsciiHexDecodeStream(sourceStream);

            var decodedBuffer = new byte[2000];
            var decodedLength = decodeStream.Read(decodedBuffer, 0, decodedBuffer.Length);

            var decodedStr = Encoding.ASCII.GetString(decodedBuffer, 0, decodedLength);
            Assert.AreEqual(Raw, decodedStr);
        }

        [Test]
        public void DecodeSmallDestinationBuffer()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes(Compressed));

            var decodeStream = new AsciiHexDecodeStream(sourceStream);

            var decodedBuffer = new byte[2000];
            var decodedLength = 0;

            int readThisIteration;
            do
            {
                readThisIteration = decodeStream.Read(decodedBuffer, decodedLength, 7);
                decodedLength += readThisIteration;
            }
            while (readThisIteration > 0);

            var decodedStr = Encoding.ASCII.GetString(decodedBuffer, 0, decodedLength);
            Assert.AreEqual(Raw, decodedStr);
        }

        [Test]
        public void Invalid()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes(
                " 012345 . 678  9abc       defABCDE>"));

            var decodeStream = new AsciiHexDecodeStream(sourceStream);

            Assert.Throws<FilterException>(() =>
            {
                var decodedBuffer = new byte[2000];
                var decodedLength = decodeStream.Read(decodedBuffer, 0, decodedBuffer.Length);
            });
        }
    }
}
