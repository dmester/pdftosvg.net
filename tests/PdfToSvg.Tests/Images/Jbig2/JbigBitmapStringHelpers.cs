// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jbig2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jbig2
{
    internal static class JbigBitmapStringHelpers
    {
        public static string NormalizeBitmapString(string str)
        {
            return string.Join("\n", str
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0));
        }

        public static JbigBitmap ParseBitmapString(string str)
        {
            var lines = str
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();

            var pixels = lines
                .SelectMany(line => line
                    .ToCharArray()
                    .Select(pixel => pixel == '◼'))
                .ToList();

            var bitmap = new JbigBitmap(lines[0].Length, lines.Count);
            pixels.CopyTo(bitmap.GetBuffer(), 0);
            return bitmap;
        }
    }
}
