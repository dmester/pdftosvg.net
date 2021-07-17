// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg.Tests
{
    public class ConversionTests
    {
        private const string TestFilesDir = "TestFiles";
        private const string OwnTestFilesDir = "Own";
        private const string InputDir = "input";
        private const string ExpectedDir = "expected";

        private static readonly string testDir;

#if NET40
        private const string TargetFramework = "net40";
#elif NET45
        private const string TargetFramework = "net45";
#elif NETCOREAPP2_1
        private const string TargetFramework = "netstandard16";
#elif NET5_0
        private const string TargetFramework = "netstandard21";
#endif

        static ConversionTests()
        {
            var directory = TestContext.CurrentContext.WorkDirectory;

            for (var i = 0; i < 8 && !string.IsNullOrEmpty(directory); i++)
            {
                var potentialTestFileDirectory = Path.Combine(directory, TestFilesDir);
                if (Directory.Exists(potentialTestFileDirectory))
                {
                    testDir = directory;
                    break;
                }

                directory = Path.GetDirectoryName(directory);
            }

            if (testDir == null)
            {
                throw new DirectoryNotFoundException("Could not find test files directory.");
            }
        }

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
            const string DataUriPrefix = "data:image/png;base64,";
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

                if (hrefAttribute != null && hrefAttribute.Value.StartsWith(DataUriPrefix))
                {
                    var base64Png = hrefAttribute.Value.Substring(DataUriPrefix.Length);
                    base64Png = Convert.ToBase64String(RecompressPng(Convert.FromBase64String(base64Png)));
                    hrefAttribute.Value = DataUriPrefix + base64Png;

                    var oldId = image.Attribute("id").Value;
                    var newId = StableID.Generate("im", hrefAttribute.Value, interpolated);

                    image.SetAttributeValue("id", newId);

                    foreach (var reference in useElements["#" + oldId])
                    {
                        reference.SetAttributeValue("href", "#" + newId);

                        if (reference.Parent.Name.LocalName == "mask")
                        {
                            var newMaskId = StableID.Generate("m", newId);
                            var oldMaskId = reference.Parent.Attribute("id").Value;

                            reference.Parent.SetAttributeValue("id", newMaskId);

                            foreach (var maskReference in maskReferences["url(#" + oldMaskId + ")"])
                            {
                                maskReference.SetAttributeValue("mask", "url(#" + newMaskId + ")");
                            }
                        }
                    }
                }
            }

            return svg.ToString(SaveOptions.DisableFormatting);
        }

        private class TestFontResolver : FontResolver
        {
            public override Font ResolveFont(string fontName, CancellationToken cancellationToken)
            {
                if (fontName == "Times-Bold")
                {
                    return new WebFont(
                        fallbackFont: new LocalFont("'Times New Roman',sans-serif", FontWeight.Bold),
                        woffUrl: "http://pdftosvg.net/assets/sourcesanspro-bold-webfont.woff",
                        woff2Url: "http://pdftosvg.net/assets/sourcesanspro-bold-webfont.woff2",
                        trueTypeUrl: "http://pdftosvg.net/assets/sourcesanspro-bold-webfont.ttf");
                }
                else
                {
                    return new WebFont(
                        fallbackFont: new LocalFont("'Times New Roman',sans-serif"),
                        woffUrl: "http://pdftosvg.net/assets/sourcesanspro-regular-webfont.woff",
                        woff2Url: "http://pdftosvg.net/assets/sourcesanspro-regular-webfont.woff2",
                        trueTypeUrl: "http://pdftosvg.net/assets/sourcesanspro-regular-webfont.ttf");
                }
            }
        }

        private static string GetInputFilePath(string fileName)
        {
            return Path.Combine(testDir, TestFilesDir, OwnTestFilesDir, InputDir, fileName);
        }

        private static string GetExpectedFilePath(string fileName)
        {
            return Path.Combine(testDir, TestFilesDir, OwnTestFilesDir, ExpectedDir, Path.ChangeExtension(fileName, ".svg"));
        }

        private static string GetActualFilePath(string category, string fileName)
        {
            return Path.Combine(testDir, TestFilesDir, OwnTestFilesDir, "actual-" + TargetFramework + "-" + category, Path.ChangeExtension(fileName, ".svg"));
        }

        [Test]
        public void WebFontConversion()
        {
            var expectedSvgPath = GetExpectedFilePath("encoding-webfont.svg");
            var actualSvgPath = GetActualFilePath("sync", "encoding-webfont.svg");
            var pdfPath = GetInputFilePath("encoding.pdf");

            Directory.CreateDirectory(Path.GetDirectoryName(actualSvgPath));

            string actual;
            using (var doc = PdfDocument.Open(pdfPath))
            {
                actual = doc.Pages[0].ToSvgString(new SvgConversionOptions
                {
                    FontResolver = new TestFontResolver(),
                });
            }

            actual = RecompressPngs(actual);
            File.WriteAllText(actualSvgPath, actual, Encoding.UTF8);

            var expected = File.Exists(expectedSvgPath) ? RecompressPngs(File.ReadAllText(expectedSvgPath, Encoding.UTF8)) : null;

            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(nameof(TestCases))]
        public void ConvertSync(string fileName)
        {
            var expectedSvgPath = GetExpectedFilePath(fileName);
            var actualSvgPath = GetActualFilePath("sync", fileName);
            var pdfPath = GetInputFilePath(fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(actualSvgPath));

            string actual;

            using (var doc = PdfDocument.Open(pdfPath))
            {
                actual = doc.Pages[0].ToSvgString();
            }

            actual = RecompressPngs(actual);
            File.WriteAllText(actualSvgPath, actual, Encoding.UTF8);

            var expected = File.Exists(expectedSvgPath) ? RecompressPngs(File.ReadAllText(expectedSvgPath, Encoding.UTF8)) : null;

            Assert.AreEqual(expected, actual);
        }

#if !NET40
        [TestCaseSource(nameof(TestCases))]
        public async Task ConvertAsync(string fileName)
        {
            var expectedSvgPath = GetExpectedFilePath(fileName);
            var actualSvgPath = GetActualFilePath("async", fileName);
            var pdfPath = GetInputFilePath(fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(actualSvgPath));

            string actual;

            using (var doc = await PdfDocument.OpenAsync(pdfPath))
            {
                actual = await doc.Pages[0].ToSvgStringAsync();
            }

            actual = RecompressPngs(actual);
            File.WriteAllText(actualSvgPath, actual, Encoding.UTF8);

            var expected = File.Exists(expectedSvgPath) ? RecompressPngs(File.ReadAllText(expectedSvgPath, Encoding.UTF8)) : null;

            Assert.AreEqual(expected, actual);
        }
#endif

        public static List<TestCaseData> TestCases
        {
            get
            {
                return Directory
                    .EnumerateFiles(Path.Combine(testDir, TestFilesDir, OwnTestFilesDir, InputDir), "*.pdf")
                    .Select(path => new TestCaseData(Path.GetFileName(path)))
                    .ToList();
            }
        }
    }
}
