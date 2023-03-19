// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal class JpegImage : Image
    {
        private readonly PdfStream imageDictionaryStream;

        public JpegImage(PdfDictionary imageDictionary) : base("image/jpeg")
        {
            if (imageDictionary.Stream == null)
            {
                throw new ArgumentException("There was no data stream attached to the image dictionary.", nameof(imageDictionary));
            }

            this.imageDictionaryStream = imageDictionary.Stream;
        }

        public static bool IsSupported(ColorSpace colorSpace)
        {
            return colorSpace is DeviceRgbColorSpace || colorSpace is DeviceGrayColorSpace;
        }

        private Stream GetStream(CancellationToken cancellationToken)
        {
            Stream? resultStream = null;

            var filters = imageDictionaryStream.Filters;
            var encodedStream = imageDictionaryStream.Open(cancellationToken);

            try
            {
                resultStream = filters.Take(filters.Count - 1).Decode(encodedStream);
            }
            finally
            {
                if (resultStream == null)
                {
                    encodedStream.Dispose();
                }
            }

            return resultStream;
        }

        public override byte[] GetContent(CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();

            using (var jpegStream = GetStream(cancellationToken))
            {
                jpegStream.CopyTo(memoryStream, cancellationToken);
            }

            return memoryStream.ToArray();
        }

#if HAVE_ASYNC
        public override async Task<byte[]> GetContentAsync(CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();

            using (var jpegStream = GetStream(cancellationToken))
            {
                await jpegStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            }

            return memoryStream.ToArray();
        }
#endif
    }
}
