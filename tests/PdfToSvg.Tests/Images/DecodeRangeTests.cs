// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images
{
    internal class DecodeRangeTests
    {
        [Test]
        public void Decode32Bit()
        {
            var range = new DecodeRange(.2f, .8f, 32);

            Assert.That(range.Decode(0), Is.EqualTo(.2f).Within(0.00001f));
            Assert.That(range.Decode((double)uint.MaxValue), Is.EqualTo(0.8).Within(0.00001f));
        }

        [Test]
        public void Decode31Bit()
        {
            var range = new DecodeRange(.2f, .8f, 31);

            Assert.That(range.Decode(0), Is.EqualTo(.2f).Within(0.00001f));
            Assert.That(range.Decode((double)(uint.MaxValue >> 1)), Is.EqualTo(0.8).Within(0.00001f));
        }

        [Test]
        public void Decode1Bit()
        {
            var range = new DecodeRange(.2f, .8f, 1);

            Assert.That(range.Decode(0), Is.EqualTo(.2f).Within(0.00001f));
            Assert.That(range.Decode(1), Is.EqualTo(.8f).Within(0.00001f));
        }
    }
}
