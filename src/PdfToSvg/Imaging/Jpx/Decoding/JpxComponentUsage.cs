// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx.Decoding
{
    internal enum JpxComponentUsage
    {
        /// <summary>
        /// Component not decoded at all.
        /// </summary>
        Skipped,
        /// <summary>
        /// Component decoded but not normalized.
        /// </summary>
        Raw,
        /// <summary>
        /// Component decoded and normalized to range [0, 1].
        /// </summary>
        Normalized,
    }
}
