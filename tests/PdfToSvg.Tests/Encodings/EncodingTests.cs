using NUnit.Framework;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Encodings
{
    class EncodingTests
    {
        [Test]
        public void WinAnsi()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var source =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!#¤%&/()=?" +
                "ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ";

            var winAnsi = new WinAnsiEncoding();
            var net1252 = Encoding.GetEncoding(1252);

            Assert.AreEqual(source, net1252.GetString(winAnsi.GetBytes(source)));
            Assert.AreEqual(source, winAnsi.GetString(net1252.GetBytes(source)));

            Assert.AreEqual("••", winAnsi.GetString(new byte[] { 0x81, 0x8d }));
        }

        [Test]
        public void MacRoman()
        {
            var source =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!#¤%&/()=?" +
                "ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÑÒÓÔÕÖØÙÚÛÜßàáâãäåæçèéêëìíîïñòóôõö÷øùúûüÿ";

            var macRoman = new MacRomanEncoding();

            Assert.AreEqual(source, macRoman.GetString(macRoman.GetBytes(source)));
            Assert.AreEqual("ÊËÎ", macRoman.GetString(new byte[] { 0xe6, 0xe8, 0xeb }));
            Assert.AreEqual("\ufffd\ufffd", macRoman.GetString(new byte[] { 1, 2 }));
        }

        [Test]
        public void Standard()
        {
            var source = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!#¤%&/()=?";

            var standard = new StandardEncoding();

            Assert.AreEqual(source, standard.GetString(standard.GetBytes(source)));
            Assert.AreEqual("ÆŁæ", standard.GetString(new byte[] { 0xe1, 0xe8, 0xf1 }));
            Assert.AreEqual("\ufffd\ufffd", standard.GetString(new byte[] { 0x81, 0x8d }));
        }

        [Test]
        public void PdfDocEncoding()
        {
            var source = 
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!#¤%&/()=?" +
                "ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÑÒÓÔÕÖ×ØÙÚÛÜßàáâãäåæçèéêëìíîïñòóôõö÷øùúûüÿ";

            var pdfDoc = new PdfDocEncoding();

            Assert.AreEqual(source, pdfDoc.GetString(pdfDoc.GetBytes(source)));
            Assert.AreEqual("œž¥", pdfDoc.GetString(new byte[] { 0x9c, 0x9e, 0xa5 }));
            Assert.AreEqual("\ufffd\ufffd", pdfDoc.GetString(new byte[] { 1, 2 }));
        }
    }
}
