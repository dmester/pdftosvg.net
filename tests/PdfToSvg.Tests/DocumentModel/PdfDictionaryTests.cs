// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.DocumentModel
{
    internal class PdfDictionaryTests
    {
        [Test]
        public void GetStream()
        {
            var dict = new PdfDictionary();

            var subdictWithStream = new PdfDictionary();
            var subdictWithoutStream = new PdfDictionary();

            var stream = new PdfMemoryStream(subdictWithStream, new byte[0], 0);
            subdictWithStream.MakeIndirectObject(new PdfObjectId(), stream);

            dict[new PdfName("withstream")] = subdictWithStream;
            dict[new PdfName("withoutstream")] = subdictWithoutStream;

            Assert.IsFalse(dict.TryGetStream(new PdfName("withoutstream22"), out var _));
            Assert.IsFalse(dict.TryGetStream(new PdfName("withoutstream"), out var _));
            Assert.IsTrue(dict.TryGetStream(new PdfName("withstream"), out var actual));
            Assert.AreEqual(stream, actual);
        }

        [TestCase(42.1)]
        [TestCase(42)]
        public void GetInteger(object value)
        {
            var dict = new PdfDictionary();

            dict[new PdfName("name")] = value;

            Assert.IsTrue(dict.TryGetValue<int>(new PdfName("name"), out var actual));
            Assert.AreEqual(42, actual);

            Assert.IsTrue(dict.TryGetInteger(new PdfName("name"), out actual));
            Assert.AreEqual(42, actual);

            Assert.AreEqual(42, dict.GetValueOrDefault(new PdfName("name"), 55));
        }

        [TestCase(42.0)]
        [TestCase(42)]
        public void GetDouble(object value)
        {
            var dict = new PdfDictionary();

            dict[new PdfName("name")] = value;

            Assert.IsTrue(dict.TryGetValue<double>(new PdfName("name"), out var actual));
            Assert.AreEqual(42.0, actual);

            Assert.AreEqual(42.0, dict.GetValueOrDefault(new PdfName("name"), 55.0));
        }

        [TestCase(1.0d, 2, 3, 4, 5, 6)]
        [TestCase(1, 2, 3, 4, 5, 6)]
        public void GetMatrix(object[] arr)
        {
            var dict = new PdfDictionary();

            dict[new PdfName("name")] = arr;

            var expected = new Matrix(1, 2, 3, 4, 5, 6);

            Assert.IsTrue(dict.TryGetValue<Matrix>(new PdfName("name"), out var actual));
            Assert.AreEqual(expected, actual);

            Assert.AreEqual(expected, dict.GetValueOrDefault(new PdfName("name"), Matrix.Identity));
        }

        [TestCase(1.0d, 2.1d, 3.1d)]
        [TestCase(1, 2.1d, 3.1d)]
        public void GetMatrix1x3(object[] arr)
        {
            var dict = new PdfDictionary();

            dict[new PdfName("name")] = arr;

            var expected = new Matrix1x3(1f, 2.1f, 3.1f);

            Assert.IsTrue(dict.TryGetValue<Matrix1x3>(new PdfName("name"), out var actual));
            Assert.AreEqual(expected, actual);

            Assert.AreEqual(expected, dict.GetValueOrDefault(new PdfName("name"), new Matrix1x3()));
        }

        [TestCase(1.0d, 2.1d, 3.1d, 4d, 5.1d, 6.1d, 7d, 8.1d, 9.1d)]
        [TestCase(1, 2.1d, 3.1d, 4, 5.1d, 6.1d, 7, 8.1d, 9.1d)]
        public void GetMatrix3x3(object[] arr)
        {
            var dict = new PdfDictionary();

            dict[new PdfName("name")] = arr;

            var expected = new Matrix3x3(1f, 2.1f, 3.1f, 4f, 5.1f, 6.1f, 7f, 8.1f, 9.1f);

            Assert.IsTrue(dict.TryGetValue<Matrix3x3>(new PdfName("name"), out var actual));
            Assert.AreEqual(expected, actual);

            Assert.AreEqual(expected, dict.GetValueOrDefault(new PdfName("name"), new Matrix3x3()));
        }

        [TestCase("2021-05-03 15:27:12 +00:00", "D:20210503152712+00'00'")]
        [TestCase("2021-05-03 15:27:12 +00:00", "D:20210503152712+00'00")]
        [TestCase("2021-05-03 15:27:12 +00:00", "D:20210503152712-00'00")]
        [TestCase("2021-05-03 15:27:12 +00:00", "D:20210503152712Z")]
        [TestCase("2021-05-03 15:27:12 -12:00", "D:20210503152712-12'00'")]
        [TestCase("2021-05-03 15:27:12 -12:00", "D:20210503152712-12'00")]
        [TestCase("2021-05-03 15:27:12 +12:00", "D:20210503152712+12'00")]
        [TestCase("2021-05-03 15:27:12 +12:00", "D:20210503152712+12'")]
        [TestCase("2021-05-03 15:27:12 +12:00", "D:20210503152712+12")]
        [TestCase("2021-05-03 15:27:12 +00:00", "D:20210503152712")]
        [TestCase("2021-05-03 15:27:00 +00:00", "D:202105031527")]
        [TestCase("2021-05-03 15:00:00 +00:00", "D:2021050315")]
        [TestCase("2021-05-03 00:00:00 +00:00", "D:20210503")]
        [TestCase("2021-05-01 00:00:00 +00:00", "D:202105")]
        [TestCase("2021-01-01 00:00:00 +00:00", "D:2021")]
        public void GetValidDate(string expectedDateTimeOffset, string input)
        {
            var expected = DateTimeOffset.Parse(expectedDateTimeOffset, CultureInfo.InvariantCulture);

            var dict = new PdfDictionary();

            dict[new PdfName("name")] = input;

            Assert.IsTrue(dict.TryGetValue<DateTimeOffset>(new PdfName("name"), out var actual));
            Assert.AreEqual(expected, actual);
        }

        [TestCase("20210503152712+00'00")]
        [TestCase("D:20210503152712+00'00x")]
        [TestCase("D:20210503152712+0000")]
        [TestCase("D:2021050315271200")]
        [TestCase("D:20210503152760")]
        [TestCase("D:2021050315271")]
        [TestCase("D:20210503152")]
        [TestCase("D:202105031")]
        [TestCase("D:20210500")]
        [TestCase("D:20210501x")]
        [TestCase("D:20210431")]
        [TestCase("D:202113")]
        [TestCase("D:20211")]
        [TestCase("D:202")]
        [TestCase("D:2x")]
        public void GetInvalidDate(string input)
        {
            var dict = new PdfDictionary();

            dict[new PdfName("name")] = input;

            Assert.IsFalse(dict.TryGetValue<DateTimeOffset>(new PdfName("name"), out var _));
        }

        [TestCase("Alternate", true)]
        [TestCase("Decode/DescendantFonts/FIRST/DeviceCMYK", 42)]
        [TestCase("Decode/DescendantFonts/LAST/DeviceCMYK", 84)]
        [TestCase("Decode/Differences/FIRST/DeviceCMYK", null)]
        [TestCase("Decode/Differences/LAST/DeviceCMYK", null)]
        [TestCase("Decode/NotAProp/FIRST/DeviceCMYK", null)]
        [TestCase("Decode/NotAProp/LAST/DeviceCMYK", null)]
        public void GetPath(string path, object expectedValue)
        {
            var namePath = new PdfNamePath(path
                .Replace("FIRST", Indexes.First.Value)
                .Replace("LAST", Indexes.Last.Value)
                .Split('/')
                .Select(name => new PdfName(name))
                .ToArray());

            var dict = new PdfDictionary
            {
                { Names.Alternate, true },
                { Names.Decode, new PdfDictionary {
                    { Names.DescendantFonts, new object[] {
                        new PdfDictionary
                        {
                            { Names.DeviceCMYK, 42 },
                        },
                        new PdfDictionary
                        {
                            { Names.DeviceCMYK, 84 },
                        }
                    } },
                    { Names.Differences, new object[0] },
                } }
            };

            if (expectedValue == null)
            {
                Assert.IsFalse(dict.TryGetValue(namePath, out var actualValue), "TryGetValue");
            }
            else
            {
                Assert.IsTrue(dict.TryGetValue(namePath, out var actualValue), "TryGetValue");
                Assert.AreEqual(expectedValue, actualValue);
            }
        }
    }
}
