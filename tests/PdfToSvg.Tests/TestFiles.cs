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
        private const string OwnTestFilesDirName = "Own";
        private const string InputDirName = "input";
        private const string ExpectedDirName = "expected";

#if NET40
        private const string TargetFramework = "net40";
#elif NET45
        private const string TargetFramework = "net45";
#elif NETCOREAPP2_1
        private const string TargetFramework = "netstandard16";
#elif NET5_0
        private const string TargetFramework = "netstandard21";
#elif NET6_0
        private const string TargetFramework = "net60";
#elif NET7_0
        private const string TargetFramework = "net70";
#elif NET8_0
        private const string TargetFramework = "net80";
#endif

        private const string TestFilesDirName = "TestFiles";

        static TestFiles()
        {
            var directory = TestContext.CurrentContext.WorkDirectory;

            for (var i = 0; i < 8 && !string.IsNullOrEmpty(directory); i++)
            {
                var potentialTestFileDirectory = Path.Combine(directory, TestFilesDirName);
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

        public static string TestFilesPath => Path.Combine(RootPath, TestFilesDirName);

        public static string InputDirectory => Path.Combine(TestFilesPath, OwnTestFilesDirName, InputDirName);

        public static string ExpectedDirectory => Path.Combine(TestFilesPath, OwnTestFilesDirName, ExpectedDirName);

        public static string OutputDirectory(bool sync) => Path.Combine(TestFilesPath, OwnTestFilesDirName, "actual-" + TargetFramework + (sync ? "-sync" : "-async"));
    }
}
