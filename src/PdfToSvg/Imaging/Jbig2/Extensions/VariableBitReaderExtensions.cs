// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Extensions
{
    internal static class VariableBitReaderExtensions
    {
        public static void SkipReservedBits(this VariableBitReader reader, int bitCount)
        {
            reader.SkipBits(bitCount);
        }
    }
}
