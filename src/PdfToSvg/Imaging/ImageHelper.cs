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
    internal static class ImageHelper
    {
        public static int GetBitsPerComponent(PdfDictionary imageDictionary)
        {
            var isStencilMask = imageDictionary.GetValueOrDefault(Names.ImageMask, false);

            if (isStencilMask)
            {
                // ISO 32000-2:2020 section 8.9.6.2
                // For stencil masks, BitsPerComponent is always 1
                return 1;
            }
            else
            {
                return imageDictionary.GetValueOrDefault(Names.BitsPerComponent, 8);
            }
        }

        public static bool HasCustomDecodeArray(PdfDictionary imageDictionary, ColorSpace colorSpace)
        {
            if (imageDictionary.TryGetArray<double>(Names.Decode, out var decodeValues))
            {
                var bitsPerComponent = GetBitsPerComponent(imageDictionary);

                var decodeArray = new DecodeArray(bitsPerComponent, decodeValues);
                var defaultDecodeArray = colorSpace.GetDefaultDecodeArray(bitsPerComponent);

                return !decodeArray.Equals(defaultDecodeArray);
            }

            return false;
        }

        public static DecodeArray GetDecodeArray(PdfDictionary imageDictionary, ColorSpace colorSpace)
        {
            DecodeArray result;

            var bitsPerComponent = GetBitsPerComponent(imageDictionary);

            if (imageDictionary.TryGetArray<double>(Names.Decode, out var decodeValues))
            {
                result = new DecodeArray(bitsPerComponent, decodeValues);
            }
            else
            {
                result = colorSpace.GetDefaultDecodeArray(bitsPerComponent);
            }

            return result;
        }

        private static Stream GetImageStream(PdfStream imageDictionaryStream, CancellationToken cancellationToken)
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

        public static byte[] GetContent(PdfStream imageDictionaryStream, CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();

            using (var jpegStream = GetImageStream(imageDictionaryStream, cancellationToken))
            {
                jpegStream.CopyTo(memoryStream, cancellationToken);
            }

            return memoryStream.ToArray();
        }

#if HAVE_ASYNC
        public static async Task<byte[]> GetContentAsync(PdfStream imageDictionaryStream, CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();

            using (var jpegStream = GetImageStream(imageDictionaryStream, cancellationToken))
            {
                await jpegStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            }

            return memoryStream.ToArray();
        }
#endif
    }
}
