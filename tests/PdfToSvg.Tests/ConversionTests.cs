// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg.Tests
{
    public class ConversionTests
    {
        private static string RecompressPngs(string svgMarkup)
        {
            const string DataUrlPrefix = "data:image/png;base64,";
            XNamespace ns = "http://www.w3.org/2000/svg";
            var svg = XElement.Parse(svgMarkup, LoadOptions.PreserveWhitespace);

            var useElements = svg
                .Descendants(ns + "use")
                .ToLookup(el => el.Attribute("href").Value);

            var maskReferences = svg
                .Descendants(ns + "g")
                .ToLookup(el => el.Attribute("mask")?.Value);

            foreach (var image in svg.Descendants(ns + "image"))
            {
                var hrefAttribute = image.Attribute("href");
                var imageRenderingAttribute = image.Attribute("image-rendering");
                var interpolated = imageRenderingAttribute?.Value != "pixelated";

                if (hrefAttribute != null && hrefAttribute.Value.StartsWith(DataUrlPrefix))
                {
                    var base64Png = hrefAttribute.Value.Substring(DataUrlPrefix.Length);
                    base64Png = Convert.ToBase64String(PngTestUtils.Recompress(Convert.FromBase64String(base64Png)));
                    hrefAttribute.Value = DataUrlPrefix + base64Png;
                }
            }

            var orderedIds = new List<string>();
            var ids = new HashSet<string>();

            foreach (var el in svg.Descendants())
            {
                var id = el.Attribute("id")?.Value;
                if (id != null && ids.Add(id))
                {
                    orderedIds.Add(id);
                }

                var classNames = el.Attribute("class")?.Value;
                if (classNames != null)
                {
                    foreach (var className in Regex.Split(classNames, "\\s+"))
                    {
                        if (!string.IsNullOrEmpty(className) && ids.Add(className))
                        {
                            orderedIds.Add(className);
                        }
                    }
                }
            }

            svgMarkup = svg.ToString(SaveOptions.DisableFormatting);

            for (var i = 0; i < orderedIds.Count; i++)
            {
                var newId = "ID" + (i + 1).ToString(CultureInfo.InvariantCulture);
                svgMarkup = svgMarkup.Replace(orderedIds[i], newId);
            }

            return svgMarkup;
        }

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
            return Path.Combine(TestFiles.ExpectedDirectory, Path.ChangeExtension(fileName, ".svg"));
        }

        private static string GetActualFilePath(string fileName, bool sync)
        {
            return Path.Combine(TestFiles.OutputDirectory(sync), Path.ChangeExtension(fileName, ".svg"));
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

            actual = RecompressPngs(actual);
            File.WriteAllText(actualSvgPath, actual, Encoding.UTF8);

            var expected = File.Exists(expectedSvgPath) ? RecompressPngs(File.ReadAllText(expectedSvgPath, Encoding.UTF8)) : null;

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

            actual = RecompressPngs(actual);
            File.WriteAllText(actualSvgPath, actual, Encoding.UTF8);

            var expected = File.Exists(expectedSvgPath) ? RecompressPngs(File.ReadAllText(expectedSvgPath, Encoding.UTF8)) : null;

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
            await ConvertAsync(fileName, "embedded-" + fileName, new SvgConversionOptions
            {
                FontResolver = FontResolver.EmbedOpenType,
            });
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
            ConvertSync(fileName, "embedded-" + fileName, new SvgConversionOptions
            {
                FontResolver = FontResolver.EmbedOpenType,
            });
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
    }
}
