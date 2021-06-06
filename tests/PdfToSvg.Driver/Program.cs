// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;

namespace PdfToSvg.Driver
{
    class Program
    {
        static void Main()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var input = @"C:\Users\Daniel\Downloads\GWG181_16Bit_CMYK_X4.pdf";

            using (var doc = PdfDocument.Open(input))
            {
                var pageIndex = 0;

                foreach (var page in doc.Pages)
                {
                    var svg = page.ToSvg();
                    var svgFileName = Path.GetFileNameWithoutExtension(input) + "-" + pageIndex++ + ".svg";
                    File.WriteAllText("R:\\" + svgFileName, svg);
                }
            }

            Console.WriteLine("Done! {0}ms", sw.ElapsedMilliseconds);
        }
    }
}
