// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests
{
    internal static class TestFiles
    {
        private const string TestFilesDir = "TestFiles";

        static TestFiles()
        {
            var directory = TestContext.CurrentContext.WorkDirectory;

            for (var i = 0; i < 8 && !string.IsNullOrEmpty(directory); i++)
            {
                var potentialTestFileDirectory = Path.Combine(directory, TestFilesDir);
                if (Directory.Exists(potentialTestFileDirectory))
                {
                    RootPath = directory;
                    break;
                }

                directory = Path.GetDirectoryName(directory);
            }

            if (RootPath == null)
            {
                throw new DirectoryNotFoundException("Could not find test files directory.");
            }
        }

        public static string RootPath { get; }

        public static string TestFilesPath => Path.Combine(RootPath, TestFilesDir);
    }
}
