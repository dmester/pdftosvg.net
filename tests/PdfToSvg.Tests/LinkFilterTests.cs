// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PdfToSvg.Tests
{
    public class LinkFilterTests
    {
        [TestCase("http://EXAMPLE.com", "http://EXAMPLE.com")]
        [TestCase("https://example.com/%7Euser?q=%40", "https://example.com/%7Euser?q=%40")]
        [TestCase("tel:+46-70-1740605", "tel:+46-70-1740605")]
        [TestCase("MAILTO:a+b@example.com?subject=%40", "MAILTO:a+b@example.com?subject=%40")]
        [TestCase("mailto:a@example.com?subject=%40", "mailto:a@example.com?subject=%40")]
        [TestCase("mailto:a%40b@example.com", "mailto:a%40b@example.com")]
        [TestCase("https://[invalid", null)]
        [TestCase("document.html", null)]
        [TestCase("ftp://example.com", null)]
        [TestCase("javascript:alert(1)", null)]
        public void LinkFilter_Default(string url, string expected)
        {
            var options = new SvgConversionOptions();

            Assert.AreEqual(expected, options.LinkFilter(url));
        }

        [Test]
        public void LinkFilter_NullKeepsOriginalTargets()
        {
            var options = new SvgConversionOptions { LinkFilter = null };

            using var document = PdfDocument.Open(Path.Combine(TestFiles.InputDirectory, "annotation-hyperlink-schemes.pdf"));

            var svg = XElement.Parse(document.Pages[0].ToSvgString(options));

            var links = svg
                .Descendants(svg.Name.Namespace + "a")
                .Select(element => (string)element.Attribute("href"));

            CollectionAssert.Contains(links, "mailto:hello@example.com");
            CollectionAssert.Contains(links, "javascript:alert('Hello!')");
        }
    }
}
