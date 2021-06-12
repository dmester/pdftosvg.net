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

        /// <summary>
        /// Saves the page as an SVG file.
        /// </summary>
        /// <param name="stream">Stream to write the SVG content to.</param>
        public void SaveAsSvg(Stream stream) => SaveAsSvg(stream, new SvgConversionOptions());

        /// <summary>
        /// Saves the page as an SVG file.
        /// </summary>
        /// <param name="path">Path to SVG file. If the file already exists, it will be overwritten.</param>
        public void SaveAsSvg(string path) => SaveAsSvg(path, new SvgConversionOptions());

        /// <summary>
        /// Saves the page as an SVG file.
        /// </summary>
        /// <param name="stream">Stream to write the SVG content to.</param>
        /// <param name="options">Additional configuration options for the conversion.</param>
        public void SaveAsSvg(Stream stream, SvgConversionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var content = SvgRenderer.Convert(page, options);
            var document = new XDocument(content);
            
            using (var writer = new SvgXmlWriter(stream, Encoding.UTF8))
            {
                document.WriteTo(writer);
            }
        }

        /// <summary>
        /// Saves the page as an SVG file.
        /// </summary>
        /// <param name="path">Path to SVG file. If the file already exists, it will be overwritten.</param>
        /// <param name="options">Additional configuration options for the conversion.</param>
        public void SaveAsSvg(string path, SvgConversionOptions options)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                SaveAsSvg(stream, options);
            }
        }

        /// <summary>
        /// Saves the page as an SVG file asynchronously.
        /// </summary>
        /// <param name="stream">Stream to write the SVG content to.</param>
        public Task SaveAsSvgAsync(Stream stream) => SaveAsSvgAsync(stream, new SvgConversionOptions());

        /// <summary>
        /// Saves the page as an SVG file asynchronously.
        /// </summary>
        /// <param name="path">Path to SVG file. If the file already exists, it will be overwritten.</param>
        public Task SaveAsSvgAsync(string path) => SaveAsSvgAsync(path, new SvgConversionOptions());

        /// <summary>
        /// Saves the page as an SVG file asynchronously.
        /// </summary>
        /// <param name="stream">Stream to write the SVG content to.</param>
        /// <param name="options">Additional configuration options for the conversion.</param>
        public async Task SaveAsSvgAsync(Stream stream, SvgConversionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var content = await SvgRenderer.ConvertAsync(page, options);
            var document = new XDocument(content);

            // XmlTextWriter does not support async, so buffer the file before writing it to the output stream.
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new SvgXmlWriter(memoryStream, Encoding.UTF8))
                {
                    document.WriteTo(writer);
                    writer.Flush();

                    var buffer = memoryStream.GetBuffer();
                    await stream.WriteAsync(buffer, 0, (int)memoryStream.Length);
                }
            }
        }

        /// <summary>
        /// Saves the page as an SVG file asynchronously.
        /// </summary>
        /// <param name="path">Path to SVG file. If the file already exists, it will be overwritten.</param>
        /// <param name="options">Additional configuration options for the conversion.</param>
        public async Task SaveAsSvgAsync(string path, SvgConversionOptions options)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await SaveAsSvgAsync(stream, options);
            }
        }

        private string ToString(XNode el)
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
