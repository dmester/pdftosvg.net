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
    public class Ascii85DecodeTests
    {
        const string Compressed = 
            "  9jqo^BlbD-BleB1DJ+*+F(f,q/0JhKF<GL>Cj@.4Gp$d7F!,L7@<6@)/0JDEF<G%<+EV:2F!," +
            "O<DJ+*.@<*K0@  <6L(Df-\\0Ec5e;DffZ(EZee.Bl.9pF\"AGXBPCsi+DGm>@3BB/F*&OCAfu2/AKY" +
            "i(DIb:@FD,*)+C]U=@3BN#EcYf8ATD3s@q?d$AftVqCh[NqF<G:8+EV:.+Cf>-FD5W8ARlolDIa" +
            "l(DId<j@<?3r@:F%a+D58'ATD4$Bl@l3De  :,-DJs`8ARoFb/0JMK@qB4^F!,R<AKZ&-DfTqBG  %G" +
            ">uD.RTpAKYo'+CT/5+Cei#DII?(E,9)oF*2M7/c~>";

        const string Raw =
            "Man is distinguished, not only by his reason, but by this singular passion from other animals, " +
            "which is a lust of the mind, that by a perseverance of delight in the continued and " + 
            "indefatigable generation of knowledge, exceeds the short vehemence of any carnal pleasure.";

        [Test]
        public void DetectLengthDividedEodMarker()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes("zzz~ >zzz"));
            Assert.AreEqual(6, Filter.Ascii85Decode.DetectStreamLength(sourceStream));
        }

        [Test]
        public void DetectLengthFullEodMarker()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes("zzz~>zzz"));
            Assert.AreEqual(5, Filter.Ascii85Decode.DetectStreamLength(sourceStream));
        }

        [Test]
        public void DetectLengthHalfEodMarker()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes("zzz~ zzz"));
            Assert.AreEqual(5, Filter.Ascii85Decode.DetectStreamLength(sourceStream));
        }

        [Test]
        public void DetectLengthMissingEodMarker()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes("zzz { this is not part of the stream"));
            Assert.AreEqual(4, Filter.Ascii85Decode.DetectStreamLength(sourceStream));
        }

        [Test]
        public void DetectLengthEndOfStream()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes("Ib:@FD,*)+C]U=@3BN#EcYf8ATD3s@q  "));
            Assert.AreEqual(33, Filter.Ascii85Decode.DetectStreamLength(sourceStream));
        }

        [Test]
        public void DecodeZeroes()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes("zzzzzzzzzzzzzzzzzzzzzzzzz"));

            var decodeStream = new Ascii85DecodeStream(sourceStream, 10);

            var decodedBuffer = new byte[2000];
            var decodedLength = decodeStream.Read(decodedBuffer, 0, decodedBuffer.Length);

            Assert.AreEqual(25 * 4, decodedLength);
        }

        [Test]
        public void DecodeSmallReadBuffer()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes(Compressed));

            var decodeStream = new Ascii85DecodeStream(sourceStream, 10);

            var decodedBuffer = new byte[2000];
            var decodedLength = decodeStream.Read(decodedBuffer, 0, decodedBuffer.Length);

            var decodedStr = Encoding.ASCII.GetString(decodedBuffer, 0, decodedLength);
            Assert.AreEqual(Raw, decodedStr);
        }

        [Test]
        public void DecodeSmallDestinationBuffer()
        {
            var sourceStream = new MemoryStream(Encoding.ASCII.GetBytes(Compressed));

            var decodeStream = new Ascii85DecodeStream(sourceStream);

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
                "  9jqo^Blb\u0004\u0003D-Bl~>"));

            var decodeStream = new Ascii85DecodeStream(sourceStream);

            Assert.Throws<FilterException>(() =>
            {
                var decodedBuffer = new byte[2000];
                var decodedLength = decodeStream.Read(decodedBuffer, 0, decodedBuffer.Length);
            });
        }
    }
}
