// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PdfToSvg.Cli
{
    internal static class Program
    {
        private static void WriteHelp()
        {
            var assembly = typeof(Program).Assembly;
            var version = assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true)
                .OfType<AssemblyInformationalVersionAttribute>()
                .Select(attr => attr.InformationalVersion)
                .FirstOrDefault();

            Console.WriteLine("pdftosvg, version {0}", version);
            Console.WriteLine("  https://github.com/dmester/pdftosvg.net/");
            Console.WriteLine("  Copyright (c) Daniel Mester Pirttijärvi");
            Console.WriteLine();
            // ------------------------------------------------------------------------------------------------|
            Console.WriteLine("Converts an input PDF file to one or multiple SVG files.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  pdftosvg.exe <input> [<output>] [<pages>]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <input>   Path to the input PDF file.");
            Console.WriteLine();
            Console.WriteLine("  <output>  Path to the output SVG file(s). A page number will be appended to ");
            Console.WriteLine("            the filename.");
            Console.WriteLine("            Default: Same as <input>, but with \".svg\" as extension.");
            Console.WriteLine();
            Console.WriteLine("  <pages>   Pages to convert. Syntax:");
            Console.WriteLine();
            Console.WriteLine("              12..15  Converts page 12 to 15.");
            Console.WriteLine("              12,15   Converts page 12 and 15.");
            Console.WriteLine("              12..    Converts page 12 and forward.");
            Console.WriteLine("              ..15    Converts page 1 to 15.");
            Console.WriteLine();
            Console.WriteLine("            Default: all pages");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  pdftosvg.exe input.pdf output.svg 1..2,9");
        }

        private static int Main(string[] args)
        {
            CommandLine commandLine;
            try
            {
                commandLine = new CommandLine(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("ERROR " + ex.Message);
                Console.Error.WriteLine("Run \"pdftosvg.exe --help\" to get help.");
                return 2;
            }

            if (commandLine.ShowHelp ||
                string.IsNullOrEmpty(commandLine.InputPath))
            {
                WriteHelp();
                return 1;
            }

            if (!File.Exists(commandLine.InputPath))
            {
                Console.Error.WriteLine("ERROR Input file not found.");
                return 3;
            }

            var outputPath = commandLine.OutputPath;

            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = commandLine.InputPath;

                if (outputPath!.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase))
                {
                    outputPath = outputPath.Substring(0, outputPath.Length - 4);
                }
            }

            var outputDir = Path.GetDirectoryName(outputPath);
            var outputFileName = Path.GetFileName(outputPath);

            if (outputFileName.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase))
            {
                outputFileName = outputFileName.Substring(0, outputFileName.Length - 4);
            }

            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var start = Stopwatch.StartNew();
            var convertedPages = 0;

            try
            {
                using (var doc = PdfDocument.Open(commandLine.InputPath!))
                {
                    IEnumerable<int> pageNumbers;

                    if (commandLine.PageRanges.Count > 0)
                    {
                        pageNumbers = commandLine.PageRanges.Pages(doc.Pages.Count);
                    }
                    else
                    {
                        pageNumbers = Enumerable.Range(1, doc.Pages.Count);
                    }

                    foreach (var pageNumber in pageNumbers)
                    {
                        var page = doc.Pages[pageNumber - 1];
                        var pageOutputPath = Path.Combine(outputDir, outputFileName + "-" + pageNumber.ToString(CultureInfo.InvariantCulture) + ".svg");
                        page.SaveAsSvg(pageOutputPath);
                        convertedPages++;
                    }
                }
            }
            catch (EncryptedPdfException)
            {
                Console.Error.WriteLine("ERROR Input file is encrypted and cannot be converted.");
                return 4;
            }

            Console.WriteLine("OK Successfully converted {0} pages to SVG in {1:0.0}s.", convertedPages, start.Elapsed.TotalSeconds);
            return 0;
        }
    }
}
