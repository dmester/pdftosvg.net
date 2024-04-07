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
    ///     The easiest way of getting started with PdfToSvg.NET is by installing the NuGet package.
    /// </para>
    /// <code language="none">
    /// PM&gt; Install-Package PdfToSvg.NET
    /// </code>
    /// <para>
    ///     Open a PDF document by calling 
    ///     <see cref="PdfDocument.Open(string, OpenOptions?, System.Threading.CancellationToken)">PdfDocument.Open()</see>.
    ///     Then use either
    ///     <see cref="PdfPage.SaveAsSvg(string, SvgConversionOptions?, System.Threading.CancellationToken)">SaveAsSvg()</see> or
    ///     <see cref="PdfPage.ToSvgString(SvgConversionOptions?, System.Threading.CancellationToken)">ToSvgString()</see>
    ///     to save each page as SVG.
    /// </para>
    /// <code language="cs">
    /// using (var doc = PdfDocument.Open("input.pdf"))
    /// {
    ///     var pageNo = 1;
    ///
    ///     foreach (var page in doc.Pages)
    ///     {
    ///         page.SaveAsSvg($"output-{pageNo++}.svg");
    ///     }
    /// }
    /// </code>
    /// <para>
    ///     By default, PdfToSvg.NET will try to extract fonts embedded in the PDF and embed them in the output SVG as
    ///     data URLs. This behavior can be changed by specifying another
    ///     <see cref="SvgConversionOptions.FontResolver"/>.
    /// </para>
    /// <note type="note">
    ///     If you parse the XML returned from PdfToSvg.NET, you must preserve space and not add indentation.
    ///     Otherwise text will not be rendered correctly in the modified markup.
    /// </note>
    /// </summary>
    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }
}
