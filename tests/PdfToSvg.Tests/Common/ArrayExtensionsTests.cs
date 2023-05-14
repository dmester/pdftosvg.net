// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Common
{
    public class ArrayExtensionsTests
    {
        [Test]
        public void IndexOf_Start()
        {
            var stack = new byte[] { 1, 2, 3, 4, 5, 6 };
            var needle = new byte[] { 1, 2, 3 };

            Assert.AreEqual(0, stack.IndexOf(needle));
        }

        [Test]
        public void IndexOf_End()
        {
            var stack = new byte[] { 1, 2, 3, 4, 5, 6 };
            var needle = new byte[] { 4, 5, 6 };

            Assert.AreEqual(3, stack.IndexOf(needle));
        }

        [Test]
        public void IndexOf_Middle()
        {
            var stack = new byte[] { 1, 2, 3, 3, 4, 5, 6 };
            var needle = new byte[] { 3, 4 };

            Assert.AreEqual(3, stack.IndexOf(needle));
        }

        [Test]
        public void IndexOf_NotFound()
        {
            var stack = new byte[] { 1, 2, 3, 3, 4, 5, 6 };
            var needle = new byte[] { 3, 6 };

            Assert.AreEqual(-1, stack.IndexOf(needle));
        }

        [Test]
        public void IndexOf_ShorterStack()
        {
            var stack = new byte[] { 3 };
            var needle = new byte[] { 3, 6 };

            Assert.AreEqual(-1, stack.IndexOf(needle));
        }

        [Test]
        public void IndexOf_EmptyStack()
        {
            var stack = new byte[0];
            var needle = new byte[] { 3, 6 };

            Assert.AreEqual(-1, stack.IndexOf(needle));
        }

        [Test]
        public void IndexOf_EmptyNeedle()
        {
            var stack = new byte[] { 2, 3, 4 };
            var needle = new byte[0];

            // Behaves the same way as String.IndexOf
            Assert.AreEqual(0, stack.IndexOf(needle));
        }

        [Test]
        public void IndexOf_EmptyStackAndNeedle()
        {
            var stack = new byte[0];
            var needle = new byte[0];

            Assert.AreEqual(0, stack.IndexOf(needle));
        }

    }
}
