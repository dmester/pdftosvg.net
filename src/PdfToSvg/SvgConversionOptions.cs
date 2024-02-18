// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    /// <summary>
    /// Contains options affecting the behavior when a PDF page is converted to SVG.
    /// </summary>
    public class SvgConversionOptions
    {
        private ImageResolver imageResolver = ImageResolver.Default;
        private FontResolver fontResolver = FontResolver.Default;

        /// <summary>
        /// Gets or sets a class that is used to resolve URLs for images.
        /// </summary>
        /// <remarks>
        /// The default implementation <see cref="ImageResolver.DataUrl"/> embeds all images as data URLs.
        /// You can implement a resolver yourself to e.g. save the images to files and instead include URLs to
        /// the separate image files in the SVG markup.
        /// </remarks>
        public ImageResolver ImageResolver
        {
            get => imageResolver;
            set => imageResolver = value ?? ImageResolver.Default;
        }

        /// <summary>
        /// Gets or sets an implementation that will be used for deciding what font to be used for text included in the
        /// SVG.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     The default implementation will in first hand try to embed fonts as WOFF files, and if not possible,
        ///     fallback to detecting standard fonts and assuming the client has those installed. You can implement a
        ///     custom font resolver for e.g. using custom WOFF or WOFF2 files, or saving the embedded fonts as separate
        ///     font files.
        /// files.
        /// </para>
        /// <para>
        ///     Built-in font resolvers:
        /// </para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Resolver</term>
        ///         <description>Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term><see cref="FontResolver.EmbedOpenType"/></term>
        ///         <description>
        ///             Extracts fonts from the PDF, converts them to OpenType format, and then embed them as data URLs
        ///             in the SVG.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="FontResolver.EmbedWoff"/></term>
        ///         <description>
        ///             Extracts fonts from the PDF, converts them to WOFF format, and then embed them as data URLs in
        ///             the SVG.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="FontResolver.LocalFonts"/></term>
        ///         <description>
        ///             Tries to match the PDF fonts with commonly available fonts, and references them by name.
        ///         </description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <inheritdoc cref="PdfToSvg.FontResolver" path="example"/>
        public FontResolver FontResolver
        {
            get => fontResolver;
            set => fontResolver = value ?? FontResolver.Default;
        }

        /// <summary>
        /// Gets or sets the minimum stroke width that will be used in the resulting SVG.
        /// If the PDF use a thinner stroke width, it will be replaced with this width.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     The default value is 0.5.
        /// </para>
        /// <para>
        ///     The value is expressed in transformed user space units of the converted PDF page. By default 1 user
        ///     space unit is 1/72 inch (0.35 mm), but this can be overridden by the PDF document. Transforms can affect
        ///     the actual minimum width in the generated SVG.
        /// </para>
        /// </remarks>
        public double MinStrokeWidth { get; set; } = 0.5;

        /// <exclude />
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(
            "Use " + nameof(SvgConversionOptions) + "." + nameof(CollapseSpaceLocalFont) + " " +
            "and " + nameof(SvgConversionOptions) + "." + nameof(CollapseSpaceEmbeddedFont) + " instead.")]
        public double KerningThreshold
        {
            get => CollapseSpaceLocalFont;
            set
            {
                CollapseSpaceLocalFont = value;
                CollapseSpaceEmbeddedFont = value;
            }
        }

        /// <summary>
        /// Explicit spacing between letters below this threshold will be removed and merged to a single text span when
        /// using a local font.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     The default value is 0.2.
        /// </para>
        /// <para>
        ///     The value is relative to the current font size, where 1.0 represents the font size.
        ///     This property affects text using a local font. A high value produces long text spans. This is better for
        ///     local fonts, where the actual font metrics might not match the metrics of the original font.
        /// </para>
        /// </remarks>
        public double CollapseSpaceLocalFont { get; set; } = 0.2;

        /// <summary>
        /// Explicit spacing between letters below this threshold will be removed and merged to a single text span when
        /// using an embedded font.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     The default value is 0.02.
        /// </para>
        /// <para>
        ///     The value is relative to the current font size, where 1.0 represents the font size.
        ///     This property affects text formatted with an embedded font. Text inlined as paths is not affected. A low
        ///     value produces a more accurate result, while a high value produces a more compact SVG markup.
        /// </para>
        /// </remarks>
        public double CollapseSpaceEmbeddedFont { get; set; } = 0.02;

        /// <summary>
        /// Determines whether web links from the PDF document will be included in the generated SVG.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     The default value is <c>true</c>.
        /// </para>
        /// <para>
        ///     Note that this property only affects links to websites. Other types of links, including links within the
        ///     document, are currently not supported.
        /// </para>
        /// </remarks>
        public bool IncludeLinks { get; set; } = true;

        /// <summary>
        /// Determines whether annotations drawn in the PDF document should be included in the generated SVG.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     The default value is <c>true</c>.
        /// </para>
        /// <para>
        ///     Interactive annotations are not supported. Links are technically also annotations, but are configured
        ///     by the <see cref="IncludeLinks"/> property.
        /// </para>
        /// <para>
        ///     Annotations are inserted as <c>&lt;g&gt;</c> elements, decorated with metadata attributes
        ///     in the namespace <c>https://pdftosvg.net/xmlns/annotations</c>. Note that some annotations are only
        ///     visible either in print or on screen.
        /// </para>
        /// <code language="xml" title="Annotation in exported SVG">
        /// &lt;svg xmlns="http://www.w3.org/2000/svg"
        ///      xmlns:annot="https://pdftosvg.net/xmlns/annotations"&gt;
        ///   &lt;g annot:type="Text"
        ///      annot:created="2024-02-18T12:37:31+01:00"
        ///      annot:popup-title="John Smith"
        ///      annot:popup-contents="Typo, should be 'than', not 'then'."&gt;
        ///     &lt;path d="M100 100v2h2v-2z" fill="#000" /&gt;
        ///   &lt;/g&gt;
        /// &lt;/svg&gt;
        /// </code>
        /// <para>
        ///     PdfToSvg.NET may use the following attributes:
        /// </para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Attribute</term>
        ///         <description>Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term><c>annot:type</c></term>
        ///         <description>
        ///             Specifies the type of annotation. A list of possible types can be found in Table 169 in the
        ///             <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf">PDF 1.7 spec</see>.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><c>annot:popup-title</c></term>
        ///         <description>
        ///             Some annotations have information that will appear in a popup when the document is opened in a
        ///             PDF viewer. This attribute contains the text that would appear as title of this popup.
        ///             The author of the annotation is usually specified as title.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><c>annot:popup-contents</c></term>
        ///         <description>
        ///             Text comment that would appear as content of the popup.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><c>annot:created</c></term>
        ///         <description>
        ///             Time when the annotation was added to the PDF document.
        ///         </description>
        ///     </item>
        /// </list>
        /// </remarks>
        public bool IncludeAnnotations { get; set; } = true;

        /// <summary>
        /// Determines whether hidden text from the PDF document will be included in the generated SVG.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     The default value is <c>true</c>.
        /// </para>
        /// <para>
        ///     Hidden text is used for multiple purposes in PDF documents. Some examples:
        /// </para>
        /// <list type="bullet">
        ///     <item>
        ///         For making it possible to search for and select text that is otherwise presented as an image. This
        ///         is common in scanned documents processed with OCR (optical character recognition).
        ///     </item>
        ///     <item>
        ///         For embedding hidden metadata about the file, e.g., the name of the software producing the document.
        ///     </item>
        ///     <item>
        ///         For clipping other graphics to the text outline.
        ///     </item>
        /// </list>
        /// <para>
        ///     By setting this property to <c>false</c>, PdfToSvg.NET will generate smaller SVG files for some PDF
        ///     documents, but it will have no or minimal impact on most documents.
        /// </para>
        /// <para>
        ///     Setting the property to <c>false</c> does not remove text used for clipping, since it is required for
        ///     preserving the appearance of the page. It does however remove duplicated <c>&lt;text&gt;</c> nodes
        ///     otherwise generated for making the clipping text searchable and selectable.
        /// </para>
        /// </remarks>
        public bool IncludeHiddenText { get; set; } = true;

#if DEBUG
        /// <summary>
        /// If <c>true</c>, the content stream operators are logged in the output SVG.
        /// </summary>
        public bool DebugLogOperators { get; set; }
#endif
    }
}
