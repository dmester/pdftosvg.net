// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
#if HAVE_SKIA
using SkiaSharp;
using Svg.Skia;
#endif

namespace PdfToSvg.Tests
{
    [Parallelizable(ParallelScope.Children)]
    public class ConversionTests
    {
        private class TestFontResolver : FontResolver
        {
            public override Font ResolveFont(SourceFont sourceFont, CancellationToken cancellationToken)
            {
                var fontName = sourceFont.Name;
                if (fontName == "Times-Bold")
                {
                    return new WebFont(
                        fallbackFont: new LocalFont("'Times New Roman',sans-serif", FontWeight.Bold),
                        woffUrl: "http://pdftosvg.net/assets/sourcesanspro-bold-webfont.woff",
                        woff2Url: "http://pdftosvg.net/assets/sourcesanspro-bold-webfont.woff2",
                        trueTypeUrl: "http://pdftosvg.net/assets/sourcesanspro-bold-webfont.ttf",
                        openTypeUrl: "http://pdftosvg.net/assets/sourcesanspro-bold-webfont.otf");
                }
                else
                {
                    return new WebFont(
                        fallbackFont: new LocalFont("'Times New Roman',sans-serif"),
                        woffUrl: "http://pdftosvg.net/assets/sourcesanspro-regular-webfont.woff",
                        woff2Url: "http://pdftosvg.net/assets/sourcesanspro-regular-webfont.woff2",
                        trueTypeUrl: "http://pdftosvg.net/assets/sourcesanspro-regular-webfont.ttf",
                        openTypeUrl: "http://pdftosvg.net/assets/sourcesanspro-regular-webfont.otf");
                }
            }
        }

        private static string GetInputFilePath(string fileName)
        {
            return Path.Combine(TestFiles.InputDirectory, fileName);
        }

        private static string GetExpectedFilePath(string fileName)
        {
            return Path.Combine(TestFiles.ExpectedDirectory, GetSvgFileName(fileName));
        }

        private static string GetActualFilePath(string fileName, bool sync)
        {
            return Path.Combine(TestFiles.OutputDirectory(sync), GetSvgFileName(fileName));
        }

        private static string GetSvgFileName(string inputFileName)
        {
            return Path.ChangeExtension(inputFileName, null) + ".svg";
        }

        private static void AssertSvgEqual(string expected, string actual)
        {
            if (expected == actual)
            {
                Assert.Pass();
                return;
            }

            if (expected == null || actual == null)
            {
                Assert.Multiple(() => Assert.AreEqual(expected, actual));
                return;
            }

            Assert.Multiple(() =>
            {
                var expectedText = ExtractSvgText(expected);
                var actualText = ExtractSvgText(actual);
                var textEqual = expectedText.SequenceEqual(actualText);

                Assert.AreEqual(expectedText, actualText, "SVG text differs.");

                var imageEqual = true;
#if HAVE_SKIA
                imageEqual = AssertSvgImagesEqual(expected, actual);
#endif

                if (textEqual && imageEqual)
                {
                    Assert.AreEqual(expected, actual);
                }
            });
        }

