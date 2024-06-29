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
                Console.WriteLine();
                Console.WriteLine("You can find the needed resource files here:");
                Console.WriteLine();
                Console.WriteLine(" * https://github.com/adobe-type-tools/cmap-resources");
                Console.WriteLine(" * https://github.com/adobe-type-tools/mapping-resources-pdf");
                Console.WriteLine();
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


            var pdfToUnicodeCMaps = new HashSet<string>
            {
                "Adobe-CNS1-UCS2",
                "Adobe-GB1-UCS2",
                "Adobe-Japan1-UCS2",
                "Adobe-Korea1-UCS2",
            };

            foreach (var fileInfo in new DirectoryInfo(cmapDirectory).EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (!PredefinedCMaps.Contains(fileInfo.Name) && !pdfToUnicodeCMaps.Contains(fileInfo.Name))
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
