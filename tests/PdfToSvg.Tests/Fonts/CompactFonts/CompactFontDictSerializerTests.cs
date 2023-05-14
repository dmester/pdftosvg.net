// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.CompactFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.CompactFonts
{
    internal class CompactFontDictSerializerTests
    {
        private class TestDict
        {
            [CompactFontDictOperator(4, Order = 4)]
            public double Double { get; set; } = 91;

            [CompactFontDictOperator(5, Order = 5)]
            public int Int { get; set; } = 91;

            [CompactFontDictOperator(6, Order = 6)]
            public bool Bool { get; set; }

            [CompactFontDictOperator(7, Order = 7)]
            public string String { get; set; }

            [CompactFontDictOperator(1, Order = 1)]
            public double? NullableDouble { get; set; } = 91;

            [CompactFontDictOperator(2, Order = 2)]
            public int? NullableInt { get; set; } = 91;

            [CompactFontDictOperator(3, Order = 3)]
            public bool? NullableBool { get; set; }

            [CompactFontDictOperator(8, Order = 8)]
            public double[] DoubleArray { get; set; } = new[] { 1d, 2d, 3d };

            [CompactFontDictOperator(9, Order = 9)]
            public int[] IntArray { get; set; } = new[] { 1, 2, 3 };

            [CompactFontDictOperator(10, Order = 10)]
            public bool[] BoolArray { get; set; } = new[] { true, true };

            [CompactFontDictOperator(11, Order = 11)]
            public string[] StringArray { get; set; } = new[] { ".notdef", "space" };
        }

        [Test]
        public void SerializeNull()
        {
            var target = new List<KeyValuePair<int, double[]>>();
            var stringTable = new CompactFontStringTable();

            var source = new TestDict()
            {
                StringArray = null,
                BoolArray = null,
                IntArray = null,
                NullableBool = null,
                NullableDouble = null,
                NullableInt = null,
            };

            CompactFontDictSerializer.Serialize(target, source, new TestDict(), stringTable, false);

            var expectedDict = new List<KeyValuePair<int, double[]>>();
            Assert.AreEqual(expectedDict, target);
            Assert.AreEqual(0, stringTable.GetCustomStrings().Count);
        }

        [Test]
        public void SerializeDefault()
        {
            var target = new List<KeyValuePair<int, double[]>>();
            var stringTable = new CompactFontStringTable();

            CompactFontDictSerializer.Serialize(target, new TestDict(), new TestDict(), stringTable, false);

            var expectedDict = new List<KeyValuePair<int, double[]>>();
            Assert.AreEqual(expectedDict, target);
            Assert.AreEqual(0, stringTable.GetCustomStrings().Count);
        }

        [TestCase(false, 0d)]
        [TestCase(true, 1d)]
        public void SerializeBool(bool input, double expected)
        {
            var target = new List<KeyValuePair<int, double[]>>();
            var stringTable = new CompactFontStringTable();

            var source = new TestDict
            {
                NullableBool = input,
                Bool = input,
                BoolArray = new[] { input, input, false },
            };

            CompactFontDictSerializer.Serialize(target, source, new TestDict(), stringTable, false);

            var expectedDict = new List<KeyValuePair<int, double[]>>();

            expectedDict.Add(new KeyValuePair<int, double[]>(3, new[] { expected }));

            if (input)
            {
                expectedDict.Add(new KeyValuePair<int, double[]>(6, new[] { expected }));
            }

            expectedDict.Add(new KeyValuePair<int, double[]>(10, new[] { expected, expected, 0d }));

            Assert.AreEqual(expectedDict, target);
            Assert.AreEqual(0, stringTable.GetCustomStrings().Count);
        }

        [TestCase(0, 0d)]
        [TestCase(42, 42d)]
        public void SerializeInt(int input, double expected)
        {
            var target = new List<KeyValuePair<int, double[]>>();
            var stringTable = new CompactFontStringTable();

            var source = new TestDict
            {
                NullableInt = input,
                Int = input,
                IntArray = new[] { input, input, 7 },
            };

            CompactFontDictSerializer.Serialize(target, source, new TestDict(), stringTable, false);

            var expectedDict = new List<KeyValuePair<int, double[]>>
            {
                new KeyValuePair<int, double[]>(2, new []{ expected }),
                new KeyValuePair<int, double[]>(5, new []{ expected }),
                new KeyValuePair<int, double[]>(9, new []{ expected, expected, 7d }),
            };
            Assert.AreEqual(expectedDict, target);
            Assert.AreEqual(0, stringTable.GetCustomStrings().Count);
        }

        [TestCase(0d)]
        [TestCase(42000d)]
        public void SerializeDouble(double input)
        {
            var target = new List<KeyValuePair<int, double[]>>();
            var stringTable = new CompactFontStringTable();

            var source = new TestDict
            {
                NullableDouble = input,
                Double = input,
                DoubleArray = new[] { input, input, 0d },
            };

            CompactFontDictSerializer.Serialize(target, source, new TestDict(), stringTable, false);

            var expectedDict = new List<KeyValuePair<int, double[]>>
            {
                new KeyValuePair<int, double[]>(1, new []{ input }),
                new KeyValuePair<int, double[]>(4, new []{ input }),
                new KeyValuePair<int, double[]>(8, new []{ input, input, 0d }),
            };
            Assert.AreEqual(expectedDict, target);
            Assert.AreEqual(0, stringTable.GetCustomStrings().Count);
        }

        [TestCase(".notdef", 0d, true)]
        [TestCase("Semibold", 390d, true)]
        [TestCase("mystr1", 391d, true)]
        [TestCase("mystr2", 392d, true)]
        [TestCase("not a known string", -1d, true)]
        [TestCase("not a known string", 393d, false)]
        public void SerializeString(string input, double expected, bool readOnlyStrings)
        {
            var target = new List<KeyValuePair<int, double[]>>();
            var stringTable = new CompactFontStringTable(new[]
            {
                "mystr1",
                "mystr2",
            });

            var source = new TestDict
            {
                String = input,
                StringArray = new[] { input, input, "oneeighth" },
            };

            CompactFontDictSerializer.Serialize(target, source, new TestDict(), stringTable, readOnlyStrings);

            var expectedDict = new List<KeyValuePair<int, double[]>>
            {
                new KeyValuePair<int, double[]>(7, new []{ expected }),
                new KeyValuePair<int, double[]>(11, new []{ expected, expected, 320d }),
            };
            Assert.AreEqual(expectedDict, target);
        }

        [TestCase(0d, false)]
        [TestCase(0.1d, true)]
        [TestCase(-5d, true)]
        public void DeserializeBool(double input, bool expected)
        {
            var target = new TestDict();
            var stringTable = new CompactFontStringTable();

            CompactFontDictSerializer.Deserialize(target, new Dictionary<int, double[]>
            {
                { 3, new[] { input } },
                { 6, new[] { input } },
                { 10, new[] { input, input, input } },
            }, stringTable);

            Assert.AreEqual(expected, target.Bool);
            Assert.AreEqual(expected, target.NullableBool);
            Assert.AreEqual(new[] { expected, expected, expected }, target.BoolArray);
        }

        [TestCase(0d, 0)]
        [TestCase(0.1d, 0)]
        [TestCase(-5d, -5)]
        [TestCase(12400d, 12400)]
        public void DeserializeInt(double input, int expected)
        {
            var target = new TestDict();
            var stringTable = new CompactFontStringTable();

            CompactFontDictSerializer.Deserialize(target, new Dictionary<int, double[]>
            {
                { 2, new[] { input } },
                { 5, new[] { input } },
                { 9, new[] { input, input, input } },
            }, stringTable);

            Assert.AreEqual(expected, target.Int);
            Assert.AreEqual(expected, target.NullableInt);
            Assert.AreEqual(new[] { expected, expected, expected }, target.IntArray);
        }

        [Test]
        public void DeserializeDouble()
        {
            var target = new TestDict();
            var stringTable = new CompactFontStringTable();

            CompactFontDictSerializer.Deserialize(target, new Dictionary<int, double[]>
            {
                { 1, new[] { 0.1d } },
                { 4, new[] { 0.1d } },
                { 8, new[] { 99d, 999d } },
            }, stringTable);

            Assert.AreEqual(0.1d, target.Double);
            Assert.AreEqual(0.1d, target.NullableDouble);
            Assert.AreEqual(new[] { 99d, 999d }, target.DoubleArray);
        }

        [Test]
        public void DeserializeString()
        {
            var target = new TestDict();
            var stringTable = new CompactFontStringTable();

            CompactFontDictSerializer.Deserialize(target, new Dictionary<int, double[]>
            {
                { 7, new[] { 213d } },
                { 11, new[] { 390d, 0d, 3900d } },
            }, stringTable);

            Assert.AreEqual("idieresis", target.String);
            Assert.AreEqual(new[] { "Semibold", ".notdef", null }, target.StringArray);
        }

        [Test]
        public void DeserializeNull()
        {
            var target = new TestDict();
            var stringTable = new CompactFontStringTable();

            CompactFontDictSerializer.Deserialize(target, new Dictionary<int, double[]>
            {
                { 1, new double[0] },
                { 2, new double[0] },
                { 3, new double[0] },
                { 4, new double[0] },
                { 5, new double[0] },
                { 6, new double[0] },
                { 7, new double[0] },
            }, stringTable);

            Assert.AreEqual(null, target.String);
            Assert.AreEqual(null, target.NullableDouble);
            Assert.AreEqual(null, target.NullableInt);
            Assert.AreEqual(null, target.NullableBool);
            Assert.AreEqual(double.NaN, target.Double);
            Assert.AreEqual(0, target.Int);
            Assert.AreEqual(false, target.Bool);
        }

        [Test]
        public void DeserializeNotDef()
        {
            var target = new TestDict();
            var stringTable = new CompactFontStringTable();

            CompactFontDictSerializer.Deserialize(target, new Dictionary<int, double[]>(), stringTable);

            Assert.AreEqual(null, target.String);
            Assert.AreEqual(91, target.NullableDouble);
            Assert.AreEqual(91, target.NullableInt);
            Assert.AreEqual(null, target.NullableBool);
            Assert.AreEqual(91, target.Double);
            Assert.AreEqual(91, target.Int);
            Assert.AreEqual(false, target.Bool);
        }
    }
}