        private static string[] ExtractSvgText(string svg)
        {
            return XDocument.Parse(svg)
                .Descendants()
                .Where(element => element.Name.LocalName == "text")
                .Select(element => element.Value)
                .ToArray();
        }

#if HAVE_SKIA
        private static bool AssertSvgImagesEqual(string expected, string actual)
        {
            try
            {
                using var expectedImage = RenderSvg(expected);
                using var actualImage = RenderSvg(actual);

                if (expectedImage.Width != actualImage.Width || expectedImage.Height != actualImage.Height)
                {
                    Assert.Fail(
                        $"SVG renderings have different dimensions. " +
                        $"Expected: {expectedImage.Width}x{expectedImage.Height}. " +
                        $"Actual: {actualImage.Width}x{actualImage.Height}.");
                    return false;
                }

                var expectedPixels = expectedImage.Pixels;
                var actualPixels = actualImage.Pixels;
                var differentPixelCount = 0;
                var minX = expectedImage.Width;
                var minY = expectedImage.Height;
                var maxX = -1;
                var maxY = -1;

                for (var y = 0; y < expectedImage.Height; y++)
                {
                    for (var x = 0; x < expectedImage.Width; x++)
                    {
                        var index = y * expectedImage.Width + x;
                        if (expectedPixels[index] != actualPixels[index])
                        {
                            differentPixelCount++;
                            minX = Math.Min(minX, x);
                            minY = Math.Min(minY, y);
                            maxX = Math.Max(maxX, x);
                            maxY = Math.Max(maxY, y);
                        }
                    }
                }

                if (differentPixelCount > 0)
                {
                    var totalPixelCount = expectedImage.Width * expectedImage.Height;
                    var differentPercent = differentPixelCount * 100d / totalPixelCount;
                    Assert.Fail(
                        $"SVG renderings differ in {differentPixelCount} of {totalPixelCount} pixels " +
                        $"({differentPercent:0.####}%). Difference bounds: x={minX}, y={minY}, " +
                        $"width={maxX - minX + 1}, height={maxY - minY + 1}.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to render SVGs with SkiaSharp: {ex.Message}");
                return false;
            }
        }

        private static SKBitmap RenderSvg(string svg)
        {
            using var svgDocument = new SKSvg();

            // We embed OpenType fonts, not SVG <font> elements. Disable the SVG font parser, whose regex
            // is very slow on large base64 font URLs in Svg.Skia 5.1.1, especially on older runtimes.
            svgDocument.Settings.EnableSvgFonts = false;

            var picture = svgDocument.FromSvg(svg);
            if (picture == null)
            {
                throw new InvalidOperationException("Svg.Skia did not produce a picture.");
            }

            var bounds = picture.CullRect;
            var width = Math.Max(1, (int)Math.Ceiling(bounds.Width));
            var height = Math.Max(1, (int)Math.Ceiling(bounds.Height));
            var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            bitmap.Erase(SKColors.Transparent);

            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Translate(-bounds.Left, -bounds.Top);
                canvas.DrawPicture(picture);
                canvas.Flush();
            }

            return bitmap;
        }
#endif

