// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Fonts.FontResolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg
{
    /// <summary>
    /// Resolves which font to be used for text in the SVG, for a given PDF font name.
    /// </summary>
    /// <example>
    /// <para>
    ///     The following example will use <see cref="FontResolver.LocalFonts"/> to convert a PDF to SVG without
    ///     embedding fonts into the extracted SVG. Instead local fonts assumed to be installed on the client machine
    ///     are used.
    /// </para>
    /// <code language="cs" title="Using local fonts instead of embedding fonts">
    /// var conversionOptions = new SvgConversionOptions
    /// {
    ///     FontResolver = FontResolver.LocalFonts,
    /// };
    /// 
    /// using (var doc = PdfDocument.Open("input.pdf"))
    /// {
    ///     var pageNo = 1;
    ///
    ///     foreach (var page in doc.Pages)
    ///     {
    ///         page.SaveAsSvg($"output-{pageNo++}.svg", conversionOptions);
    ///     }
    /// }
    /// </code>
    /// <para>
    ///     Custom behavior can be achieved by subclassing <see cref="FontResolver"/> and implementing the
    ///     <see cref="ResolveFont(SourceFont, CancellationToken)"/> method. Here is a custom implementation using a
    ///     locally installed Open Sans font.
    /// </para>
    /// <code language="cs" title="Custom font resolver">
    /// class OpenSansFontResolver : FontResolver
    /// {
    ///     public override Font ResolveFont(SourceFont sourceFont, CancellationToken cancellationToken)
    ///     {
    ///         var font = FontResolver.LocalFonts.ResolveFont(sourceFont, cancellationToken);
    /// 
    ///         if (sourceFont.Name != null &amp;&amp;
    ///             sourceFont.Name.Contains("OpenSans", StringComparison.InvariantCultureIgnoreCase) &amp;&amp;
    ///             font is LocalFont localFont)
    ///         {
    ///             font = new LocalFont("'Open Sans',sans-serif", localFont.FontWeight, localFont.FontStyle);
    ///         }
    ///
    ///         return font;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <h2>Font types</h2>
    /// <para>
    ///     The font resolver will for each font from the PDF provide a substitute font to be used in the generated SVG.
    ///     Types of substitute fonts that can be returned:
    /// </para>
    /// <list type="table">
    ///     <listheader>
    ///         <term>Font type</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="LocalFont"/></term>
    ///         <description>A font that is assumed to be installed on the machine viewing the SVG.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="WebFont"/></term>
    ///         <description>
    ///             Use a provided TrueType, OpenType, WOFF or WOFF2 font. Note that external resources are not allowed
    ///             in standalone SVG files when displayed in browsers, so if you intend to use external SVG files, you
    ///             need to return a <see cref="WebFont"/> instance using
    ///             <see href="https://en.wikipedia.org/wiki/Data_URI_scheme">data URLs</see> only.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="InlinedFont"/></term>
    ///         <description>
    ///             Inlines the glyphs from the font as paths and other elements within the SVG. This differs from
    ///             <see cref="WebFont"/>, which are embedded as font files in the SVG. The inlined glyphs cannot be
    ///             selected as text in the SVG. Currently only Type 3 fonts can be inlined, but more font types might
    ///             be supported in the future.
    ///         </description>
    ///     </item>
    /// </list>
    /// <h2>Text representation</h2>
    /// <para>
    ///     PDF documents store text encoded as character codes, and provide mappings from character codes to font
    ///     glyphs and Unicode characters. Some documents map multiple character codes to the same Unicode
    ///     character, giving PdfToSvg.NET a choice. Exporting both character codes as the same Unicode character
    ///     results in good text representation but potentially visually inaccurate SVG’s. Remapping one of
    ///     the character codes to another Unicode character ensures visually accurate SVG’s at the cost of inaccurate
    ///     text representation if text is exported from the SVG.
    /// </para>
    /// <para>
    ///     When exporting text using a <see cref="LocalFont"/>, the library will use the Unicode mapping specified by
    ///     the document, providing more accurate text representation.
    /// </para>
    /// <para>
    ///     When exporting text using a <see cref="WebFont"/>, the library will remap duplicate character codes to 
    ///     characters in the 
    ///     <see href="https://en.wikipedia.org/wiki/Private_Use_Areas">Private Use Areas</see>, making sure the
    ///     exported SVG’s are visually accurate, but text might appear as a series of question marks, <c>������</c>,
    ///     in the SVG markup. If you intend to extract text from the SVG, consider exporting the SVG using 
    ///     <see cref="FontResolver.LocalFonts">local fonts</see> instead.
    /// </para>
    /// </remarks>
    /// <seealso cref="SvgConversionOptions.FontResolver"/>
    public abstract class FontResolver
    {
        /// <summary>
        /// Font resolver substituting fonts in the PDF with commonly available fonts. No fonts are embedded or inlined
        /// in the resulting SVG. The resolved fonts need to be available on the viewing machine.
        /// </summary>
        /// <example>
        /// <para>
        ///     The following example will produce SVG using locally installed fonts on the user machine. All fonts in
        ///     the PDF are remapped to commonly installed fonts.
        /// </para>
        /// <code language="cs" title="Using local fonts">
        /// var conversionOptions = new SvgConversionOptions
        /// {
        ///     FontResolver = FontResolver.LocalFonts,
        /// };
        /// 
        /// using (var doc = PdfDocument.Open("input.pdf"))
        /// {
        ///     var pageNo = 1;
        ///
        ///     foreach (var page in doc.Pages)
        ///     {
        ///         page.SaveAsSvg($"output-{pageNo++}.svg", conversionOptions);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// <para>
        ///     Note that only using locally installed fonts will produce an SVG with a look deviating from the
        ///     original PDF document.
        /// </para>
        /// <h2>Text representation</h2>
        /// <para>
        ///     PDF documents store text encoded as character codes, and provide mappings from character codes to font
        ///     glyphs and Unicode characters. Some documents map multiple character codes to the same Unicode
        ///     character, giving PdfToSvg.NET a choice. Exporting both character codes as the same Unicode character
        ///     results in good text representation but potentially visually inaccurate SVG’s. Remapping one of
        ///     the character codes to another Unicode character ensures visually accurate SVG’s at the cost of inaccurate
        ///     text representation if text is exported from the SVG.
        /// </para>
        /// <para>
        ///     When exporting text using a <see cref="LocalFont"/>, the library will use the Unicode mapping specified by
        ///     the document, providing more accurate text representation.
        /// </para>
        /// </remarks>
        public static FontResolver LocalFonts { get; } = new LocalFontResolver();

        /// <summary>
        /// Font resolver converting fonts in the PDF to WOFF format and embedding them in the output SVG.
        /// If the font cannot be converted, the resolver in first hand tries to inline the glyphs. If this is not
        /// possible, the resolver falls back to the <see cref="LocalFonts"/> resolver.
        /// </summary>
        /// <example>
        /// <para>
        ///     The following example will embed fonts in the exported SVG as WOFF fonts. Note that this is 
        ///     currently the default behavior, so it is not necessary specifying a font resolver.
        /// </para>
        /// <code language="cs" title="Export fonts as WOFF fonts">
        /// var conversionOptions = new SvgConversionOptions
        /// {
        ///     FontResolver = FontResolver.EmbedWoff,
        /// };
        /// 
        /// using (var doc = PdfDocument.Open("input.pdf"))
        /// {
        ///     var pageNo = 1;
        ///
        ///     foreach (var page in doc.Pages)
        ///     {
        ///         page.SaveAsSvg($"output-{pageNo++}.svg", conversionOptions);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// <para>
        ///     PDF documents store text encoded as character codes, and provide mappings from character codes to font
        ///     glyphs and Unicode characters. Some documents map multiple character codes to the same Unicode
        ///     character, giving PdfToSvg.NET a choice. Exporting both character codes as the same Unicode character
        ///     results in good text representation but potentially visually inaccurate SVG’s. Remapping one of
        ///     the character codes to another Unicode character ensures visually accurate SVG’s at the cost of inaccurate
        ///     text representation if text is exported from the SVG.
        /// </para>
        /// <para>
        ///     When exporting text using a WOFF font, the library will remap duplicate character codes to characters in the 
        ///     <see href="https://en.wikipedia.org/wiki/Private_Use_Areas">Private Use Areas</see>, making sure the
        ///     exported SVG’s are visually accurate, but text might appear as a series of question marks, <c>������</c>,
        ///     in the SVG markup. If you intend to extract text from the SVG, consider exporting the SVG using 
        ///     <see cref="FontResolver.LocalFonts">FontResolver.LocalFonts</see> instead.
        /// </para>
        /// </remarks>
        public static FontResolver EmbedWoff { get; } = new EmbedWoffFontResolver();

        /// <summary>
        /// Font resolver converting fonts in the PDF to OpenType (.otf) format and embedding them in the output SVG.
        /// If the font cannot be converted, the resolver in first hand tries to inline the glyphs. If this is not
        /// possible, the resolver falls back to the <see cref="LocalFonts"/> resolver.
        /// </summary>
        /// <example>
        /// <para>
        ///     The following example will embed fonts in the exported SVG as OpenType fonts.
        /// </para>
        /// <code language="cs" title="Export fonts as OpenType fonts">
        /// var conversionOptions = new SvgConversionOptions
        /// {
        ///     FontResolver = FontResolver.EmbedOpenType,
        /// };
        /// 
        /// using (var doc = PdfDocument.Open("input.pdf"))
        /// {
        ///     var pageNo = 1;
        ///
        ///     foreach (var page in doc.Pages)
        ///     {
        ///         page.SaveAsSvg($"output-{pageNo++}.svg", conversionOptions);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// <para>
        ///     PDF documents store text encoded as character codes, and provide mappings from character codes to font
        ///     glyphs and Unicode characters. Some documents map multiple character codes to the same Unicode
        ///     character, giving PdfToSvg.NET a choice. Exporting both character codes as the same Unicode character
        ///     results in good text representation but potentially visually inaccurate SVG’s. Remapping one of
        ///     the character codes to another Unicode character ensures visually accurate SVG’s at the cost of inaccurate
        ///     text representation if text is exported from the SVG.
        /// </para>
        /// <para>
        ///     When exporting text using an OpenType font, the library will remap duplicate character codes to 
        ///     characters in the 
        ///     <see href="https://en.wikipedia.org/wiki/Private_Use_Areas">Private Use Areas</see>, making sure the
        ///     exported SVG’s are visually accurate, but text might appear as a series of question marks, <c>������</c>,
        ///     in the SVG markup. If you intend to extract text from the SVG, consider exporting the SVG using 
        ///     <see cref="FontResolver.LocalFonts">FontResolver.LocalFonts</see> instead.
        /// </para>
        /// </remarks>
        public static FontResolver EmbedOpenType { get; } = new EmbedOpenTypeFontResolver();

        /// <summary>
        /// Gets the default font resolver used when no resolver is explicitly specified.
        /// Currently <see cref="EmbedWoff"/> is the default font resolver, but this can change in the future.
        /// </summary>
        public static FontResolver Default { get; } = EmbedWoff;

        /// <summary>
        /// Resolves which font to be used for text in the SVG, for a given source PDF font.
        /// </summary>
        /// <param name="sourceFont">Provides information about the source PDF font.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <returns>
        /// The font to be used in the resulting SVG markup.
        /// Can be a <see cref="LocalFont"/> or <see cref="WebFont"/>.
        /// </returns>
        /// <exception cref="OperationCanceledException">The operation was cancelled because the cancellation token was triggered.</exception>
        public virtual Font ResolveFont(SourceFont sourceFont, CancellationToken cancellationToken)
        {
            throw new NotImplementedException(
                $"This {nameof(FontResolver)} does not implement any of the {nameof(ResolveFont)} methods.");
        }

        /// <inheritdoc cref="ResolveFont(SourceFont, CancellationToken)"/>
        /// <summary>
        /// Resolves asynchronously which font to be used for text in the SVG, for a given source PDF font.
        /// </summary>
        public virtual Task<Font> ResolveFontAsync(SourceFont sourceFont, CancellationToken cancellationToken)
        {
#if NET40
            var tcs = new TaskCompletionSource<Font>();
            tcs.SetResult(ResolveFont(sourceFont, cancellationToken));
            return tcs.Task;
#else
            return Task.FromResult(ResolveFont(sourceFont, cancellationToken));
#endif
        }
    }
}
