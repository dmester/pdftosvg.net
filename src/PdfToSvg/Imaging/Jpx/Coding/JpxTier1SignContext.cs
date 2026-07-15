// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx.Coding
{
    internal readonly struct JpxTier1SignContext(int label, int xorBit)
    {
        /// <summary>
        /// "Context label" from Table D.3.
        /// </summary>
        public readonly byte Label = (byte)label;

        /// <summary>
        /// "XORbit" from Table D.3.
        /// </summary>
        public readonly byte XorBit = (byte)xorBit;
    }
}
