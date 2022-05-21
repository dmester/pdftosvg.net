// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Cli
{
    internal class CommandLine
    {
        public CommandLine(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg == "-h" || arg == "/?" || arg == "--help" ||
                    args.Length == 1 && arg == "help")
                {
                    ShowHelp = true;
                    break;
                }

                if (arg == "--password" && i + 1 < args.Length)
                {
                    Password = args[++i];
                    continue;
                }

                if (InputPath != null && PageRange.TryParse(arg, out var ranges))
                {
                    PageRanges.AddRange(ranges);
                    continue;
                }

                if (InputPath == null)
                {
                    InputPath = arg;
                    if (i + 1 >= args.Length)
                        OutputPath = arg.Replace(".pdf", ".svg");
                    continue;
                }

                if (OutputPath == null)
                {
                    OutputPath = arg;
                    continue;
                }

                throw new ArgumentException("Unknown argument \"" + arg + "\".");
            }
        }

        public bool ShowHelp { get; }

        public string? InputPath { get; }

        public string? OutputPath { get; }

        public string? Password { get; }

        public List<PageRange> PageRanges { get; } = new List<PageRange>();
    }
}
