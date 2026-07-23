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
        [TestCase(-2f, 8f, 9f)]
        [TestCase(0.2f, 10f, 11f)]
        [TestCase(0.4f, 42.8f, 43.8f)]
        [TestCase(0.5f, 44f, 45f)]
        [TestCase(0.6f, 102f, 103f)]
        [TestCase(0.7f, 102.5f, 103.5f)]
        [TestCase(0.8f, 103f, 104f)]
        [TestCase(1.1f, 103f, 104f)]
        public void ValidDefinition(float input, params float[] expectedOutput)
        {
            var function = new StitchingFunction(new PdfDictionary
            {
                { Names.Domain, new object[] { 0d, 1d } },
                { Names.Range, new object[] { 0d, 103d, 0d, 104d } },
                { Names.Bounds, new object[] { 0.4d, 0.6d } },
                { Names.Encode, new object[] { 0d, 1d, 0.2d, 0.8d, 0d, 1d } },
                { Names.Functions, new object[]
                {
                    new PdfDictionary
                    {
                        { Names.FunctionType, 2 },
                        { Names.Domain, new object[] { 0, 1 } },
                        { Names.C0, new object[] { 8, 9 } },
                        { Names.C1, new object[] { 12, 13 } },
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
                        { Names.C0, new object[] { 102, 103 } },
                        { Names.C1, new object[] { 104, 105 } },
                    },
                } },
            });

            Assert.AreEqual(expectedOutput, function.EvaluateForTest(input));
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
