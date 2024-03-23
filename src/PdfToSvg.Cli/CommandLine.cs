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

                bool BooleanArgument(string key, string value)
                {
                    return value.ToLowerInvariant() switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => throw new ArgumentException("Invalid value \"" + value + "\" for option " + key + ".")
                    };
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

                if (key == "--non-interactive")
                {
                    NonInteractive = true;
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

                if (key == "--include-fonts" && TryReadValue(out value))
                {
                    ConversionOptions.FontResolver = value.ToLowerInvariant() switch
                    {
                        "true" => FontResolver.EmbedWoff,
                        "false" => FontResolver.LocalFonts,
                        "embed-woff" => FontResolver.EmbedWoff,
                        "embed-opentype" => FontResolver.EmbedOpenType,
                        "none" => FontResolver.LocalFonts,
                        _ => throw new ArgumentException("Invalid value \"" + value + "\" for option --include-fonts.")
                    };
                    continue;
                }

                if (key == "--include-links" && TryReadValue(out value))
                {
                    ConversionOptions.IncludeLinks = BooleanArgument(key, value);
                    continue;
                }

                if (key == "--include-annotations" && TryReadValue(out value))
                {
                    ConversionOptions.IncludeAnnotations = BooleanArgument(key, value);
                    continue;
                }

                if (key == "--include-hidden-text" && TryReadValue(out value))
                {
                    ConversionOptions.IncludeHiddenText = BooleanArgument(key, value);
                    continue;
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

        public bool NonInteractive { get; }

        public string? InputPath { get; }

        public string? OutputPath { get; }

        public string? Password { get; }

        public SvgConversionOptions ConversionOptions { get; } = new();

        public List<PageRange> PageRanges { get; } = new List<PageRange>();

        public static void WriteHelp()
        {
            // ------------------------------------------------------------------------------------------------|
            Console.WriteLine("Converts an input PDF file to one or multiple SVG files.");
            Console.WriteLine();
            Console.WriteLine("USAGE");
            Console.WriteLine("  pdftosvg [OPTIONS...] <input> [<output>]");
            Console.WriteLine();
            Console.WriteLine("OPTIONS");
            Console.WriteLine("  <input>     Path to the input PDF file.");
            Console.WriteLine();
            Console.WriteLine("  <output>    Path to the output SVG file(s). A page number will be appended to");
            Console.WriteLine("              the filename.");
            Console.WriteLine();
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
            Console.WriteLine("  --non-interactive");
            Console.WriteLine("              Disables any interactive prompts and progress reports.");
            Console.WriteLine();
            Console.WriteLine("CONVERSION OPTIONS");
            Console.WriteLine("  --include-fonts <value>");
            Console.WriteLine("              Specifies how fonts from the PDF should be embedded in SVG:");
            Console.WriteLine();
            Console.WriteLine("                none             Only local fonts will be used");
            Console.WriteLine("                embed-woff       Fonts will be embedded as WOFF fonts");
            Console.WriteLine("                embed-opentype   Fonts will be embedded as OpenType fonts");
            Console.WriteLine();
            Console.WriteLine("              Default: embed-woff");
            Console.WriteLine();
            Console.WriteLine("  --include-links <true|false>");
            Console.WriteLine("              Determines whether web links from the PDF document will be");
            Console.WriteLine("              included in the generated SVG. Note that this property only");
            Console.WriteLine("              affects links to websites. Other types of links, including links");
            Console.WriteLine("              within the document, are currently not supported.");
            Console.WriteLine();
            Console.WriteLine("              Default: true");
            Console.WriteLine();
            Console.WriteLine("  --include-annotations <true|false>");
            Console.WriteLine("              Determines whether annotations drawn in the PDF document should be");
            Console.WriteLine("              included in the generated SVG.");
            Console.WriteLine();
            Console.WriteLine("              Default: true");
            Console.WriteLine();
            Console.WriteLine("  --include-hidden-text <true|false>");
            Console.WriteLine("              Determines whether hidden text from the PDF document will be ");
            Console.WriteLine("              included in the generated SVG.");
            Console.WriteLine();
            Console.WriteLine("              Default: true");
            Console.WriteLine();
            Console.WriteLine("EXAMPLE");
            Console.WriteLine("  pdftosvg.exe input.pdf output.svg --pages 1..2,9");
        }
    }
}