        private void ConvertSync(string pdfName, string expectedSvgName,
            SvgConversionOptions conversionOptions = null,
            Action<PdfDocument> documentSetup = null)
        {
            var expectedSvgPath = GetExpectedFilePath(expectedSvgName);
            var actualSvgPath = GetActualFilePath(expectedSvgName, sync: true);
            var pdfPath = GetInputFilePath(pdfName);

            Directory.CreateDirectory(Path.GetDirectoryName(actualSvgPath));

            string actual;
            using (var doc = PdfDocument.Open(pdfPath))
            {
                documentSetup?.Invoke(doc);
                actual = doc.Pages[0].ToSvgString(conversionOptions);
            }

            actual = PngTestUtils.RecompressPngsInSvg(actual);
            File.WriteAllText(actualSvgPath, actual, Encoding.UTF8);

            var expected = File.Exists(expectedSvgPath) ? PngTestUtils.RecompressPngsInSvg(File.ReadAllText(expectedSvgPath, Encoding.UTF8)) : null;

            AssertSvgEqual(expected, actual);
        }

#if !NET40
        private async Task ConvertAsync(string pdfName, string expectedSvgName, SvgConversionOptions conversionOptions)
        {
            var expectedSvgPath = GetExpectedFilePath(expectedSvgName);
            var actualSvgPath = GetActualFilePath(expectedSvgName, sync: false);
            var pdfPath = GetInputFilePath(pdfName);

            Directory.CreateDirectory(Path.GetDirectoryName(actualSvgPath));

            string actual;
            using (var doc = await PdfDocument.OpenAsync(pdfPath))
            {
                actual = await doc.Pages[0].ToSvgStringAsync(conversionOptions);
            }

            actual = PngTestUtils.RecompressPngsInSvg(actual);
            File.WriteAllText(actualSvgPath, actual, Encoding.UTF8);

            var expected = File.Exists(expectedSvgPath) ? PngTestUtils.RecompressPngsInSvg(File.ReadAllText(expectedSvgPath, Encoding.UTF8)) : null;

            AssertSvgEqual(expected, actual);
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task ConvertAsync(string fileName)
        {
            await ConvertAsync(fileName, fileName, new SvgConversionOptions
            {
                FontResolver = FontResolver.LocalFonts,
            });
        }

        [TestCaseSource(nameof(FontTestCases))]
        public async Task ConvertEmbeddedAsync(string fileName)
        {
            var options = new SvgConversionOptions();
            options.FontRepository.AddDirectory(TestFiles.ExternalFontsDirectory, allowEmbedding: true);
            options.FontResolver = FontResolver.EmbedOpenType;

            await ConvertAsync(fileName, "embedded-" + fileName, options);
        }
#endif

        [TestCaseSource(nameof(TestCases))]
        public void ConvertSync(string fileName)
        {
            ConvertSync(fileName, fileName, new SvgConversionOptions
            {
                FontResolver = FontResolver.LocalFonts,
            });
        }

        [TestCaseSource(nameof(FontTestCases))]
        public void ConvertEmbeddedSync(string fileName)
        {
            var options = new SvgConversionOptions();
            options.FontRepository.AddDirectory(TestFiles.ExternalFontsDirectory, allowEmbedding: true);
            options.FontResolver = FontResolver.EmbedOpenType;

            ConvertSync(fileName, "embedded-" + fileName, options);
        }

        [TestCaseSource(nameof(ExternalFontTestCases))]
        public void ConvertNonEmbeddedSync(string fileName)
        {
            var options = new SvgConversionOptions();
            options.FontRepository.AddDirectory(TestFiles.ExternalFontsDirectory, allowEmbedding: false);
            options.FontResolver = FontResolver.EmbedOpenType;

            ConvertSync(fileName, "nonembedded-" + fileName, options);
        }

        [TestCase("clip-path-depth.pdf", "Maximum clip path depth exceeded")]
        public void ConvertShouldFail(string filename, string expectedException)
        {
            var input = Path.Combine(TestFiles.InputShouldFailDirectory, filename);

            var exception = Assert.Throws<PdfException>(() =>
            {
                using (var document = PdfDocument.Open(input))
                {
                    foreach (var page in document.Pages)
                    {
                        page.ToSvgString();
                    }
                }
            });

            StringAssert.Contains(expectedException, exception.Message);
        }

#if !NET40
        [TestCase("clip-path-depth.pdf", "Maximum clip path depth exceeded")]
        public void ConvertShouldFailAsync(string filename, string expectedException)
        {
            var input = Path.Combine(TestFiles.InputShouldFailDirectory, filename);

            var exception = Assert.ThrowsAsync<PdfException>(async () =>
            {
                using (var document = await PdfDocument.OpenAsync(input))
                {
                    foreach (var page in document.Pages)
                    {
                        await page.ToSvgStringAsync();
                    }
                }
            });

            StringAssert.Contains(expectedException, exception.Message);
        }
#endif

        [Test]
        public void ToggleOptionalContentGroup()
        {
            ConvertSync("optionalcontentgroup-contentypes.pdf", "optionalcontentgroup-contentypes-toggled.svg",
                new SvgConversionOptions
                {
                    FontResolver = FontResolver.LocalFonts,
                },
                doc =>
                {
                    var group = doc.OptionalContentGroups[0];

                    Assert.AreEqual("Group 1", group.Name);
                    Assert.IsTrue(group.Visible);

                    group.Visible = false;
                });
        }

        [Test]
        public void WebFontConversion()
        {
            ConvertSync("encoding.pdf", "encoding-webfont.svg", new SvgConversionOptions
            {
                FontResolver = new TestFontResolver(),
            });
        }

        [Test]
        public void ExcludeHiddenText()
        {
            ConvertSync("text-rendering-mode.pdf", "text-rendering-mode-without-hidden-text.svg", new SvgConversionOptions
            {
                IncludeHiddenText = false,
                FontResolver = FontResolver.LocalFonts,
            });
        }

        [Test]
        public void ExcludeAnnotations()
        {
            ConvertSync("annotation-markup.pdf", "annotation-markup-notexported.svg", new SvgConversionOptions
            {
                IncludeAnnotations = false,
                FontResolver = FontResolver.LocalFonts,
            });
        }

        [Test]
        public void ExposeFileAttachments()
        {
            var pdfPath = GetInputFilePath("annotation-files.pdf");
            using var doc = PdfDocument.Open(pdfPath);
            var page = doc.Pages[0];
            var svg = page.ToSvgString();

            Assert.AreEqual(2, page.FileAttachments.Count, "File attachment count");

            Assert.AreEqual("Test file 1.txt", page.FileAttachments[1].Name);
            Assert.AreEqual("file 2.txt", page.FileAttachments[0].Name);

            using (var stream1 = page.FileAttachments[1].GetContent())
            {
                Assert.AreEqual(0, stream1.Position);
                Assert.AreEqual("Test file 1 content", Encoding.ASCII.GetString(stream1.ToArray()));
            }
            using (var stream2 = page.FileAttachments[0].GetContent())
            {
                Assert.AreEqual(0, stream2.Position);
                Assert.AreEqual("Test file 2 content", Encoding.ASCII.GetString(stream2.ToArray()));
            }

            Assert.That(svg.Contains("annot:file-index=\"0\""));
            Assert.That(svg.Contains("annot:file-index=\"1\""));
        }

#if !NET40
        [Test]
        public async Task ExposeFileAttachmentsAsync()
        {
            var pdfPath = GetInputFilePath("annotation-files.pdf");
            using var doc = await PdfDocument.OpenAsync(pdfPath);
            var page = doc.Pages[0];
            var svg = page.ToSvgString();

            Assert.AreEqual(2, page.FileAttachments.Count, "File attachment count");

            Assert.AreEqual("Test file 1.txt", page.FileAttachments[1].Name);
            Assert.AreEqual("file 2.txt", page.FileAttachments[0].Name);

            using (var stream1 = await page.FileAttachments[1].GetContentAsync())
            {
                Assert.AreEqual(0, stream1.Position);
                Assert.AreEqual("Test file 1 content", Encoding.ASCII.GetString(stream1.ToArray()));
            }
            using (var stream2 = await page.FileAttachments[0].GetContentAsync())
            {
                Assert.AreEqual(0, stream2.Position);
                Assert.AreEqual("Test file 2 content", Encoding.ASCII.GetString(stream2.ToArray()));
            }

            Assert.That(svg.Contains("annot:file-index=\"0\""));
            Assert.That(svg.Contains("annot:file-index=\"1\""));
        }
#endif

        [Test]
        public void DocumentInfo()
        {
            var pdfPath = GetInputFilePath("document-info-pdfdoc-utf16-utf8.pdf");

            using (var doc = PdfDocument.Open(pdfPath))
            {
                Assert.AreEqual("Title åäö", doc.Info.Title);
                Assert.AreEqual("Subject åäö", doc.Info.Subject);
                Assert.AreEqual("Author åäö", doc.Info.Author);
            }
        }

        public static List<TestCaseData> TestCases
        {
            get
            {
                return Directory
                    .EnumerateFiles(Path.Combine(TestFiles.InputDirectory), "*.pdf")
                    .Select(path => new TestCaseData(Path.GetFileName(path)))
                    .ToList();
            }
        }

        public static List<TestCaseData> FontTestCases
        {
            get
            {
                return Directory
                    .EnumerateFiles(Path.Combine(TestFiles.InputDirectory), "fonts-*.pdf")
                    .Select(path => new TestCaseData(Path.GetFileName(path)))
                    .ToList();
            }
        }

        public static List<TestCaseData> ExternalFontTestCases
        {
            get
            {
                return Directory
                    .EnumerateFiles(Path.Combine(TestFiles.InputDirectory), "fonts-external-*.pdf")
                    .Select(path => new TestCaseData(Path.GetFileName(path)))
                    .ToList();
            }
        }
    }
}
