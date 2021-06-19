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
        public void GetOptimizableElements()
        {
            var svg = XElement.Parse(@"
                <svg xmlns=""http://www.w3.org/2000/svg"" id=""svg"">
                  <g id=""1"">
                    xxx
                    <image>
                      <g id=""2"">
                        xxx
                      </g>
                    </image>
                    <!-- xxx -->
                    <g id=""3"">
                      <text>abc</text>
                      <g id=""4"">
                        xxx
                        <g id=""5"">
                        </g>
                      </g>
                    </g>
                    xxx
                  </g>
                  <text>
                    abc
                    <g id=""6""></g>
                  </text>
                  <g id=""7""></g>
                </svg>");

            Assert.AreEqual(new[] { "svg", "1", "3", "4", "5", "7" }, SvgAttributeOptimizer
                .GetOptimizableElements(svg)
                .Select(x => (string)x.Attribute("id"))
                .ToArray());

            Assert.AreEqual(new[] { "1", "3", "4", "5" }, SvgAttributeOptimizer
                .GetOptimizableElements(svg.Element((XNamespace)"http://www.w3.org/2000/svg" + "g"))
                .Select(x => (string)x.Attribute("id"))
                .ToArray());
        }

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

            SvgAttributeOptimizer.Optimize(svg);

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
