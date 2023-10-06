// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
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
        private const string OwnTestFilesDir = "Own";
        private const string InputDir = "input";
        private const string ExpectedDir = "expected";

#if NET40
        private const string TargetFramework = "net40";
#elif NET45
        private const string TargetFramework = "net45";
#elif NETCOREAPP2_1
        private const string TargetFramework = "netstandard16";
#elif NET5_0
        private const string TargetFramework = "netstandard21";
#endif

        private static byte[] RecompressPng(byte[] pngData)
        {
            const int SignatureLength = 8;
            const int Int32Length = 4;
            const int NameLength = 4;

            var cursor = SignatureLength;
            while (cursor + Int32Length * 2 + NameLength < pngData.Length)
            {
                var chunkLength =
                    (pngData[cursor + 0] << 24) |
                    (pngData[cursor + 1] << 16) |
                    (pngData[cursor + 2] << 8) |
                    (pngData[cursor + 3]);

                cursor += Int32Length;

                var name = Encoding.ASCII.GetString(pngData, cursor, NameLength);
                cursor += NameLength;

                if (name == PngChunkIdentifier.ImageData)
                {
                    using (var resultStream = new MemoryStream())
                    {
                        resultStream.Write(pngData, 0, cursor);

                        var dataStartIndex = (int)resultStream.Position;

                        using (var deflateStream = new ZLibStream(resultStream, CompressionMode.Compress, true))
                        {
                            using (var originalDataStream = new MemoryStream(pngData, cursor, chunkLength, false))
                            {
                                using (var inflateStream = new ZLibStream(originalDataStream, CompressionMode.Decompress))
                                {
                                    inflateStream.CopyTo(deflateStream);
                                }
                            }
                        }

                        var dataEndIndex = (int)resultStream.Position;

                        cursor += chunkLength;
                        resultStream.Write(pngData, cursor, pngData.Length - cursor);

                        var resultData = resultStream.ToArray();

                        // Update length
                        var chunkLengthOffset = dataStartIndex - Int32Length - NameLength;
                        var newChunkLength = dataEndIndex - dataStartIndex;
                        resultData[chunkLengthOffset + 0] = unchecked((byte)(newChunkLength >> 24));
                        resultData[chunkLengthOffset + 1] = unchecked((byte)(newChunkLength >> 16));
                        resultData[chunkLengthOffset + 2] = unchecked((byte)(newChunkLength >> 8));
                        resultData[chunkLengthOffset + 3] = unchecked((byte)(newChunkLength));

                        var crc32 = new Crc32();
                        crc32.Update(resultData, dataStartIndex - NameLength, dataEndIndex - dataStartIndex + NameLength);

                        var checksum = crc32.Value;

                        // Update checksum
                        resultData[dataEndIndex + 0] = unchecked((byte)(checksum >> 24));
                        resultData[dataEndIndex + 1] = unchecked((byte)(checksum >> 16));
                        resultData[dataEndIndex + 2] = unchecked((byte)(checksum >> 8));
                        resultData[dataEndIndex + 3] = unchecked((byte)(checksum));

                        return resultData;
                    }
                }

                cursor += chunkLength;
                cursor += Int32Length; // crc
            }

            throw new Exception("Could not find any " + PngChunkIdentifier.ImageData + " chunk in the specified PNG.");
        }

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
                    base64Png = Convert.ToBase64String(RecompressPng(Convert.FromBase64String(base64Png)));
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
            return Path.Combine(TestFiles.TestFilesPath, OwnTestFilesDir, InputDir, fileName);
        }

        private static string GetExpectedFilePath(string fileName)
        {
            return Path.Combine(TestFiles.TestFilesPath, OwnTestFilesDir, ExpectedDir, Path.ChangeExtension(fileName, ".svg"));
        }

        private static string GetActualFilePath(string category, string fileName)
        {
            return Path.Combine(TestFiles.TestFilesPath, OwnTestFilesDir, "actual-" + TargetFramework + "-" + category, Path.ChangeExtension(fileName, ".svg"));
        }

        private void ConvertSync(string pdfName, string expectedSvgName, SvgConversionOptions conversionOptions)
        {
            var expectedSvgPath = GetExpectedFilePath(expectedSvgName);
            var actualSvgPath = GetActualFilePath("sync", expectedSvgName);
            var pdfPath = GetInputFilePath(pdfName);

            Directory.CreateDirectory(Path.GetDirectoryName(actualSvgPath));

            string actual;
            using (var doc = PdfDocument.Open(pdfPath))
            {
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
            var actualSvgPath = GetActualFilePath("async", expectedSvgName);
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

        public static List<TestCaseData> TestCases
        {
            get
            {
                return Directory
                    .EnumerateFiles(Path.Combine(TestFiles.TestFilesPath, OwnTestFilesDir, InputDir), "*.pdf")
                    .Select(path => new TestCaseData(Path.GetFileName(path)))
                    .ToList();
            }
        }

        public static List<TestCaseData> FontTestCases
        {
            get
            {
                return Directory
                    .EnumerateFiles(Path.Combine(TestFiles.TestFilesPath, OwnTestFilesDir, InputDir), "fonts-*.pdf")
                    .Select(path => new TestCaseData(Path.GetFileName(path)))
                    .ToList();
            }
        }
    }
}
