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
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg.Tests
{
    class ConversionTests
    {
        private static string GetTargetFramework()
        {
            var directory = TestContext.CurrentContext.TestDirectory;
            return Path.GetFileName(directory).Replace(".", "");
        }

        private static string GetTestFileDirectory()
        {
            var directory = TestContext.CurrentContext.TestDirectory;
            
            for (var i = 0; i < 8 && !string.IsNullOrEmpty(directory); i++)
            {
                var potentialTestFileDirectory = Path.Combine(directory, "Test-files");
                if (Directory.Exists(potentialTestFileDirectory))
                {
                    return potentialTestFileDirectory;
                }

                directory = Path.GetDirectoryName(directory);
            }

            throw new DirectoryNotFoundException("Could not find test files directory.");
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

            foreach (var image in svg.Descendants(ns + "image"))
            {
                var hrefAttribute = image.Attribute("href");

                if (hrefAttribute != null && hrefAttribute.Value.StartsWith(DataUriPrefix))
                {
                    var base64Png = hrefAttribute.Value.Substring(DataUriPrefix.Length);
                    base64Png = Convert.ToBase64String(RecompressPng(Convert.FromBase64String(base64Png)));
                    hrefAttribute.Value = DataUriPrefix + base64Png;

                    var oldId = image.Attribute("id").Value;
                    var newId = StableID.Generate("im", hrefAttribute.Value);
                    
                    image.SetAttributeValue("id", newId);

                    foreach (var reference in useElements["#" + oldId])
                    {
                        reference.SetAttributeValue("href", "#" + newId);
                    }
                }
            }

            return svg.ToString(SaveOptions.DisableFormatting);
        }

        [TestCaseSource(nameof(TestCases))]
        public void ConvertSync(string fileName)
        {
            var testFileDirectory = GetTestFileDirectory();
            var targetFramework = GetTargetFramework();

            var expectedSvgPath = Path.Combine(testFileDirectory, Path.ChangeExtension(fileName, ".svg"));
            var actualSvgPath = Path.Combine(testFileDirectory, Path.ChangeExtension(fileName, null)) + "-actual-" + targetFramework + "-sync.svg";

            var expected = File.Exists(expectedSvgPath) ? File.ReadAllText(expectedSvgPath, Encoding.UTF8) : "<non-existing>";
            
            string actual;

            var pdfPath = Path.Combine(testFileDirectory, fileName);
            using (var doc = PdfDocument.Open(pdfPath))
            {
                actual = doc.Pages[0].ToSvg();
            }

            actual = RecompressPngs(actual);
            expected = RecompressPngs(expected);

            File.WriteAllText(actualSvgPath, actual, Encoding.UTF8);
            Assert.AreEqual(expected, actual);
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task ConvertAsync(string fileName)
        {
            var testFileDirectory = GetTestFileDirectory();
            var targetFramework = GetTargetFramework();

            var expectedSvgPath = Path.Combine(testFileDirectory, Path.ChangeExtension(fileName, ".svg"));
            var actualSvgPath = Path.Combine(testFileDirectory, Path.ChangeExtension(fileName, null)) + "-actual-" + targetFramework + "-async.svg";

            var expected = File.Exists(expectedSvgPath) ? File.ReadAllText(expectedSvgPath, Encoding.UTF8) : "<non-existing>";

            string actual;

            var pdfPath = Path.Combine(testFileDirectory, fileName);

            using (var doc = await PdfDocument.OpenAsync(pdfPath))
            {
                actual = await doc.Pages[0].ToSvgAsync();
            }

            actual = RecompressPngs(actual);
            expected = RecompressPngs(expected);

            File.WriteAllText(actualSvgPath, actual, Encoding.UTF8);
            Assert.AreEqual(expected, actual);
        }

        public static List<TestCaseData> TestCases
        {
            get
            {
                return Directory
                    .EnumerateFiles(GetTestFileDirectory(), "*.pdf")
                    .Select(path => new TestCaseData(Path.GetFileName(path)))
                    .ToList();
            }
        }
    }
}
