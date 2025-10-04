// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal class JpegImage : Image
    {
        private const int YccComponents = 3;

        private readonly PdfDictionary imageDictionary;
        private readonly PdfStream imageDictionaryStream;
        private readonly ColorSpace colorSpace;
        private readonly DecodeArray decodeArray;
        private readonly int? dctDecodeColorTransform;

        public JpegImage(PdfDictionary imageDictionary, ColorSpace colorSpace)
            : base(imageDictionary, "image/jpeg", ".jpeg")
        {
            if (imageDictionary.Stream == null)
            {
                throw new ArgumentException("There was no data stream attached to the image dictionary.", nameof(imageDictionary));
            }

            this.imageDictionary = imageDictionary;
            this.imageDictionaryStream = imageDictionary.Stream;
            this.colorSpace = colorSpace;
            this.decodeArray = ImageHelper.GetDecodeArray(imageDictionary, colorSpace);

            var lastFilter = imageDictionaryStream.Filters.LastOrDefault();
            this.dctDecodeColorTransform = lastFilter?.DecodeParms?.GetValueOrDefault<int?>(Names.ColorTransform);
        }

        public JpegColorSpace GetSourceColorSpace(JpegDecoder decoder)
        {
            // ISO 32000-2:2020 - Table 13 - Optional parameter for the DCTDecode filter

            // The color transform should be ignored if the number of components is 1 or 2
            if (decoder.Components <= 2)
            {
                return decoder.Components switch
                {
                    1 => JpegColorSpace.Gray,
                    _ => JpegColorSpace.Unknown,
                };
            }

            // APP14 marker in the JPEG data overrides the ColorTransform parameter
            if (decoder.HasAdobeMarker)
            {
                return decoder.ColorSpace;
            }

            // Default value for ColorTransform depends on the number of components
            var colorTransform = dctDecodeColorTransform == null
                ? (decoder.Components == 3)
                : (dctDecodeColorTransform == 1);

            if (colorTransform)
            {
                return decoder.Components switch
                {
                    1 => JpegColorSpace.Gray,
                    3 => JpegColorSpace.YCbCr,
                    4 => JpegColorSpace.Ycck,
                    _ => JpegColorSpace.Unknown,
                };
            }
            else
            {
                return decoder.Components switch
                {
                    1 => JpegColorSpace.Gray,
                    3 => JpegColorSpace.Rgb,
                    4 => JpegColorSpace.Cmyk,
                    _ => JpegColorSpace.Unknown,
                };
            }
        }

        private bool IsPassThroughPossible(JpegColorSpace sourceColorSpace)
        {
            if (ImageHelper.HasCustomDecodeArray(imageDictionary, colorSpace))
            {
                return false;
            }

            if (colorSpace is UnsupportedColorSpace &&
                (sourceColorSpace == JpegColorSpace.Gray || sourceColorSpace == JpegColorSpace.YCbCr || sourceColorSpace == JpegColorSpace.Unknown))
            {
                return true;
            }

            if (colorSpace is DeviceRgbColorSpace &&
                sourceColorSpace == JpegColorSpace.YCbCr)
            {
                return true;
            }

            if (colorSpace is DeviceGrayColorSpace &&
                sourceColorSpace == JpegColorSpace.Gray)
            {
                return true;
            }

            return false;
        }

        private byte[] Convert(byte[] sourceJpegData, CancellationToken cancellationToken)
        {
            var decoder = new JpegDecoder();
            decoder.ReadMetadata(sourceJpegData, 0, sourceJpegData.Length);

            var sourceColorSpace = GetSourceColorSpace(decoder);

            if (IsPassThroughPossible(sourceColorSpace))
            {
                return sourceJpegData;
            }

            if (!decoder.IsSupported)
            {
                // Possible cause: progressive JPEG.
                // Just return it and hope for the best.
                return sourceJpegData;
            }

            return FullTranscode(decoder, sourceColorSpace, cancellationToken);
        }

        private byte[] FullTranscode(JpegDecoder decoder, JpegColorSpace sourceColorSpace, CancellationToken cancellationToken)
        {
            var encoder = new JpegEncoder();

            encoder.Width = decoder.Width;
            encoder.Height = decoder.Height;
            encoder.ColorSpace = JpegColorSpace.YCbCr;
            encoder.Quality = 90;

            encoder.WriteMetadata();

            var floatScan = ArrayUtils.Empty<float>();
            var convertedScan = ArrayUtils.Empty<short>();

            foreach (var scan in decoder.ReadImageData())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sampleCount = scan.Length / decoder.Components;

                // Convert to float
                if (floatScan.Length != scan.Length)
                {
                    floatScan = new float[scan.Length];
                }

                for (var i = 0; i < scan.Length; i++)
                {
                    floatScan[i] = scan[i];
                }

                // Reverse DCTDecode implicit color transform
                switch (sourceColorSpace)
                {
                    case JpegColorSpace.YCbCr:
                        JpegColorSpaceTransform.YccToRgb(floatScan, 0, floatScan.Length);
                        break;

                    case JpegColorSpace.Ycck:
                        JpegColorSpaceTransform.YcckToCmyk(floatScan, 0, floatScan.Length);
                        break;
                }

                // Decode
                decodeArray.Decode(floatScan, 0, floatScan.Length);

                // Convert to RGB
                var convertedLength = sampleCount * YccComponents;
                if (convertedScan.Length != convertedLength)
                {
                    convertedScan = new short[convertedLength];
                }

                colorSpace.ToRgb8(floatScan, 0, convertedScan, 0, sampleCount);

                // Convert to YCbCr
                JpegColorSpaceTransform.RgbToYcc(convertedScan, 0, convertedScan.Length);

                // Done!
                encoder.WriteImageData(convertedScan, 0, convertedLength);
            }

            encoder.WriteEndImage();

            return encoder.ToByteArray();
        }

        public override byte[] GetContent(CancellationToken cancellationToken)
        {
            return Convert(ImageHelper.GetContent(imageDictionaryStream, cancellationToken), cancellationToken);
        }

#if HAVE_ASYNC
        public override async Task<byte[]> GetContentAsync(CancellationToken cancellationToken)
        {
            var sourceJpegData = await ImageHelper
                .GetContentAsync(imageDictionaryStream, cancellationToken)
                .ConfigureAwait(false);

            return Convert(sourceJpegData, cancellationToken);
        }
#endif

        public override int GetHashCode() =>
            611922474 ^
            RuntimeHelpers.GetHashCode(imageDictionaryStream) ^
            colorSpace.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is JpegImage jpegImage &&
            ReferenceEquals(jpegImage.imageDictionaryStream, imageDictionaryStream) &&
            jpegImage.colorSpace.Equals(colorSpace);
    }
}
