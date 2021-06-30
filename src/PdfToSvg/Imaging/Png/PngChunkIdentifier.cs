// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace PdfToSvg.Imaging.Png
{
    internal static class PngChunkIdentifier
    {
        public const string ImageHeader = "IHDR";
        public const string ImageData = "IDAT";
        public const string ImageEnd = "IEND";
        public const string Palette = "PLTE";
        public const string ImageGamma = "gAMA";
        public const string TextualData = "tEXt";
        public const string Transparency = "tRNS";
    }
}
