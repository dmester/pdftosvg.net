// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace LzwEncoder
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var commandLine = new CommandLine(args);
            if (commandLine.InputPath == null && commandLine.OutputPath == null)
            {
                Console.WriteLine("Usage: [--earlychange] [--hex] <input file> <output file>");
                return;
            }

            var input = File.ReadAllBytes(commandLine.InputPath);
            var compressed = Encoder.Encode(input, commandLine.EarlyChange);

            if (commandLine.HexEncode)
            {
                using var output = new StreamWriter(commandLine.OutputPath, false, Encoding.ASCII);

                for (var i = 0; i < compressed.Length; i++)
                {
                    if ((i % 40) == 0 && i != 0)
                    {
                        output.WriteLine();
                    }

                    output.Write(compressed[i].ToString("x2"));
                }

                output.Write('>');
            }
            else
            {
                File.WriteAllBytes(commandLine.OutputPath, compressed);
            }

            Console.WriteLine("Compressing done.");
        }
    }
}
