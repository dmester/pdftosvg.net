// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg.Tests
{
    public class ImageResolverTests
    {
        private sealed class Resolver : ImageResolver
        {
            public string Url;

            public override string ResolveImageUrl(Image image, CancellationToken cancellationToken)
            {
                return Url;
            }
        }

        private sealed class DelegatingResolver : ImageResolver
        {
            public override string ResolveImageUrl(Image image, CancellationToken cancellationToken)
            {
                return ImageResolver.DataUrl.ResolveImageUrl(image, cancellationToken);
            }
        }

        [Test]
        public void ResolveImageUrl_GeneratedUrls()
        {
            using var doc = PdfDocument.Open(Path.Combine(TestFiles.InputDirectory, "blend-mode-image.pdf"));

            var options = new SvgConversionOptions { ImageResolver = ImageResolver.DataUrl };
            var actual = doc.Pages[0].ToSvgString(options);

            options.ImageResolver = new DelegatingResolver();
            Assert.AreEqual(actual, doc.Pages[0].ToSvgString(options));

            var images = XDocument.Parse(actual).Descendants().Where(x => x.Name.LocalName == "image").ToArray();
            Assert.IsNotEmpty(images);

            foreach (var image in images)
            {
                StringAssert.StartsWith("data:image/", (string)image.Attribute("href"));
                StringAssert.Contains(";base64,", (string)image.Attribute("href"));
            }
        }

#if !NET40
        [Test]
        public async Task ResolveImageUrlAsync_GeneratedUrls()
        {
            using var doc = PdfDocument.Open(Path.Combine(TestFiles.InputDirectory, "blend-mode-image.pdf"));

            var options = new SvgConversionOptions { ImageResolver = ImageResolver.DataUrl };
            var actual = await doc.Pages[0].ToSvgStringAsync(options);

            options.ImageResolver = new DelegatingResolver();
            Assert.AreEqual(actual, doc.Pages[0].ToSvgString(options));

            var images = XDocument.Parse(actual).Descendants().Where(x => x.Name.LocalName == "image").ToArray();
            Assert.IsNotEmpty(images);

            foreach (var image in images)
            {
                StringAssert.StartsWith("data:image/", (string)image.Attribute("href"));
                StringAssert.Contains(";base64,", (string)image.Attribute("href"));
            }
        }
#endif

        [TestCase("image.png?a=1&b=%22%26")]
        [TestCase("data:image/png;base64,AA\"/><script>bad</script><image href=\"'")]
        public void ResolveImageUrl_ExternalUrls(string url)
        {
            using var doc = PdfDocument.Open(Path.Combine(TestFiles.InputDirectory, "blend-mode-image.pdf"));

            var options = new SvgConversionOptions { ImageResolver = new Resolver { Url = url } };
            var actual = doc.Pages[0].ToSvgString(options);

            var xml = XDocument.Parse(actual);
            Assert.IsFalse(xml.Descendants().Any(x => x.Name.LocalName == "script"));

            var images = xml.Descendants().Where(x => x.Name.LocalName == "image").ToArray();
            Assert.IsNotEmpty(images);

            foreach (var image in images)
            {
                Assert.AreEqual(url, (string)image.Attribute("href"));
            }
        }

#if !NET40
        [TestCase("image.png?a=1&b=%22%26")]
        [TestCase("data:image/png;base64,AA\"/><script>bad</script><image href=\"'")]
        public async Task ResolveImageUrlAsync_ExternalUrls(string url)
        {
            using var doc = PdfDocument.Open(Path.Combine(TestFiles.InputDirectory, "blend-mode-image.pdf"));

            var options = new SvgConversionOptions { ImageResolver = new Resolver { Url = url } };
            var actual = await doc.Pages[0].ToSvgStringAsync(options);

            var xml = XDocument.Parse(actual);
            Assert.IsFalse(xml.Descendants().Any(x => x.Name.LocalName == "script"));

            var images = xml.Descendants().Where(x => x.Name.LocalName == "image").ToArray();
            Assert.IsNotEmpty(images);

            foreach (var image in images)
            {
                Assert.AreEqual(url, (string)image.Attribute("href"));
            }
        }
#endif
    }
}
