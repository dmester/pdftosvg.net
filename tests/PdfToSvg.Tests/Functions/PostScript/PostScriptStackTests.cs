// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Functions;
using PdfToSvg.Functions.PostScript;

namespace PdfToSvg.Tests.Functions.PostScript
{
    public class PostScriptStackTests
    {
        [Test]
        public void Push_FullStack_Throws()
        {
            var stack = new PostScriptStack();

            for (var i = 0; i < PostScriptLimits.StackSize; i++)
            {
                stack.Push(1);
            }

            Assert.Throws<PostScriptStackOverflowException>(() => stack.Push(2));
        }

        [Test]
        public void Copy_FullStack_Throws()
        {
            var stack = new PostScriptStack();
            
            for (var i = 0; i < PostScriptLimits.StackSize; i++)
            {
                stack.Push(1);
            }

            Assert.Throws<PostScriptStackOverflowException>(() => stack.Copy(1));
        }

        [Test]
        public void Copy_AvailableCapacity_CopiesValues()
        {
            var stack = new PostScriptStack();
            stack.Push(1);
            stack.Push(2);

            stack.Copy(2);

            Assert.AreEqual(4, stack.Count);
            Assert.AreEqual(2d, stack.Get(0).Number);
            Assert.AreEqual(1d, stack.Get(1).Number);
        }

        [Test]
        public void Get_OutOfRange_Throws()
        {
            var stack = new PostScriptStack();
            stack.Push(1);

            Assert.Throws<PostScriptStackUnderflowException>(() => stack.Get(1));
        }

        [Test]
        public void PopInt_Boolean_ConvertsNumericValue()
        {
            var stack = new PostScriptStack();
            stack.Push(true);

            stack.Pop(out int result);

            Assert.AreEqual(1, result);
        }
    }
}
