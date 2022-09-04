// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompressCMaps
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("CompressCMaps <CMap resource dir> <output file>");
                return 1;
            }

            var cmapDirectory = args[0];
            var outputFile = args[1];

            var cmaps = new List<CMapData>();

            var licenseBanner =
                "\n" +
                "-----------------------------------------------------------\n" +
                File.ReadAllText(Path.Combine(cmapDirectory, "license.md")) +
                "-----------------------------------------------------------\n";

            foreach (var fileInfo in new DirectoryInfo(cmapDirectory).EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (!PredefinedCMaps.Contains(fileInfo.Name))
                {
                    continue;
                }

                using var stream = fileInfo.OpenRead();

                var preambleBytes = new byte[100];
                stream.Read(preambleBytes, 0, 100);

                var preamble = Encoding.ASCII.GetString(preambleBytes);

                if (!preamble.StartsWith("%!PS-Adobe-3.0 Resource-CMap"))
                {
                    continue;
                }

                stream.Position = 0;

                cmaps.Add(CMapParser.Parse(stream, default));
            }

            var pack = CMapPackBuilder.Pack(licenseBanner, cmaps);
            File.WriteAllBytes(outputFile, pack);

            return 0;
        }
    }
}
