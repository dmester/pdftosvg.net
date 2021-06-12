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
        static async Task Main()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var input = @"M:\Repos\pdftosvg.net\tests\Test-files\rotated-page.pdf";

            using (var doc = await PdfDocument.OpenAsync(input))
            {
                var pageIndex = 0;

                foreach (var page in doc.Pages)
                {
                    var svgFileName = Path.GetFileNameWithoutExtension(input) + "-" + pageIndex++ + ".svg";
                    await page.SaveAsSvgAsync("R:\\" + svgFileName);
                }
            }

            Console.WriteLine("Done! {0}ms", sw.ElapsedMilliseconds);
        }
    }
}
