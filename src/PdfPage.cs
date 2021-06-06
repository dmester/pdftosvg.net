// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg
{
    /// <summary>
    /// Represents a single page in a PDF document.
    /// </summary>
    public class PdfPage
    {
        private readonly PdfDocument owner;
        private readonly PdfDictionary page;

        internal PdfPage(PdfDocument owner, PdfDictionary page)
        {
            this.owner = owner;
            this.page = page;
        }

        /// <summary>
        /// Gets the owner <see cref="PdfDocument"/> that this page is part of.
        /// </summary>
        public PdfDocument Document => owner;

        /// <summary>
        /// Converts this page to an SVG fragment.
        /// </summary>
        /// <returns>SVG fragment without XML declaration. The fragment can be saved to a file or included as inline SVG in HTML.</returns>
        /// <remarks>
        /// Note that if you parse the returned SVG fragment as XML, you need to preserve space and not add indentation. Text content
        /// will otherwise not render correctly.
        /// </remarks>
        public string ToSvg()
        {
            return ToSvg(new SvgConversionOptions());
        }

        /// <summary>
        /// Converts this page to an SVG fragment.
        /// </summary>
        /// <param name="options">Additional configuration options for the conversion.</param>
        /// <returns>SVG fragment without XML declaration. The fragment can be saved to a file or included as inline SVG in HTML.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Note that if you parse the returned SVG fragment as XML, you need to preserve space and not add indentation. Text content
        /// will otherwise not render correctly.
        /// </remarks>
        public string ToSvg(SvgConversionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return ToString(SvgRenderer.Convert(page, options));
        }

        /// <summary>
        /// Converts this page to an SVG fragment asynchronously.
        /// </summary>
        /// <returns>SVG fragment without XML declaration. The fragment can be saved to a file or included as inline SVG in HTML.</returns>
        /// <remarks>
        /// Note that if you parse the returned SVG fragment as XML, you need to preserve space and not add indentation. Text content
        /// will otherwise not render correctly.
        /// </remarks>
        public Task<string> ToSvgAsync()
        {
            return ToSvgAsync(new SvgConversionOptions());
        }

        /// <summary>
        /// Converts this page to an SVG fragment asynchronously.
        /// </summary>
        /// <param name="options">Additional configuration options for the conversion.</param>
        /// <returns>SVG fragment without XML declaration. The fragment can be saved to a file or included as inline SVG in HTML.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Note that if you parse the returned SVG fragment as XML, you need to preserve space and not add indentation. Text content
        /// will otherwise not render correctly.
        /// </remarks>
        public async Task<string> ToSvgAsync(SvgConversionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return ToString(await SvgRenderer.ConvertAsync(page, options));
        }

        private string ToString(XElement el)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var writer = new SvgXmlWriter(stringWriter))
                {
                    el.WriteTo(writer);
                }

                return stringWriter.ToString();
            }
        }
    }
}
