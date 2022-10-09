// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg
{
    /// <summary>
    /// <para>
    ///     To convert a PDF file to SVG, start by creating a <see cref="PdfDocument"/>. Then use either
    ///     <see cref="PdfPage.SaveAsSvg(string, SvgConversionOptions?, System.Threading.CancellationToken)"/> or
    ///     <see cref="PdfPage.ToSvgString(SvgConversionOptions?, System.Threading.CancellationToken)"/>
    ///     to save each page as SVG.
    /// </para>
    /// <code lang="cs" title="Convert PDF to SVG">
    /// using (var doc = PdfDocument.Open("input.pdf"))
    /// {
    ///     var pageIndex = 0;
    ///
    ///     foreach (var page in doc.Pages)
    ///     {
    ///         page.SaveAsSvg($"output-{pageIndex++}.svg");
    ///     }
    /// }
    /// </code>
    /// <para>
    ///     By default, PdfToSvg.NET will try to extract fonts embedded in the PDF and embed them in the output SVG as
    ///     data URIs. This behavior can be changed by specifying another
    ///     <see cref="SvgConversionOptions.FontResolver"/>.
    /// </para>
    /// </summary>
    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }
}
