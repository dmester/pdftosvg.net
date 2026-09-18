// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Svg;
using System;

namespace PdfToSvg.Tests.Svg
{
    public class SvgElementTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CloneNode_IndependentStyle(bool modifyClone)
        {
            var original = new SvgElement("path");
            original.Style.Fill = "red";
            var clone = (SvgElement)original.CloneNode();

            var modified = modifyClone ? clone : original;
            var unchanged = modifyClone ? original : clone;
            modified.Style.Fill = "blue";

            Assert.AreEqual("red", unchanged.Style.Fill);
            Assert.AreEqual("blue", modified.Style.Fill);
        }

        [Test]
        public void CloneNode_ReusedChild()
        {
            var original = new SvgElement("g");
            original.AddChild(new SvgElement("path"));
            var clone = (SvgElement)original.CloneNode();
            var other = new SvgElement("g");

            other.AddChild(clone.FirstChild);
            other.AddChild(new SvgElement("rect"));

            Assert.AreNotSame(clone.FirstChild, other.FirstChild);
            Assert.IsNull(clone.FirstChild.NextNode);
            Assert.AreSame(clone.FirstChild, clone.LastChild);
        }

        [Test]
        public void SetValue_ReusedChild()
        {
            var original = new SvgElement("text");
            original.SetValue("first");
            var other = new SvgElement("text");

            other.AddChild(original.FirstChild);
            other.AddChild(new SvgText("second"));

            Assert.AreEqual("<text>first</text>", original.ToString().Trim());
            Assert.AreEqual("<text>firstsecond</text>", other.ToString().Trim());
            Assert.AreNotSame(original.FirstChild, other.FirstChild);
        }

        [Test]
        public void CloneNode_Element_Attributes()
        {
            var originalRoot = new SvgElement("a");

            originalRoot.AddAttribute("attr1", "1");
            originalRoot.AddAttribute("attr2", "2");
            originalRoot.AddAttribute("attr3", "3");

            var clonedRoot = (SvgElement)originalRoot.CloneNode();

            clonedRoot.SetAttribute("attr1", "1b");
            clonedRoot.SetAttribute("attr4", "4b");

            Assert.AreEqual("<a attr1=\"1\" attr2=\"2\" attr3=\"3\"/>", originalRoot.ToString().Trim());
            Assert.AreEqual("<a attr1=\"1b\" attr2=\"2\" attr3=\"3\" attr4=\"4b\"/>", clonedRoot.ToString().Trim());
        }

        [Test]
        public void CloneNode_Element_NoChildren()
        {
            var originalRoot = new SvgElement("a");
            var clonedRoot = (SvgElement)originalRoot.CloneNode();

            Assert.AreNotSame(originalRoot, clonedRoot);
            Assert.IsNull(clonedRoot.NextNode);
        }

        [Test]
        public void CloneNode_Element_SingleChild()
        {
            var originalRoot = new SvgElement("a");

            var originalChild1 = new SvgText("1");

            originalRoot.AddChild(originalChild1);

            var clonedRoot = (SvgElement)originalRoot.CloneNode();

            var clonedChild1 = (SvgText)clonedRoot.FirstChild;

            Assert.IsNull(clonedChild1.NextNode);

            Assert.AreNotSame(originalChild1, clonedChild1);

            Assert.AreEqual(originalChild1.Value, clonedChild1.Value);

            Assert.AreSame(clonedChild1, clonedRoot.FirstChild);
            Assert.AreSame(clonedChild1, clonedRoot.LastChild);

            // Original not modified
            Assert.AreSame(originalChild1, originalRoot.FirstChild);
            Assert.AreSame(originalChild1, originalRoot.LastChild);
        }

        [Test]
        public void CloneNode_Element_MultipleChildren()
        {
            var originalRoot = new SvgElement("a");

            var originalChild1 = new SvgText("1");
            var originalChild2 = new SvgText("2");
            var originalChild3 = new SvgText("3");

            originalRoot.AddChild(originalChild1);
            originalRoot.AddChild(originalChild2);
            originalRoot.AddChild(originalChild3);

            var clonedRoot = (SvgElement)originalRoot.CloneNode();

            var clonedChild1 = (SvgText)clonedRoot.FirstChild;
            var clonedChild2 = (SvgText)clonedChild1.NextNode;
            var clonedChild3 = (SvgText)clonedChild2.NextNode;

            Assert.IsNull(clonedChild3.NextNode);

            Assert.AreNotSame(originalChild1, clonedChild1);
            Assert.AreNotSame(originalChild2, clonedChild2);
            Assert.AreNotSame(originalChild3, clonedChild3);

            Assert.AreEqual(originalChild1.Value, clonedChild1.Value);
            Assert.AreEqual(originalChild2.Value, clonedChild2.Value);
            Assert.AreEqual(originalChild3.Value, clonedChild3.Value);

            Assert.AreSame(clonedChild1, clonedRoot.FirstChild);
            Assert.AreSame(clonedChild3, clonedRoot.LastChild);

            // Original not modified
            Assert.AreSame(originalChild1, originalRoot.FirstChild);
            Assert.AreSame(originalChild3, originalRoot.LastChild);
        }
    }
}
