// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

#if NET8_0_OR_GREATER

using NUnit.Framework;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace PdfToSvg.Tests.Common
{
    public class VectorUtilsTests
    {
        [Test]
        public void ClampNative128_Scalar()
        {
            var input = Vector128.Create(-0.001f, 0.001f, 249.999f, 255.1f);

            var output = VectorUtils.ClampNative(input, Vector128.Create(0f), Vector128.Create(255f));

            Assert.AreEqual(Vector128.Create(0, 0.001f, 249.999f, 255f), output);
        }

        [Test]
        public void ClampNative128_Infinity()
        {
            var input = Vector128.Create(float.NegativeInfinity, 0.001f, 249.999f, float.PositiveInfinity);

            var output = VectorUtils.ClampNative(input, Vector128.Create(0f), Vector128.Create(255f));

            Assert.AreEqual(Vector128.Create(0, 0.001f, 249.999f, 255f), output);
        }
        
        [Test]
        public void ClampNative256()
        {
            var input = Vector256.Create(float.NegativeInfinity, -0.001f, 0.001f, 1f, 2f, 249.999f, 255f, float.PositiveInfinity);

            var output = VectorUtils.ClampNative(input, Vector256.Create(0f), Vector256.Create(255f));

            Assert.AreEqual(Vector256.Create(0, 0, 0.001f, 1f, 2f, 249.999f, 255f, 255f), output);
        }

        [Test]
        public void InterleaveLow128()
        {
            var output = VectorUtils.InterleaveLow(Vector128.Create(1f, 2f, 3f, 4f), Vector128.Create(5f, 6f, 7f, 8f));

            Assert.AreEqual(Vector128.Create(1f, 5f, 2f, 6f), output);
        }

        [Test]
        public void InterleaveHigh128()
        {
            var output = VectorUtils.InterleaveHigh(Vector128.Create(1f, 2f, 3f, 4f), Vector128.Create(5f, 6f, 7f, 8f));

            Assert.AreEqual(Vector128.Create(3f, 7f, 4f, 8f), output);
        }

        [Test]
        public void ConvertToInt32RoundToEven128()
        {
            var negativeOutput = VectorUtils.ConvertToInt32RoundToEven(Vector128.Create(-0.4f, -0.5f, -0.6f, -1.5f));
            var positiveOutput = VectorUtils.ConvertToInt32RoundToEven(Vector128.Create(0.4f, 0.5f, 0.6f, 1.5f));

            Assert.AreEqual(Vector128.Create(0, 0, -1, -2), negativeOutput);
            Assert.AreEqual(Vector128.Create(0, 0, 1, 2), positiveOutput);
        }

        [Test]
        public void ConvertToInt32RoundToEven256()
        {
            var input = Vector256.Create(-0.4f, -0.5f, -0.6f, -1.5f, 0.4f, 0.5f, 0.6f, 1.5f);
            var expectedOutput = Vector256.Create(0, 0, -1, -2, 0, 0, 1, 2);

            var actualOutput = VectorUtils.ConvertToInt32RoundToEven(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }
    }
}

#endif
