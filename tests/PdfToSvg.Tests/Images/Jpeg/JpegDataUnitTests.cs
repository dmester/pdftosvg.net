// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal class JpegDataUnitTests
    {
        [Test]
        public void DataUnit1()
        {
            var block = new short[64];
            block[0] = 122;
            block[1] = 13;
            block[2] = 14;
            block[63] = 12;
            DataUnitRoundtrip(block);
        }

        [Test]
        public void DataUnit2()
        {
            var block = new short[64];
            block[0] = -12;
            block[1] = 13;
            block[2] = -14;
            block[9] = 12;
            DataUnitRoundtrip(block);
        }

        [Test]
        public void DataUnit3()
        {
            var block = new short[64];
            block[0] = 12;
            block[1] = 13;
            block[2] = -140;
            block[20] = 1;
            DataUnitRoundtrip(block);
        }

        [Test]
        public void DataUnit4()
        {
            var block = new short[64];
            block[0] = 12;
            DataUnitRoundtrip(block);
        }

        [Test]
        public void DataUnit5()
        {
            var block = new short[64];
            block[1] = 12;
            DataUnitRoundtrip(block);
        }

        [Test]
        public void DataUnit6()
        {
            var block = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                block[i] = (short)random.Next(-128, 300);
            }

            DataUnitRoundtrip(block);
        }

        private void DataUnitRoundtrip(short[] block)
        {
            var stream = new MemoryStream();
            var writer = new JpegImageDataWriter(stream);

            writer.WriteDataUnit(block, JpegHuffmanTable.DefaultLuminanceDCTable, JpegHuffmanTable.DefaultLuminanceACTable);
            writer.Dispose();

            var buff = stream.ToArray();

            var reader = new JpegImageDataReader(buff, 0, buff.Length);

            var block2 = new short[64];

            reader.ReadDataUnit(block2, JpegHuffmanTable.DefaultLuminanceDCTable, JpegHuffmanTable.DefaultLuminanceACTable);

            Assert.AreEqual(block, block2);
        }
    }
}
