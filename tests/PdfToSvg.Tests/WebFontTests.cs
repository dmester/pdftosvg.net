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
    public class WebFontTests
    {
        private sealed class Resolver : FontResolver
        {
            public Font Font;

            public override Font ResolveFont(SourceFont sourceFont, CancellationToken cancellationToken)
            {
                return Font;
            }
        }

        private static readonly TestCaseData[] Urls =
        {
            new TestCaseData(
                "font.woff?a=1&b=%22%26",
                "\"font.woff?a=1&amp;b=%22%26\""),
            new TestCaseData(
                "font'\"\\\r\n.woff</style><script>bad</script>",
                "\"font'\\\"\\\\\\d\\a.woff&lt;/style&gt;&lt;script&gt;bad&lt;/script&gt;\""),
            new TestCaseData(
                "data:font/woff;base64,AA'\"\\\n",
                "\"data:font/woff;base64,AA'\\\"\\\\\\a\""),
        };

        [TestCaseSource(nameof(Urls))]
        public void ResolveFont_ExternalUrls(string url, string expectedEncodedUrl)
        {
            var font = new WebFont(woffUrl: url, woff2Url: url, trueTypeUrl: url, openTypeUrl: url);

            Assert.IsFalse(font.IsSafeDataUrl);

            using var doc = PdfDocument.Open(Path.Combine(TestFiles.InputDirectory, "encoding.pdf"));
            var options = new SvgConversionOptions { FontResolver = new Resolver { Font = font } };

            var actual = doc.Pages[0].ToSvgString(options);

            Assert.IsFalse(actual.Contains(url));
            Assert.IsTrue(actual.Contains(expectedEncodedUrl));

            // Should parse:
            XDocument.Parse(actual);
        }

#if !NET40
        [TestCaseSource(nameof(Urls))]
        public async Task ResolveFontAsync_ExternalUrls(string url, string expectedEncodedUrl)
        {
            var font = new WebFont(woffUrl: url, woff2Url: url, trueTypeUrl: url, openTypeUrl: url);

            Assert.IsFalse(font.IsSafeDataUrl);

            using var doc = await PdfDocument.OpenAsync(Path.Combine(TestFiles.InputDirectory, "encoding.pdf"));
            var options = new SvgConversionOptions { FontResolver = new Resolver { Font = font } };

            var actual = await doc.Pages[0].ToSvgStringAsync(options);

            Assert.IsFalse(actual.Contains(url));
            Assert.IsTrue(actual.Contains(expectedEncodedUrl));

            // Should parse:
            XDocument.Parse(actual);
        }
#endif

        [Test]
        public void FromWoffData_GeneratedUrls()
        {
            var font = WebFont.FromWoffData(new byte[] { 0, 255, 254, 253 });
            AssertGeneratedUrls(font, "font/woff");
        }

        [Test]
        public void FromOtfData_GeneratedUrls()
        {
            var font = WebFont.FromOtfData(new byte[] { 0, 255, 254, 253 });
            AssertGeneratedUrls(font, "font/otf");
        }

        private void AssertGeneratedUrls(WebFont font, string dataType)
        {
            Assert.IsTrue(font.IsSafeDataUrl);
            Assert.IsNull(font.FallbackFont);

            using var doc = PdfDocument.Open(Path.Combine(TestFiles.InputDirectory, "encoding.pdf"));

            var options = new SvgConversionOptions { FontResolver = new Resolver { Font = font } };
            var actual = doc.Pages[0].ToSvgString(options);

            var url = "data:" + dataType + ";base64,AP/+/Q==";
            StringAssert.Contains("url('" + url + "')", actual);

            // Should not throw:
            XDocument.Parse(actual);
        }

#if !NET40
        [Test]
        public Task FromWoffData_GeneratedUrls_Async()
        {
            var font = WebFont.FromWoffData(new byte[] { 0, 255, 254, 253 });
            return AssertGeneratedUrlsAsync(font, "font/woff");
        }

        [Test]
        public Task FromOtfData_GeneratedUrls_Async()
        {
            var font = WebFont.FromOtfData(new byte[] { 0, 255, 254, 253 });
            return AssertGeneratedUrlsAsync(font, "font/otf");
        }

        private async Task AssertGeneratedUrlsAsync(WebFont font, string dataType)
        {
            Assert.IsTrue(font.IsSafeDataUrl);
            Assert.IsNull(font.FallbackFont);

            using var doc = PdfDocument.Open(Path.Combine(TestFiles.InputDirectory, "encoding.pdf"));

            var options = new SvgConversionOptions { FontResolver = new Resolver { Font = font } };
            var actual = await doc.Pages[0].ToSvgStringAsync(options);

            var url = "data:" + dataType + ";base64,AP/+/Q==";
            StringAssert.Contains("url('" + url + "')", actual);

            // Should not throw:
            XDocument.Parse(actual);
        }
#endif
    }
}
