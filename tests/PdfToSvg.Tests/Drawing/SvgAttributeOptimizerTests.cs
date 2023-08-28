// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg.Tests.Drawing
{
    public class SvgAttributeOptimizerTests
    {
        [Test]
        public void Optimize()
        {
            var svg = XElement.Parse(@"<svg xmlns=""http://www.w3.org/2000/svg"">
  <g>
    <text class=""abc"">abc</text>
    <text class=""abc"">abc</text>
    <text>abc</text>
    <text class=""abc"">abc</text>
    <!-- abc -->
    <text class=""abc"">abc</text>
    <text class=""abc"" stroke=""5"">abc</text>
    <text class=""abc"" stroke=""5"">abc</text>
    <text class=""abcd"" stroke=""5"">abc</text>
    <g>
      <path stroke=""black"" />
      <path stroke=""black"" />
      <path stroke=""white"" />
    </g>
  </g>
</svg>");

            svg = SvgAttributeOptimizer.Optimize(svg);

            var expected = @"<svg xmlns=""http://www.w3.org/2000/svg"">
  <g>
    <g class=""abc"">
      <text>abc</text>
      <text>abc</text>
    </g>
    <text>abc</text>
    <text class=""abc"">abc</text>
    <!-- abc -->
    <text class=""abc"">abc</text>
    <g class=""abc"" stroke=""5"">
      <text>abc</text>
      <text>abc</text>
    </g>
    <text class=""abcd"" stroke=""5"">abc</text>
    <g>
      <g stroke=""black"">
        <path />
        <path />
      </g>
      <path stroke=""white"" />
    </g>
  </g>
</svg>";

            var actual = svg.ToString();

            Assert.AreEqual(expected, actual);
        }
    }
}
