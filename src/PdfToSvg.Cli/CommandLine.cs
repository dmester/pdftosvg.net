// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PdfToSvg.Cli
{
    internal class CommandLine
    {
        public CommandLine(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                string key = args[i];
                string? value = null;

                bool TryReadValue([NotNullWhen(true)] out string? result)
                {
                    if (value != null)
                    {
                        result = value;
                        return true;
                    }
                    else if (i + 1 < args.Length)
                    {
                        result = args[++i];
                        return true;
                    }
                    else
                    {
                        result = null;
                        return false;
                    }
                }

                var optionWithValue = Regex.Match(key, "^(--?[a-z-]+)=(.+)");
                if (optionWithValue.Success)
                {
                    key = optionWithValue.Groups[1].Value;
                    value = optionWithValue.Groups[2].Value;
                }

                if (key == "-h" || key == "/?" || key == "/h" || key == "--help" ||
                    args.Length == 1 && args[0] == "help")
                {
                    ShowHelp = true;
                    break;
                }

                if (key == "--password" && TryReadValue(out value))
                {
                    Password = value;
                    continue;
                }

                if (key == "--no-color")
                {
                    NoColor = true;
                    continue;
                }

                if ((key == "--pages" || key == "-p") && TryReadValue(out value))
                {
                    if (!PageRange.TryParse(value, out var pageRanges))
                    {
                        throw new ArgumentException("Invalid page range \"" + value + "\".");
                    }
                    else
                    {
                        PageRanges.AddRange(pageRanges);
                        continue;
                    }
                }

                if (InputPath == null)
                {
                    InputPath = key;
                    continue;
                }

                if (OutputPath == null)
                {
                    OutputPath = key;
                    continue;
                }

                throw new ArgumentException("Unknown argument \"" + key + "\".");
            }
        }

        public bool ShowHelp { get; }

        public bool NoColor { get; }

        public string? InputPath { get; }

        public string? OutputPath { get; }

        public string? Password { get; }

        public List<PageRange> PageRanges { get; } = new List<PageRange>();

        public static void WriteHelp()
        {
            // ------------------------------------------------------------------------------------------------|
            Console.WriteLine("Converts an input PDF file to one or multiple SVG files.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  pdftosvg.exe [OPTIONS...] <input> [<output>]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <input>     Path to the input PDF file.");
            Console.WriteLine();
            Console.WriteLine("  <output>    Path to the output SVG file(s). A page number will be appended to");
            Console.WriteLine("              the filename.");
            Console.WriteLine("              Default: Same as <input>, but with \".svg\" as extension.");
            Console.WriteLine();
            Console.WriteLine("  --pages <pages>");
            Console.WriteLine("              Pages to convert. Syntax:");
            Console.WriteLine();
            Console.WriteLine("                12..15  Converts page 12 to 15.");
            Console.WriteLine("                12,15   Converts page 12 and 15.");
            Console.WriteLine("                12..    Converts page 12 and forward.");
            Console.WriteLine("                ..15    Converts page 1 to 15.");
            Console.WriteLine();
            Console.WriteLine("              Default: all pages");
            Console.WriteLine();
            Console.WriteLine("  --password \"<password>\"");
            Console.WriteLine("              Owner or user password for opening the input file. By specifying");
            Console.WriteLine("              the owner password, any access restrictions are bypassed.");
            Console.WriteLine();
            Console.WriteLine("  --no-color  Disables colored text output in the console.");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  pdftosvg.exe input.pdf output.svg --pages 1..2,9");
        }
    }
}
