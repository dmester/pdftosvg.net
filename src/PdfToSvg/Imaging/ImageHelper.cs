// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.Filters;
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
            const int DefaultBitsPerComponent = 8;

            var isStencilMask = imageDictionary.GetValueOrDefault(Names.ImageMask, false);
            if (isStencilMask)
            {
                // ISO 32000-2:2020 section 8.9.6.2
                // For stencil masks, BitsPerComponent is always 1
                return 1;
            }

            if (IsJpxImage(imageDictionary))
            {
                // ISO 32000-2:2020 table 87
                // BitsPerComponent shall be ignored for JPXDecode images
                return DefaultBitsPerComponent;
            }

            return imageDictionary.GetValueOrDefault(Names.BitsPerComponent, DefaultBitsPerComponent);
        }

        public static bool HasCustomDecodeArray(PdfDictionary imageDictionary, ColorSpace colorSpace)
        {
            if (imageDictionary.ContainsKey(Names.Decode))
            {
                var bitsPerComponent = GetBitsPerComponent(imageDictionary);
                var decodeArray = GetDecodeArray(imageDictionary, colorSpace);
                var defaultDecodeArray = colorSpace.GetDefaultDecodeArray(bitsPerComponent);

                return !decodeArray.Equals(defaultDecodeArray);
            }

            return false;
        }

        public static DecodeArray GetDecodeArray(PdfDictionary imageDictionary, ColorSpace colorSpace)
        {
            DecodeArray result;

            var bitsPerComponent = GetBitsPerComponent(imageDictionary);

            if (imageDictionary.TryGetArray<double>(Names.Decode, out var decodeValues) &&

                // ISO 32000-2:2020 table 87:
                // Decode array should be ignored for JPX images if the color space is absent and its not a image mask
                !(
                    IsJpxImage(imageDictionary) &&
                    !imageDictionary.ContainsKey(Names.ColorSpace) &&
                    !imageDictionary.GetValueOrDefault(Names.ImageMask, false)
                ))
            {
                var expectedLength = colorSpace.ComponentsPerSample * 2;

                if (decodeValues.Length == expectedLength)
                {
                    result = new DecodeArray(bitsPerComponent, decodeValues);
                }
                else if (decodeValues.Length < expectedLength)
                {
                    // Pad with default ranges
                    var defaultDecodeArray = colorSpace.GetDefaultDecodeArray(bitsPerComponent);
                    var newDecodeValues = new float[expectedLength];

                    var i = 0;

                    for (; i < decodeValues.Length; i++)
                    {
                        newDecodeValues[i] = (float)decodeValues[i];
                    }

                    i &= ~1; // Skip partial range

                    for (; i + 1 < newDecodeValues.Length; i += 2)
                    {
                        var range = defaultDecodeArray[i / 2];

                        newDecodeValues[i + 0] = range.Dmin;
                        newDecodeValues[i + 1] = range.Dmax;
                    }

                    result = new DecodeArray(bitsPerComponent, newDecodeValues);
                }
                else
                {
                    var newDecodeValues = new double[expectedLength];

                    Array.Copy(decodeValues, newDecodeValues, newDecodeValues.Length);

                    result = new DecodeArray(bitsPerComponent, newDecodeValues);
                }
            }
            else
            {
                result = colorSpace.GetDefaultDecodeArray(bitsPerComponent);
            }

            return result;
        }

        private static bool IsJpxImage(PdfDictionary imageDictionary)
        {
            return imageDictionary.Stream?.Filters.LastOrDefault()?.Filter == Filter.JpxDecode;
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

            using (var imageStream = GetImageStream(imageDictionaryStream, cancellationToken))
            {
                imageStream.CopyTo(memoryStream, cancellationToken);
            }

            return memoryStream.ToArray();
        }

#if HAVE_ASYNC
        public static async Task<byte[]> GetContentAsync(PdfStream imageDictionaryStream, CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();

            using (var imageStream = GetImageStream(imageDictionaryStream, cancellationToken))
            {
                await imageStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            }

            return memoryStream.ToArray();
        }
#endif
    }
}
