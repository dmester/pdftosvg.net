// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Cli
{
    internal static class Program
    {
        private static string? version = typeof(Program)
            .Assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true)
            .OfType<AssemblyInformationalVersionAttribute>()
            .Select(attr => attr.InformationalVersion)
            .FirstOrDefault();

        private static void WriteHelp()
        {
            Console.WriteLine("pdftosvg, version {0}", version);
            Console.WriteLine("  https://github.com/dmester/pdftosvg.net/");
            Console.WriteLine("  Copyright (c) Daniel Mester Pirttijärvi");
            Console.WriteLine();
            CommandLine.WriteHelp();
        }

        private static string? ReadPassword(string label)
        {
            Console.Write(label);

            var password = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        Console.WriteLine();
                        return password.ToString();

                    case ConsoleKey.Escape:
                        Console.WriteLine();
                        Console.WriteLine();
                        return null;

                    case ConsoleKey.Backspace:
                        if (password.Length > 0)
                        {
                            password.Length--;
                            Console.Write("\b \b");
                        }
                        break;

                    default:
                        password.Append(key.KeyChar);
                        Console.Write('*');
                        break;
                }
            }
        }

        private static async Task<int> Main(string[] args)
        {
            // Error reporting is easier when all exceptions are in English
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            CommandLine commandLine;
            try
            {
                commandLine = new CommandLine(args);

                if (commandLine.NoColor)
                {
                    ColoredConsole.NoOutputColors = true;
                    ColoredConsole.NoErrorColors = true;
                }
            }
            catch (ArgumentException ex)
            {
                ColoredConsole.WriteError("ERROR ", ConsoleColor.Red);
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine("Run \"pdftosvg.exe --help\" to get help.");
                return 2;
            }

            var interactive = !commandLine.NonInteractive && Environment.UserInteractive && !Console.IsInputRedirected;

            if (commandLine.ShowHelp ||
                string.IsNullOrEmpty(commandLine.InputPath))
            {
                WriteHelp();
                return 1;
            }

            if (!File.Exists(commandLine.InputPath))
            {
                ColoredConsole.WriteError("ERROR ", ConsoleColor.Red);
                Console.Error.WriteLine("Input file not found.");
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

            var openOptions = new OpenOptions { Password = commandLine.Password };

        TryAgain:
            var start = Stopwatch.StartNew();
            var convertedPages = 0;

            try
            {
                using (var doc = await PdfDocument.OpenAsync(commandLine.InputPath!, openOptions))
                {
                    if (!doc.Permissions.HasOwnerPermission &&
                        !doc.Permissions.AllowExtractContent)
                    {
                        if (interactive)
                        {
                            Console.WriteLine("The document author does not allow extraction of content from the specified");
                            Console.WriteLine("  document. If you are the author, you can specify the owner password to proceed");
                            Console.WriteLine("  with the conversion.");

                            Console.WriteLine();
                            openOptions.Password = ReadPassword("Enter owner password: ");
                            goto TryAgain;
                        }
                        else
                        {
                            ColoredConsole.WriteError("ERROR ", ConsoleColor.Red);

                            Console.Error.WriteLine("The document author does not allow extraction of content from the");
                            Console.Error.WriteLine("  specified document. If you are the author, you can specify the owner password");
                            Console.Error.WriteLine("  using the --password command line option to proceed with the conversion.");

                            return 5;
                        }
                    }

                    IEnumerable<int> pageNumbers;

                    if (commandLine.PageRanges.Count > 0)
                    {
                        pageNumbers = commandLine.PageRanges.Pages(doc.Pages.Count);
                    }
                    else
                    {
                        pageNumbers = Enumerable.Range(1, doc.Pages.Count);
                    }

                    var pageCount = pageNumbers.Count();

                    ProgressReporter progress;

                    if (commandLine.NonInteractive)
                    {
                        progress = ProgressReporter.CreateNullReporter();
                    }
                    else
                    {
                        progress = ProgressReporter.CreateCliProgressBar("Converting PDF...");
                    }

                    await ParallelUtils.ForEachAsync(pageNumbers, async (pageNumber, _) =>
                    {
                        var page = doc.Pages[pageNumber - 1];
                        var pageOutputPath = outputFileName + "-" + pageNumber.ToString(CultureInfo.InvariantCulture) + ".svg";

                        if (!string.IsNullOrEmpty(outputDir))
                        {
                            pageOutputPath = Path.Combine(outputDir, pageOutputPath);
                        }

                        await Task.Yield();
                        await page.SaveAsSvgAsync(pageOutputPath, commandLine.ConversionOptions);

                        lock (progress)
                        {
                            convertedPages++;
                            progress.ProgressPercent = 100 * convertedPages / pageCount;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                if (ex is AggregateException aex)
                {
                    ex = aex.InnerException ?? ex;
                }

                if (ex is InvalidCredentialException)
                {
                    if (interactive)
                    {
                        if (string.IsNullOrEmpty(openOptions.Password))
                        {
                            Console.WriteLine("Input file is encrypted and requires a password to open.");
                        }
                        else
                        {
                            Console.WriteLine("The specified password is not correct. Try again.");
                        }

                        Console.WriteLine();
                        openOptions.Password = ReadPassword("Enter password: ");
                        goto TryAgain;
                    }
                    else
                    {
                        ColoredConsole.WriteError("ERROR ", ConsoleColor.Red);

                        if (string.IsNullOrEmpty(openOptions.Password))
                        {
                            Console.Error.WriteLine("Input file is encrypted and requires a password to open. You can specify");
                            Console.Error.WriteLine("  the password by using the --password command line option.");
                        }
                        else
                        {
                            Console.Error.WriteLine("The specified password is not correct.");
                        }
                    }

                    return 4;
                }
                else
                {
                    ColoredConsole.WriteError("ERROR ", ConsoleColor.Red);

                    Console.Error.WriteLine("Something went terribly wrong. This is either because the PDF is malformed,");
                    Console.Error.WriteLine("  or a bug in PdfToSvg.NET. If you think it is a bug, consider submitting a bug");
                    Console.Error.WriteLine("  report. Please include the failing PDF file and the error information below.");
                    Console.Error.WriteLine();
                    Console.Error.WriteLine("ISSUE TRACKER");
                    Console.Error.WriteLine("https://github.com/dmester/pdftosvg.net/issues");
                    Console.Error.WriteLine();
                    Console.Error.WriteLine("ERROR INFORMATION");
                    Console.Error.WriteLine("Version: PdfToSvg.NET " + version);
                    Console.Error.WriteLine("Operating system: " + Environment.OSVersion);
                    Console.Error.WriteLine("Architecture: " + (Environment.Is64BitProcess ? "64" : "32") + " bit");
                    Console.Error.WriteLine(ex.ToString());

                    return 6;
                }
            }

            ColoredConsole.Write("OK ", ConsoleColor.Green);
            Console.WriteLine("Successfully converted {0} pages to SVG in {1:0.0}s.", convertedPages, start.Elapsed.TotalSeconds);
            return 0;
        }
    }
}
