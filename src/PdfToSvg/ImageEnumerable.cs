// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg
{
    /// <summary>
    /// Provides access to images embedded in a PDF document.
    /// </summary>
    /// <inheritdoc cref="GetEnumerator()" path="example"/>
    public class ImageEnumerable : IEnumerable<Image>
#if HAVE_ASYNC_ENUMERABLE
        , IAsyncEnumerable<Image>
#endif
    {
        private readonly IEnumerable<PdfDictionary> pageDicts;
        private readonly Action permissionAssert;

        internal ImageEnumerable(IEnumerable<PdfDictionary> pageDicts, Action permissionAssert)
        {
            this.pageDicts = pageDicts;
            this.permissionAssert = permissionAssert;
        }

#if HAVE_ASYNC
        /// <summary>
        /// Returns embedded images as a list asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <example>
        /// <para>
        ///     The following example exports all images embedded in a PDF document asynchronously.
        /// </para>
        /// <code lang="cs">
        /// using (var document = await PdfDocument.OpenAsync("input.pdf"))
        /// {
        ///     var imageNo = 1;
        /// 
        ///     foreach (var image in await document.Images.ToListAsync())
        ///     {
        ///         var content = await image.GetContentAsync();
        ///         var fileName = $"image{imageNo++}{image.Extension}";
        ///         File.WriteAllBytes(fileName, content);
        ///     }
        /// }
        /// </code>
        /// </example>
        public Task<List<Image>> ToListAsync(CancellationToken cancellationToken = default)
        {
            var enumerator = new ImageEnumerator(permissionAssert, cancellationToken);
            return enumerator.ToListAsync(pageDicts);
        }
#endif

#if HAVE_ASYNC_ENUMERABLE
        /// <summary>
        /// Gets an enumerator that iterates over embedded images asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <example>
        /// <para>
        ///     The following example exports all images embedded in a PDF document asynchronously.
        /// </para>
        /// <code lang="cs">
        /// using (var document = await PdfDocument.OpenAsync("input.pdf"))
        /// {
        ///     var imageNo = 1;
        /// 
        ///     await foreach (var image in document.Images)
        ///     {
        ///         var content = await image.GetContentAsync();
        ///         var fileName = $"image{imageNo++}{image.Extension}";
        ///         File.WriteAllBytes(fileName, content);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// <note>
        ///     This method is not available when using PdfToSvg.NET on .NET Framework.
        ///     <see cref="ToListAsync(CancellationToken)"/> is available as an alternative for consumers targeting
        ///     .NET Framework.
        /// </note>
        /// </remarks>
        public async IAsyncEnumerator<Image> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var enumerator = new ImageEnumerator(permissionAssert, cancellationToken);
            var returnedImages = new HashSet<Image>();

            foreach (var pageDict in pageDicts)
            {
                await foreach (var image in enumerator.VisitPageAsync(pageDict).ConfigureAwait(false))
                {
                    if (returnedImages.Add(image))
                    {
                        yield return image;
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Gets an enumerator that iterates over embedded images.
        /// </summary>
        /// <example>
        /// <para>
        ///     The following example exports images from all pages in the PDF document to image files.
        /// </para>
        /// <code lang="cs">
        /// using (var document = PdfDocument.Open("input.pdf"))
        /// {
        ///     var imageNo = 1;
        /// 
        ///     foreach (var image in document.Images)
        ///     {
        ///         var content = image.GetContent();
        ///         var fileName = $"image{imageNo++}{image.Extension}";
        ///         File.WriteAllBytes(fileName, content);
        ///     }
        /// }
        /// </code>
        /// </example>
        public IEnumerator<Image> GetEnumerator()
        {
            var enumerator = new ImageEnumerator(permissionAssert, CancellationToken.None);
            var returnedImages = new HashSet<Image>();

            foreach (var pageDict in pageDicts)
            {
                foreach (var image in enumerator.VisitPage(pageDict))
                {
                    if (returnedImages.Add(image))
                    {
                        yield return image;
                    }
                }
            }
        }

        /// <summary>
        /// Gets an enumerator that iterates over embedded images.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
