// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg
{
    /// <summary>
    /// Filters or rewrites a hyperlink target extracted from a PDF.
    /// </summary>
    /// <param name="originalUrl">The original hyperlink target from the PDF.</param>
    /// <returns>The hyperlink target to include in the SVG, or <see langword="null"/> to omit the link.</returns>
    public delegate string? LinkFilter(string originalUrl);
}
