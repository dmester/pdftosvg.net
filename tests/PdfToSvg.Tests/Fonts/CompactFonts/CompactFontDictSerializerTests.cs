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
            [CompactFontDictOperator(1)]
            public double? NullableDouble { get; set; } = 91;

            [CompactFontDictOperator(2)]
            public int? NullableInt { get; set; } = 91;

            [CompactFontDictOperator(3)]
            public bool? NullableBool { get; set; }

            [CompactFontDictOperator(4)]
            public double Double { get; set; } = 91;

            [CompactFontDictOperator(5)]
            public int Int { get; set; } = 91;

            [CompactFontDictOperator(6)]
            public bool Bool { get; set; }

            [CompactFontDictOperator(7)]
            public string String { get; set; }

            [CompactFontDictOperator(8)]
            public double[] DoubleArray { get; set; }

            [CompactFontDictOperator(9)]
            public int[] IntArray { get; set; }

            [CompactFontDictOperator(10)]
            public bool[] BoolArray { get; set; }

            [CompactFontDictOperator(11)]
            public string[] StringArray { get; set; }
        }

        [TestCase(0d, false)]
        [TestCase(0.1d, true)]
        [TestCase(-5d, true)]
        public void Bool(double input, bool expected)
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
        public void Int(double input, int expected)
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
        public void Double()
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
        public void String()
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
        public void Null()
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
        public void NotDef()
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
