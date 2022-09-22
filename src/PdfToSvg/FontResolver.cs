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
