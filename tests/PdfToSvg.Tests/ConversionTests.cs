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

            Assert.AreEqual(expected, actual);
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

            Assert.AreEqual(expected, actual);
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
