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

namespace PdfToSvg.Tests.DocumentModel
{
    internal class PdfDictionaryTests
    {
        [Test]
        public void Add_Unique()
        {
            var dict = new PdfDictionary();

            for (var i = 0; i < 1024; i++)
            {
                var key = new PdfName(i.ToString());
                dict.Add(key, i);

                Assert.AreEqual(i + 1, dict.Count, "Count");
                Assert.IsTrue(dict.ContainsKey(key));
                Assert.IsTrue(dict.TryGetValue(key, out var value));
                Assert.AreEqual(i, value, "Value");
            }
        }

        [TestCase(16)]
        [TestCase(32)]
        [TestCase(64)]
        [TestCase(1024)]
        public void Add_Remove_Add(int count)
        {
            var dict = new PdfDictionary();

            // Add initial
            for (var i = 0; i < count; i++)
            {
                var key = new PdfName(i.ToString());
                dict.Add(key, i);
            }

            // Remove
            for (var i = 0; i < 5; i++)
            {
                dict.Remove(new PdfName(i.ToString()));
            }

            // Readd
            for (var i = 0; i < count; i++)
            {
                var key = new PdfName(i.ToString());
                dict.Add(key, i);
            }

            // Ensure they exist
            for (var i = 0; i < count; i++)
            {
                var key = new PdfName(i.ToString());
                Assert.IsTrue(dict.ContainsKey(key));
            }
            Assert.AreEqual(count, dict.Count);
        }

        [Test]
        public void Add_Replace()
        {
            var dict = new PdfDictionary();

            dict.Add(new PdfName("1"), 1);
            dict.Add(new PdfName("2"), 2);
            dict.Add(new PdfName("1"), 3);
            dict.Add(new PdfName("1"), 4);

            Assert.AreEqual(4, dict[new PdfName("1")]);
            Assert.AreEqual(2, dict[new PdfName("2")]);
        }

        [Test]
        public void Add_Remove_DoesNotIncreaseCapacity()
        {
            var dict = new PdfDictionary();

            // Add
            for (var i = 0; i < 32; i++)
            {
                dict.Add(new PdfName(i.ToString(CultureInfo.InvariantCulture)), 1);
            }

            var capacityAfterAdd = dict.Capacity;

            // Remove
            for (var i = 0; i < 32; i++)
            {
                dict.Remove(new PdfName(i.ToString(CultureInfo.InvariantCulture)));
            }

            // Add
            for (var i = 0; i < 32; i++)
            {
                dict.Add(new PdfName(i.ToString(CultureInfo.InvariantCulture)), 1);
            }

            Assert.AreEqual(capacityAfterAdd, dict.Capacity);
        }

        [Test]
        public void Add_Remove_Compaction()
        {
            var dict = new PdfDictionary();

            var expectedValues = new List<int>();

            // Add
            for (var i = 0; i < 32; i++)
            {
                dict.Add(new PdfName(i.ToString(CultureInfo.InvariantCulture)), i);
            }

            // Remove even entries
            for (var i = 0; i < 32; i += 2)
            {
                dict.Remove(new PdfName(i.ToString(CultureInfo.InvariantCulture)));
            }
            for (var i = 1; i < 32; i += 2)
            {
                expectedValues.Add(i);
            }

            // Add new values, causing compaction
            for (var i = 0; i < 32; i++)
            {
                dict.Add(new PdfName((32 + i).ToString(CultureInfo.InvariantCulture)), 32 + i);
                expectedValues.Add(32 + i);
            }

            Assert.AreEqual(expectedValues, dict.Values);
        }

        [Test]
        public void Clear()
        {
            var dict = new PdfDictionary();

            dict.Add(new PdfName("1"), 1);
            dict.Add(new PdfName("2"), 2);
            dict.Add(new PdfName("3"), 3);
            dict.Add(new PdfName("4"), 4);

            dict.Clear();

            Assert.IsFalse(dict.ContainsKey(new PdfName("1")));
            Assert.AreEqual(0, dict.Count, "Count");
        }

        [Test]
        public void TryGetValue()
        {
            var dict = new PdfDictionary();

            dict.Add(new PdfName("1"), 1);
            dict.Add(new PdfName("2"), 2);
            dict.Add(new PdfName("3"), 3);
            dict.Add(new PdfName("4"), 4);

            Assert.IsTrue(dict.TryGetValue(new PdfName("1"), out var value));
            Assert.AreEqual(1, value);

            Assert.IsFalse(dict.TryGetValue(new PdfName("5"), out value));
            Assert.IsNull(value);
        }

