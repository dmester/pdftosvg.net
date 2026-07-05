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
    }
}

#endif
