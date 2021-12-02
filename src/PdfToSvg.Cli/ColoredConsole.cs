// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Cli
{
    internal static class ColoredConsole
    {
        public static bool NoOutputColors { get; set; }

        public static bool NoErrorColors { get; set; }

        static ColoredConsole()
        {
            var NO_COLOR = Environment.GetEnvironmentVariable("NO_COLOR");
            var TERM = Environment.GetEnvironmentVariable("TERM");

            if (NO_COLOR != null && NO_COLOR != "0" || TERM == "dumb")
            {
                NoOutputColors = true;
                NoErrorColors = true;
            }
            else
            {
                if (Console.IsOutputRedirected)
                {
                    NoOutputColors = true;
                }

                if (Console.IsErrorRedirected)
                {
                    NoErrorColors = true;
                }
            }
        }

        public static void Write(string value, ConsoleColor color)
        {
            if (NoOutputColors)
            {
                Console.Write(value);
            }
            else
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write(value);
                Console.ForegroundColor = originalColor;
            }
        }

        public static void WriteError(string value, ConsoleColor color)
        {
            if (NoErrorColors)
            {
                Console.Error.Write(value);
            }
            else
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Error.Write(value);
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
