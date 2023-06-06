// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CcittFaxEncoder
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var inputPath = args.Where(x => !x.StartsWith("--")).FirstOrDefault();
            var outputPath = args.Where(x => !x.StartsWith("--")).Skip(1).FirstOrDefault();

            if (inputPath == null || outputPath == null)
            {
                Console.WriteLine("CcittFaxEncoder");
                Console.WriteLine("Creates test cases for CCITTFaxDecode.");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  CcittFaxEncoder <input> <output> <options>");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine();
                Console.WriteLine("  <input>            Path to 32 bit bmp");
                Console.WriteLine("  <output>           Path to PDF encoded image");
                Console.WriteLine();
                Console.WriteLine("  --K=<num>          K value.");
                Console.WriteLine();
                Console.WriteLine("  --EncodedByteAlign Aligns rows at byte boundaries.");
                Console.WriteLine();
                Console.WriteLine("  --EndOfLine        Produces end-of-line markers.");
                Console.WriteLine();
                Console.WriteLine("  --EndOfBlock       Produces an end-of-block marker.");
                Console.WriteLine();
                Console.WriteLine("  --BlackIs1         Encodes black pixels as 1 instead of 0.");
                Console.WriteLine();
                return;
            }

            var K = args
                .Where(x => x.StartsWith("--K="))
                .Select(x => int.Parse(x.Substring(4), CultureInfo.InvariantCulture))
                .DefaultIfEmpty()
                .First();

            var endOfLine = args.Contains("--EndOfLine");
            var encodedByteAlign = args.Contains("--EncodedByteAlign");
            var endOfBlock = args.Contains("--EndOfBlock");
            var blackIs1 = args.Contains("--BlackIs1");

            var reader = new BitmapReader(File.ReadAllBytes(inputPath));
            var encoder = new FaxEncoder();

            var decodeParms = new Dictionary<string, object>
            {
                { "/K", K },
                { "/Columns", reader.Width },
                { "/Rows", reader.Height },
            };

            if (endOfBlock)
            {
                decodeParms["/EndOfBlock"] = "true";
            }

            if (encodedByteAlign)
            {
                decodeParms["/EncodedByteAlign"] = "true";
            }

            if (blackIs1)
            {
                decodeParms["/BlackIs1"] = "true";
            }

            encoder.K = K;
            encoder.EndOfLine = endOfLine;
            encoder.EncodedByteAlign = encodedByteAlign;

            foreach (var row in reader.ReadMonochromeRows())
            {
                if (blackIs1)
                {
                    for (var i = 0; i < row.Length; i++)
                    {
                        row[i] = !row[i];
                    }
                }

                encoder.WriteRow(row);

            }

            if (endOfBlock)
            {
                encoder.WriteEndOfBlock();

                // These lines should not be displayed
                foreach (var row in reader.ReadMonochromeRows())
                {
                    encoder.WriteRow(row);
                }

                // This value should be overridden
                decodeParms["/Rows"] = (reader.Height * 2).ToString();
            }

            var binaryFax = encoder.ToArray();

            var pdf = PdfEncoder.CreateTestFile(binaryFax, reader.Width, reader.Height,
                new Dictionary<string, object>(),
                decodeParms);

            File.WriteAllText(outputPath, pdf, Encoding.ASCII);
        }
    }
}
