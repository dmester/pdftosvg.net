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
    /// <code lang="cs" title="Using local fonts instead of embedding fonts">
    /// var conversionOptions = new SvgConversionOptions
    /// {
    ///     FontResolver = FontResolver.LocalFonts,
    /// };
    /// 
    /// using (var doc = PdfDocument.Open("input.pdf"))
    /// {
    ///     var pageIndex = 0;
    ///
    ///     foreach (var page in doc.Pages)
    ///     {
    ///         page.SaveAsSvg($"output-{pageIndex++}.svg", conversionOptions);
    ///     }
    /// }
    /// </code>
    /// <para>
    ///     Custom behavior can be achieved by subclassing <see cref="FontResolver"/> and implementing the
    ///     <see cref="ResolveFont(SourceFont, CancellationToken)"/> method. Here is a custom implementation using a
    ///     locally installed Open Sans font.
    /// </para>
    /// <code lang="cs" title="Custom font resolver">
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
    /// <para>
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
    /// </list>
    /// </example>
    public abstract class FontResolver
    {
        /// <summary>
        /// Font resolver substituting fonts in the PDF with commonly available fonts. No fonts are embedded in the
        /// resulting SVG. The resolved fonts need to be available on the viewing machine.
        /// </summary>
        public static FontResolver LocalFonts { get; } = new LocalFontResolver();

        /// <summary>
        /// Font resolver converting fonts in the PDF to WOFF format and embedding them in the output SVG.
        /// If the font cannot be converted, this resolver falls back to the <see cref="LocalFonts"/> resolver.
        /// </summary>
        public static FontResolver EmbedWoff { get; } = new EmbedWoffFontResolver();

        /// <summary>
        /// Font resolver converting fonts in the PDF to OpenType (.otf) format and embedding them in the output SVG.
        /// If the font cannot be converted, this resolver falls back to the <see cref="LocalFonts"/> resolver.
        /// </summary>
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
