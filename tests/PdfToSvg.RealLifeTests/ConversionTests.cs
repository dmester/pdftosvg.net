// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
#if HAVE_SKIA
using SkiaSharp;
using Svg.Skia;
#endif

namespace PdfToSvg.RealLifeTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class ConversionTests
    {
        private const string TestFilesDir = "TestFiles";
        private const string ThirdPartyTestFilesDir = "3rdParty";
        private const string TestResultsDir = "TestResults";

#if NET40
        private const string TargetFramework = "net40";
#elif NET45
        private const string TargetFramework = "net45";
#elif NETCOREAPP3_1
        private const string TargetFramework = "netstandard20";
#elif NET5_0
        private const string TargetFramework = "net50";
#elif NET6_0
        private const string TargetFramework = "netstandard16";
#elif NET7_0
        private const string TargetFramework = "net70";
#elif NET8_0
        private const string TargetFramework = "net80";
#endif

        private static readonly DateTime Started = DateTime.Now;

        private static readonly string testDir;
        private static readonly string currentResultsDir;
        private static readonly List<string> previousResultsDirs;

        private int counter;

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

            var testResultsDir = Path.Combine(testDir, TestResultsDir);
            var testResultsDirName = string.Format(CultureInfo.InvariantCulture, "{0}-{1:yyyyMMddTHHmmss}", TargetFramework, Started);
            currentResultsDir = Path.Combine(testResultsDir, testResultsDirName);

            if (Directory.Exists(testResultsDir))
            {
                previousResultsDirs = Directory
                    .EnumerateDirectories(testResultsDir, TargetFramework + "-*")
                    .OrderByDescending(x => x)
                    .ToList();
            }
            else
            {
                previousResultsDirs = new();
            }
        }

        public static List<TestCaseData> TestCases
        {
            get
            {
                return Directory
                    .EnumerateFiles(Path.Combine(testDir, TestFilesDir, ThirdPartyTestFilesDir), "*.pdf")
                    .Select(path => new TestCaseData(Path.GetFileName(path)))
                    .ToList();
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            WriteDiffHtml();
            CleanUpOldResults();
        }

        private void CleanUpOldResults()
        {
            var testFilesDir = Path.Combine(testDir, TestFilesDir, ThirdPartyTestFilesDir);

            var expectedResultsCount =
                Directory.EnumerateFiles(testFilesDir, "*.pdf").Count();

            var fullResultsCount = 0;

            foreach (var oldTestResult in previousResultsDirs)
            {
                if (fullResultsCount < 2)
                {
                    var fileCount = Directory.EnumerateFiles(oldTestResult, "*.svg").Count();
                    var isFullResult = fileCount > expectedResultsCount / 2;

                    if (isFullResult)
                    {
                        fullResultsCount++;
                    }

                    continue;
                }

                try
                {
                    Directory.Delete(oldTestResult, true);
                }
                catch
                {
                }
            }
        }

        private void WriteDiffHtml()
        {
            var assemblyPath = typeof(ConversionTests).Assembly.Location;
            var templatePath = Path.Combine(Path.GetDirectoryName(assemblyPath), "diff-template.html");
            var template = File.ReadAllText(templatePath);
            var diffPath = Path.Combine(currentResultsDir, "_diff.html");

            template = template.Replace("fileCount = 0", "fileCount = " + counter);

            File.WriteAllText(diffPath, template);
        }

        private static void AssertSvgEqual(string expected, string actual, string context)
        {
            if (expected == actual)
            {
                return;
            }

            if (expected == null || actual == null)
            {
                Assert.AreEqual(expected, actual, context);
                return;
            }

            var expectedText = ExtractSvgText(expected);
            var actualText = ExtractSvgText(actual);
            var textEqual = expectedText.SequenceEqual(actualText);

            Assert.AreEqual(expectedText, actualText, context + ": SVG text differs.");

            var imageEqual = true;
#if HAVE_SKIA
            imageEqual = AssertSvgImagesEqual(expected, actual, context);
#endif

            if (textEqual && imageEqual)
            {
                Assert.AreEqual(expected, actual, context);
            }
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
        private static bool AssertSvgImagesEqual(string expected, string actual, string context)
        {
            try
            {
                using var expectedImage = RenderSvg(expected);
                using var actualImage = RenderSvg(actual);

                if (expectedImage.Width != actualImage.Width || expectedImage.Height != actualImage.Height)
                {
                    Assert.Fail(
                        $"{context}: SVG renderings have different dimensions. " +
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
                        $"{context}: SVG renderings differ in {differentPixelCount} of {totalPixelCount} pixels " +
                        $"({differentPercent:0.####}%). Difference bounds: x={minX}, y={minY}, " +
                        $"width={maxX - minX + 1}, height={maxY - minY + 1}.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Assert.Fail($"{context}: Failed to render SVGs with SkiaSharp: {ex.Message}");
                return false;
            }
        }

        private static SKBitmap RenderSvg(string svg)
        {
            using var svgDocument = new SKSvg();
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

        [TestCaseSource(nameof(TestCases))]
        public void ConvertSync(string fileName)
        {
            var inputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            var testFilesDir = Path.Combine(testDir, TestFilesDir, ThirdPartyTestFilesDir);

            var previousResultsDir = previousResultsDirs
                .Where(x => Directory.EnumerateFiles(x, inputFileNameWithoutExtension + "-*").Any())
                .FirstOrDefault();

            Directory.CreateDirectory(currentResultsDir);

            using (var doc = PdfDocument.Open(Path.Combine(testFilesDir, fileName)))
            {
                var previousPages = new string[doc.Pages.Count];
                var currentPages = new string[doc.Pages.Count];

                Parallel.ForEach(doc.Pages, new ParallelOptions { MaxDegreeOfParallelism = 2 }, (page, _, pageIndex) =>
                {
                    var pageNo = pageIndex + 1;

                    var outputFileName = inputFileNameWithoutExtension + "-sync-" + pageNo + ".svg";
                    var outputPath = Path.Combine(currentResultsDir, outputFileName);

                    if (previousResultsDir != null)
                    {
                        var previousOutputPath = Path.Combine(previousResultsDir, outputFileName);
                        if (File.Exists(previousOutputPath))
                        {
                            previousPages[pageIndex] = File.ReadAllText(previousOutputPath);
                        }
                    }

                    page.SaveAsSvg(outputPath);

                    currentPages[pageIndex] = File.ReadAllText(outputPath);
                });

                Assert.Multiple(() =>
                {
                    for (var i = 0; i < previousPages.Length; i++)
                    {
                        if (previousPages[i] != null &&
                            previousPages[i] != currentPages[i])
                        {
                            var id = Interlocked.Increment(ref counter) - 1;

                            var scriptPath = Path.Combine(currentResultsDir, "diff-" + id + ".js");
                            var script = "load(" + JsonConvert.SerializeObject(new
                            {
                                Id = id,
                                Name = fileName + " page " + (i + 1),
                                Before = previousPages[i],
                                After = currentPages[i],
                            }) + ")";

                            File.WriteAllText(scriptPath, script);
                        }

                        AssertSvgEqual(previousPages[i], currentPages[i], fileName + " page " + (i + 1));
                    }
                });
            }
        }
    }
}
