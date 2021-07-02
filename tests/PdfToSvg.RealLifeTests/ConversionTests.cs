// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PdfToSvg.RealLifeTests
{
    [TestFixture]
    public class ConversionTests
    {
        private const string TestFilesDir = "TestFiles";
        private const string ThirdPartyTestFilesDir = "3rdParty";
        private const string TestResultsDir = "TestResults";

#if NET40
        private const string TargetFramework = "net40";
#elif NET45
        private const string TargetFramework = "net45";
#elif NETCOREAPP2_1
        private const string TargetFramework = "netstandard16";
#elif NET5_0
        private const string TargetFramework = "netstandard21";
#endif

        private static readonly DateTime Started = DateTime.Now;

        private static readonly string testDir;
        private static readonly string currentResultsDir;
        private static readonly List<string> previousResultsDirs;

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

            var testResultsDirName = string.Format(CultureInfo.InvariantCulture, "{0}-{1:yyyyMMddTHHmmss}", TargetFramework, Started);
            currentResultsDir = Path.Combine(testDir, TestResultsDir, testResultsDirName);

            previousResultsDirs = Directory
                .EnumerateDirectories(Path.Combine(testDir, TestResultsDir), TargetFramework + "-*")
                .OrderByDescending(x => x)
                .ToList();
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
                        Assert.AreEqual(previousPages[i], currentPages[i]);
                    }
                });
            }
        }
    }
}