        [Test]
        public void GetIndexer()
        {
            var dict = new PdfDictionary();

            dict.Add(new PdfName("1"), 1);
            dict.Add(new PdfName("2"), 2);
            dict.Add(new PdfName("3"), 3);
            dict.Add(new PdfName("4"), 4);

            Assert.AreEqual(1, dict[new PdfName("1")]);
            Assert.AreEqual(null, dict[new PdfName("5")]);
        }

        [Test]
        public void SetIndexer_Unique()
        {
            var dict = new PdfDictionary();

            for (var i = 0; i < 1024; i++)
            {
                var key = new PdfName(i.ToString());
                dict[key] = i;

                Assert.AreEqual(i + 1, dict.Count, "Count");
                Assert.IsTrue(dict.ContainsKey(key));
                Assert.IsTrue(dict.TryGetValue(key, out var value));
                Assert.AreEqual(i, value, "Value");
            }
        }

        [Test]
        public void Remove()
        {
            var dict = new PdfDictionary();

            dict.Add(new PdfName("1"), 1);
            dict.Add(new PdfName("2"), 2);
            dict.Add(new PdfName("3"), 3);
            dict.Add(new PdfName("4"), 4);

            Assert.IsTrue(dict.Remove(new PdfName("2")), "Remove valueless");
            Assert.IsFalse(dict.Remove(new PdfName("2")), "Repeated remove");

            Assert.IsFalse(dict.Remove(new KeyValuePair<PdfName, object>(new PdfName("3"), 2)), "Remove non-matching value");
            Assert.IsTrue(dict.Remove(new KeyValuePair<PdfName, object>(new PdfName("3"), 3)), "Remove non-matching value");
            Assert.IsFalse(dict.Remove(new KeyValuePair<PdfName, object>(new PdfName("3"), 3)), "Repeated remove");

            Assert.IsTrue(dict.ContainsKey(new PdfName("1")));
            Assert.IsFalse(dict.ContainsKey(new PdfName("2")));
            Assert.IsFalse(dict.ContainsKey(new PdfName("3")));
            Assert.IsTrue(dict.ContainsKey(new PdfName("4")));
            Assert.AreEqual(2, dict.Count, "Count");
        }

        [Test]
        public void Remove_Readd()
        {
            var dict = new PdfDictionary();

            dict.Add(new PdfName("1"), 1);
            dict.Add(new PdfName("2"), 2);
            dict.Add(new PdfName("3"), 3);
            dict.Add(new PdfName("4"), 4);

            dict.Remove(new PdfName("2"));
            dict.Add(new PdfName("2"), 3);

            Assert.IsTrue(dict.TryGetValue(new PdfName("2"), out var value));
            Assert.AreEqual(3, value);
            Assert.AreEqual("/1,/3,/4,/2", string.Join(",", dict.Keys));
        }

        [Test]
        public void GetEnumerator()
        {
            var dict = new PdfDictionary();

            dict.Add(new PdfName("1"), 10);
            dict.Add(new PdfName("2"), 20);
            dict.Add(new PdfName("3"), 30);
            dict.Add(new PdfName("4"), 40);

            dict.Remove(new PdfName("2"));
            dict.Remove(new PdfName("3"));

            dict.Add(new PdfName("1"), 50);
            dict.Add(new PdfName("2"), 60);

            var str = string.Join(", ",
                dict.Select(x => x.Key + "=" + ((int)x.Value).ToString(CultureInfo.InvariantCulture)));

            Assert.AreEqual("/1=50, /4=40, /2=60", str);
        }

        [Test]
        public void CopyTo()
        {
            var dict = new PdfDictionary();

            dict.Add(new PdfName("1"), 10);
            dict.Add(new PdfName("2"), 20);
            dict.Add(new PdfName("3"), 30);
            dict.Add(new PdfName("4"), 40);

            dict.Remove(new PdfName("2"));
            dict.Remove(new PdfName("3"));

            dict.Add(new PdfName("1"), 50);
            dict.Add(new PdfName("2"), 60);

            var copy = new KeyValuePair<PdfName, object>[dict.Count + 2];
            ((ICollection<KeyValuePair<PdfName, object>>)dict).CopyTo(copy, 1);

            var str = string.Join(", ",
                copy.Select(x => x.Key + "=" + ((int)(x.Value ?? 99)).ToString(CultureInfo.InvariantCulture)));

            Assert.AreEqual("=99, /1=50, /4=40, /2=60, =99", str);
        }

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

        [TestCase(42.0)]
        [TestCase(42)]
        public void GetSingle(object value)
        {
            var dict = new PdfDictionary();

            dict[new PdfName("name")] = value;

            Assert.IsTrue(dict.TryGetValue<float>(new PdfName("name"), out var actual));
            Assert.AreEqual(42f, actual);

            Assert.AreEqual(42f, dict.GetValueOrDefault(new PdfName("name"), 55f));
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
