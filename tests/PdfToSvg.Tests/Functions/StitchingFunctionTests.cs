// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Functions
{
    public class StitchingFunctionTests
    {
        [TestCase(-2d, 8d)]
        [TestCase(0.2d, 10d)]
        [TestCase(0.4d, 42.8d, 43.8d)]
        [TestCase(0.5d, 44d, 45d)]
        [TestCase(0.6d, 102d)]
        [TestCase(0.7d, 102.5d)]
        [TestCase(0.8d, 103d)]
        [TestCase(1.1d, 103d)]
        public void ValidDefinition(double input, params double[] expectedOutput)
        {
            var function = new StitchingFunction(new PdfDictionary
            {
                { Names.Domain, new object[] { 0d, 1d } },
                { Names.Range, new object[] { 0d, 103d } },
                { Names.Bounds, new object[] { 0.4d, 0.6d } },
                { Names.Encode, new object[] { 0d, 1d, 0.2d, 0.8d, 0d, 1d } },
                { Names.Functions, new object[]
                {
                    new PdfDictionary
                    {
                        { Names.FunctionType, 2 },
                        { Names.Domain, new object[] { 0, 1 } },
                        { Names.C0, new object[] { 8 } },
                        { Names.C1, new object[] { 12 } },
                    },
                    new PdfDictionary
                    {
                        { Names.FunctionType, 2 },
                        { Names.Domain, new object[] { 0, 1 } },
                        { Names.C0, new object[] { 42, 43 } },
                        { Names.C1, new object[] { 46, 47 } },
                    },
                    new PdfDictionary
                    {
                        { Names.FunctionType, 2 },
                        { Names.Domain, new object[] { 0, 1 } },
                        { Names.C0, new object[] { 102 } },
                        { Names.C1, new object[] { 104 } },
                    },
                } },
            });

            Assert.AreEqual(expectedOutput, function.Evaluate(input));
        }

        [Test]
        public void MissingFunctions()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new StitchingFunction(new PdfDictionary
                {
                    { Names.Domain, new object[] { 0d, 1d } },
                    { Names.Range, new object[] { 0d, 103d } },
                    { Names.Bounds, new object[] { 0.4d, 0.6d } },
                    { Names.Functions, new object[]
                    {
                        new PdfDictionary
                        {
                            { Names.FunctionType, 2 },
                            { Names.Domain, new object[] { 0, 1 } },
                            { Names.C0, new object[] { 8 } },
                            { Names.C1, new object[] { 12 } },
                        },
                    } },
                });
            });
        }

        [Test]
        public void MissingBounds()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new StitchingFunction(new PdfDictionary
                {
                    { Names.Domain, new object[] { 0d, 1d } },
                    { Names.Range, new object[] { 0d, 103d } },
                    { Names.Encode, new object[] { 0.4d, 0.6d } },
                    { Names.Functions, new object[]
                    {
                        new PdfDictionary
                        {
                            { Names.FunctionType, 2 },
                            { Names.Domain, new object[] { 0, 1 } },
                            { Names.C0, new object[] { 8 } },
                            { Names.C1, new object[] { 12 } },
                        },
                    } },
                });
            });
        }

        [Test]
        public void MissingDomain()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new StitchingFunction(new PdfDictionary
                {
                    { Names.Bounds, new object[] { 0d, 1d } },
                    { Names.Range, new object[] { 0d, 103d } },
                    { Names.Encode, new object[] { 0.4d, 0.6d } },
                    { Names.Functions, new object[]
                    {
                        new PdfDictionary
                        {
                            { Names.FunctionType, 2 },
                            { Names.Domain, new object[] { 0, 1 } },
                            { Names.C0, new object[] { 8 } },
                            { Names.C1, new object[] { 12 } },
                        },
                    } },
                });
            });
        }

        [Test]
        public void MissingEncode()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new StitchingFunction(new PdfDictionary
                {
                    { Names.Domain, new object[] { 0d, 1d } },
                    { Names.Range, new object[] { 0d, 103d } },
                    { Names.Bounds, new object[] { 0.4d, 0.6d } },
                    { Names.Encode, new object[] {  } },
                });
            });
        }
    }
}
