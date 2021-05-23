using NUnit.Framework;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Images
{
    class DecodeArrayTests
    {
        [Test]
        public void DecodeValues()
        {
            var decodeArray = new DecodeArray(4, new[] { .2f, .8f, 0f, 2f });

            var data = new[] { 15f, 0f, 7f, 6f, 9f };
            decodeArray.Decode(data, 0, data.Length);

            Assert.That(data, Is.EqualTo(new[] { .8f, 0f, .48f, .8f, .56f }).Within(0.00001f));
        }

        [Test]
        public void DecodeInvertedValues()
        {
            var decodeArray = new DecodeArray(4, new[] { .8f, .2f, 2f, 0f });

            var data = new[] { 15f, 0f, 7f, 6f, 9f };
            decodeArray.Decode(data, 0, data.Length);

            Assert.That(data, Is.EqualTo(new[] { .2f, 2f, .52f, 1.2f, .44f }).Within(0.00001f));
        }
    }
}
