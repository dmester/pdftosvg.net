// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging.Jpeg;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
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

            if ((colorSpace is NullColorSpace || colorSpace is UnsupportedColorSpace) &&
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

            if (IsFastTranscodePossible(decoder, sourceColorSpace))
            {
                return FastTranscode(decoder, sourceColorSpace, cancellationToken);
            }
            else
            {
                return RowTranscode(decoder, sourceColorSpace, cancellationToken);
            }
        }

        /// <summary>
        /// Determines whether the de-interleaved fast block path can be used. It skips the upsampling/interleaving
        /// done by <see cref="RowTranscode"/> and streams 8x8 blocks straight from the decoder through the color
        /// transform to the encoder.
        /// </summary>
        private bool IsFastTranscodePossible(JpegDecoder decoder, JpegColorSpace sourceColorSpace)
        {
            // The fast color transforms only cover device CMYK -> YCbCr and YCCK -> YCbCr
            if (sourceColorSpace != JpegColorSpace.Cmyk &&
                sourceColorSpace != JpegColorSpace.Ycck)
            {
                return false;
            }

            if (colorSpace is not DeviceCmykColorSpace)
            {
                return false;
            }

            // The fast path reads raw samples without applying a decode array
            if (ImageHelper.HasCustomDecodeArray(imageDictionary, colorSpace))
            {
                return false;
            }

            // Block grouping assumes exactly one block per component per MCU
            if (decoder.ChromaSubSampling != JpegChromaSubSampling.None &&
                decoder.ChromaSubSampling != JpegChromaSubSampling.Ratio444)
            {
                return false;
            }

            return true;
        }

        private byte[] FastTranscode(JpegDecoder decoder, JpegColorSpace sourceColorSpace, CancellationToken cancellationToken)
        {
            const int BlockSize = 8 * 8;
            const int BlockBatchCount = 256;

            var encoder = new JpegEncoder
            {
                Width = decoder.Width,
                Height = decoder.Height,
                ColorSpace = JpegColorSpace.YCbCr,
                ChromaSubSampling = JpegChromaSubSampling.None,
                Quality = decoder.Quality,
            };

            encoder.WriteMetadata();

            // ReadBlocks keeps the block count a multiple of the source component count
            var blocks = new float[BlockSize * BlockBatchCount];

            foreach (var readBlockCount in decoder.ReadBlocks(blocks))
            {
                cancellationToken.ThrowIfCancellationRequested();

                int convertedBlocks;

                switch (sourceColorSpace)
                {
                    case JpegColorSpace.Cmyk:
                        convertedBlocks = JpegColorSpaceTransform.CmykBlocksToYcc(blocks, readBlockCount);
                        break;

                    case JpegColorSpace.Ycck:
                        convertedBlocks = JpegColorSpaceTransform.YcckBlocksToYcc(blocks, readBlockCount);
                        break;

                    default:
                        throw new PdfException(
                            "Unexpected state. Color space should have been either CMYK or YCCK but was " + sourceColorSpace + ".");
                }

                encoder.WriteBlocks(blocks, convertedBlocks);
            }

            encoder.WriteEndImage();

            return encoder.ToByteArray();
        }

        private byte[] RowTranscode(JpegDecoder decoder, JpegColorSpace sourceColorSpace, CancellationToken cancellationToken)
        {
            var encoder = new JpegEncoder
            {
                Width = decoder.Width,
                Height = decoder.Height,
                ColorSpace = JpegColorSpace.YCbCr,
                ChromaSubSampling = decoder.ChromaSubSampling,
                Quality = decoder.Quality,
            };

            encoder.WriteMetadata();

            var convertedScan = ArrayUtils.Empty<float>();

            foreach (var scan in decoder.ReadImageData())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Reverse DCTDecode implicit color transform
                switch (sourceColorSpace)
                {
                    case JpegColorSpace.YCbCr:
                        JpegColorSpaceTransform.YccToRgb(scan, 0, scan.Length);
                        break;

                    case JpegColorSpace.Ycck:
                        JpegColorSpaceTransform.YcckToCmyk(scan, 0, scan.Length);
                        break;
                }

                // Decode
                decodeArray.Decode(scan, 0, scan.Length);

                // Convert to RGB
                var sampleCount = scan.Length / decoder.Components;
                var convertedLength = sampleCount * YccComponents;
                if (convertedScan.Length != convertedLength)
                {
                    convertedScan = new float[convertedLength];
                }

                colorSpace.ToRgb8(scan, 0, convertedScan, 0, sampleCount);

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
