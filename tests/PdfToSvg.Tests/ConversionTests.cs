using NUnit.Framework;
using PdfToSvg;
using PdfToSvg.Drawing;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [TestCaseSource(nameof(TestCases))]
        public void Convert(string fileName)
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
