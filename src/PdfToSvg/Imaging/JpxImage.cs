// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging.Jpeg;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.Container;
using PdfToSvg.Imaging.Jpx.Transforms;
using PdfToSvg.Imaging.Png;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#if HAVE_ASYNC
using System.Threading.Tasks;
#endif

namespace PdfToSvg.Imaging
{
    internal class JpxImage : Image
    {
        private enum TranscodeMode
        {
            IndexedPdf,
            IndexedJP2,
            Lossless,
            Lossy,
        }

        private enum LossyBlockTransform
        {
            None,
            RgbToYcc,
            CmykToYcc,
        }

        private const int MaxPngIndexedColors = 256;
        private const int MaxPngIndexedPrecision = 8;

        private readonly PdfDictionary imageDictionary;
        private readonly PdfStream imageDictionaryStream;
        private readonly ColorSpace colorSpace;
        private readonly float maxSourceValue;
        private ColorSpace effectiveColorSpace;

        private readonly JpxDecoder decoder = new JpxDecoder();
        private readonly JpxAlphaMode alphaMode;
        private TranscodeMode transcodeMode;

        public JpxImage(PdfDictionary imageDictionary, ColorSpace colorSpace)
            : base(imageDictionary, "image/jpeg", ".jpeg")
        {
            if (imageDictionary.Stream == null)
            {
                throw new ArgumentException(
                    "There was no data stream attached to the image dictionary.", nameof(imageDictionary));
            }

            this.imageDictionary = imageDictionary;
            this.imageDictionaryStream = imageDictionary.Stream;
            this.colorSpace = colorSpace;
            this.effectiveColorSpace = colorSpace;

            this.maxSourceValue = (1 << ImageHelper.GetBitsPerComponent(imageDictionary)) - 1;
            this.alphaMode = imageDictionary.GetValueOrDefault(Names.SMaskInData, 0) switch
            {
                1 => JpxAlphaMode.Opacity,
                2 => JpxAlphaMode.Premultiplied,
                _ => JpxAlphaMode.None,
            };
        }

        internal override void Initialize(CancellationToken cancellationToken)
        {
            ReadMetadata(ImageHelper.GetContent(imageDictionaryStream, cancellationToken));
        }

#if HAVE_ASYNC
        internal override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            var content = await ImageHelper
                .GetContentAsync(imageDictionaryStream, cancellationToken)
                .ConfigureAwait(false);

            ReadMetadata(content);
        }
#endif

        private void ReadMetadata(byte[] data)
        {
            decoder.ReadMetadata(data, 0, data.Length);

            var channelInfo = decoder.GetChannelInfo(alphaMode);

            effectiveColorSpace = DetermineEffectiveColorSpace(channelInfo);
            transcodeMode = DetermineTranscodeMode(channelInfo.HasAlphaChannel);

            decoder.SetConstraints(effectiveColorSpace.ComponentsPerSample, alphaMode);

            if (transcodeMode != TranscodeMode.Lossy)
            {
                ContentType = "image/png";
                Extension = ".png";
            }
            else
            {
                ContentType = "image/jpeg";
                Extension = ".jpeg";
            }
        }

        private ColorSpace DetermineEffectiveColorSpace(JpxChannelInfo channelInfo)
        {
            // ISO 32000-2:2020 Section 7.4.9 says PDF color space has precedence over JPEG 2000 color space
            if (colorSpace is not NullColorSpace &&
                colorSpace is not UnsupportedColorSpace)
            {
                return colorSpace;
            }

            var colorCount = channelInfo.ColorChannelCount;

            if (colorCount >= 1 && decoder.EnumeratedColorSpace == JpxEnumeratedColorSpace.Greyscale ||
                colorCount == 1)
            {
                return new DeviceGrayColorSpace();
            }

            if (colorCount >= 3 && decoder.EnumeratedColorSpace == JpxEnumeratedColorSpace.SRgb ||
                colorCount == 3)
            {
                return new DeviceRgbColorSpace();
            }

            if (colorCount >= 4 && decoder.EnumeratedColorSpace == JpxEnumeratedColorSpace.Cmyk ||
                colorCount == 4)
            {
                return new DeviceCmykColorSpace();
            }

            throw new JpxException("Failed to determine colorspace for JPEG 2000 image");
        }

        private TranscodeMode DetermineTranscodeMode(bool hasAlpha)
        {
            // Palette in PDF
            if (colorSpace is IndexedColorSpace indexedColorSpace &&
                indexedColorSpace.ColorCount <= MaxPngIndexedColors)
            {
                return TranscodeMode.IndexedPdf;
            }

            // Palette embedded in image
            if (CanTranscodeIndexedJP2(decoder))
            {
                return TranscodeMode.IndexedJP2;
            }

            // JPEG cannot have alpha
            if (hasAlpha)
            {
                return TranscodeMode.Lossless;
            }

            return TranscodeMode.Lossy;
        }

        private static bool CanTranscodeIndexedJP2(JpxDecoder decoder)
        {
            return
                decoder.Components.Length == 1 &&
                decoder.MultipleComponentTransformation == false &&
                (
                    decoder.ComponentMappings.Length == 3 &&
                        decoder.EnumeratedColorSpace == JpxEnumeratedColorSpace.SRgb ||
                    decoder.ComponentMappings.Length == 1 &&
                        decoder.EnumeratedColorSpace == JpxEnumeratedColorSpace.Greyscale
                ) &&
                decoder.Palette != null &&
                decoder.Palette.EntryCount <= MaxPngIndexedColors &&
                decoder.Palette.Columns.All(column => !column.Signed && column.Precision <= MaxPngIndexedPrecision) &&
                decoder.ComponentMappings.All(mapping => mapping.Type == JpxComponentMappingType.Palette);
        }

        private byte[] Transcode(CancellationToken cancellationToken)
        {
            return transcodeMode switch
            {
                TranscodeMode.Lossy => TranscodeLossy(cancellationToken),
                TranscodeMode.IndexedJP2 => TranscodeIndexedJp2(cancellationToken),
                TranscodeMode.IndexedPdf => TranscodeIndexedPdf(cancellationToken),
                _ => TranscodeLossless(cancellationToken),
            };
        }

        private byte[] TranscodeIndexedJp2(CancellationToken cancellationToken)
        {
            // Indexed PNG using the JP2 palette box (ITU-T T.800 I.5.3.4 / I.5.3.5).

            var palette = decoder.Palette ?? throw new JpxException("Expected a palette in this state.");
            var mappings = decoder.ComponentMappings;

            var indexComponent = mappings.Length > 0 ? mappings[0].ComponentIndex : 0;

            return WriteIndexedPng(
                BuildJp2Palette(palette, mappings),
                palette.EntryCount,
                decoder.GetComponentIndices(indexComponent, cancellationToken),
                cancellationToken);
        }

        private byte[] TranscodeIndexedPdf(CancellationToken cancellationToken)
        {
            // Indexed PNG using a PDF /Indexed colour space (palette stored in the PDF).

            if (effectiveColorSpace is not IndexedColorSpace indexedColorSpace)
            {
                throw new InvalidOperationException("Expected " + nameof(IndexedColorSpace));
            }

            const int OutputChannelCount = 3;
            var colorCount = indexedColorSpace.ColorCount;

            // Resolve each palette entry through the /Decode array and the base colour space.
            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, indexedColorSpace);
            var palette = new byte[colorCount * OutputChannelCount];
            var indexedColorValue = new float[1];

            for (var i = 0; i < colorCount; i++)
            {
                indexedColorValue[0] = i;
                decodeArray.Decode(indexedColorValue, offset: 0, count: 1);
                indexedColorSpace.ToRgb8(
                    indexedColorValue, inputOffset: 0,
                    palette,
                    rgbBufferOffset: i * 3, count: 1);
            }

            return WriteIndexedPng(palette, colorCount, decoder.GetComponentIndices(0, cancellationToken),
                cancellationToken);
        }

        private byte[] WriteIndexedPng(byte[] rgbPalette, int colorCount, byte[] indices,
            CancellationToken cancellationToken)
        {
            var bitsPerComponent =
                colorCount < 2 ? 1 :
                colorCount < 4 ? 2 :
                colorCount < 16 ? 4 :
                8;

            var pngStream = new MemoryStream();
            var pngWriter = new PngEncoder(pngStream);

            pngWriter.WriteSignature();
            pngWriter.WriteImageHeader(
                decoder.DecodedWidth, decoder.DecodedHeight,
                PngColorType.IndexedColour, bitsPerComponent);
            pngWriter.WriteImageGamma();
            pngWriter.WritePalette(rgbPalette);

            using (var pngDataStream = pngWriter.GetImageDataStream())
            {
                var pngRow = new byte[1 + MathUtils.BitsToBytes(decoder.DecodedWidth * bitsPerComponent)];

                for (var y = 0; y < decoder.DecodedHeight; y++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Array.Clear(pngRow, 0, pngRow.Length);

                    var pngRowCursor = 1;
                    var packedByteCursor = 0;
                    var packedByteValue = 0;
                    var rowOffset = y * decoder.DecodedWidth;

                    for (var x = 0; x < decoder.DecodedWidth; x++)
                    {
                        var index = (int)indices[rowOffset++];
                        if (index >= colorCount)
                        {
                            index = 0;
                        }

                        packedByteValue = (packedByteValue << bitsPerComponent) | index;
                        packedByteCursor += bitsPerComponent;

                        if (packedByteCursor == 8)
                        {
                            pngRow[pngRowCursor++] = (byte)packedByteValue;
                            packedByteCursor = 0;
                            packedByteValue = 0;
                        }
                    }

                    if (packedByteCursor > 0)
                    {
                        packedByteValue <<= 8 - packedByteCursor;
                        pngRow[pngRowCursor] = (byte)packedByteValue;
                    }

                    pngDataStream.Write(pngRow, 0, pngRow.Length);
                }
            }

            pngWriter.WriteImageEnd();

            return pngStream.ToArray();
        }

        private byte[] TranscodeLossy(CancellationToken cancellationToken)
        {
            var image = decoder.ReadImageData(cancellationToken);
            var channels = image.ColorChannels;
            var pixelCount = image.Width * image.Height;

            if (IsSYccEncoded(channels.Length))
            {
                if (!ImageHelper.HasCustomDecodeArray(imageDictionary, effectiveColorSpace))
                {
                    // sYCC and JPEG YCbCr share the BT.601 colour model, so the decoded samples
                    // can be encoded without colour conversion. Only the scale differs: the block
                    // reader maps [0, 1] samples (chrominance centered around 0.5) to [0, 255]
                    // (centered around 127.5).
                    return TranscodeLossy8x8Blocks(image, JpegColorSpace.YCbCr, LossyBlockTransform.None,
                        cancellationToken);
                }
                else
                {
                    // A custom /Decode array is defined on the RGB values and requires converting to
                    // RGB before the decode ranges are applied by the block reader.
                    JpxColorSpaceTransform.YccToRgb(channels, pixelCount);
                    return TranscodeLossy8x8Blocks(image, JpegColorSpace.YCbCr, LossyBlockTransform.RgbToYcc,
                        cancellationToken);
                }
            }

            switch (effectiveColorSpace)
            {
                case DeviceGrayColorSpace when channels.Length == 1:
                    return TranscodeLossy8x8Blocks(image, JpegColorSpace.Gray, LossyBlockTransform.None,
                        cancellationToken);

                case DeviceRgbColorSpace when channels.Length == 3:
                    return TranscodeLossy8x8Blocks(image, JpegColorSpace.YCbCr, LossyBlockTransform.RgbToYcc,
                        cancellationToken);

                case DeviceCmykColorSpace when channels.Length == 4:
                    return TranscodeLossy8x8Blocks(image, JpegColorSpace.YCbCr, LossyBlockTransform.CmykToYcc,
                        cancellationToken);

                default:
                    return TranscodeLossyGeneric(image, cancellationToken);
            }
        }

        private byte[] TranscodeLossy8x8Blocks(
            JpxImageData image, JpegColorSpace jpegColorSpace, LossyBlockTransform blockTransform,
            CancellationToken cancellationToken)
        {
            const int BlockLength = 8 * 8;
            const int BlockBatchCount = 256;

            var encoder = new JpegEncoder
            {
                Width = image.Width,
                Height = image.Height,
                ColorSpace = jpegColorSpace,
                Quality = 90,
            };

            encoder.WriteMetadata();

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, effectiveColorSpace);
            var blockTiler = new JpegBlockTiler(
                image.ColorChannels, image.Width, image.Height, maxSourceValue, decodeArray);

            // ReadBlocks keeps the block count a multiple of the source channel count
            var blocks = new float[BlockLength * BlockBatchCount];

            foreach (var readBlockCount in blockTiler.ReadBlocks(blocks))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var convertedBlockCount = blockTransform switch
                {
                    LossyBlockTransform.RgbToYcc => JpegColorSpaceTransform.RgbBlocksToYcc(blocks, readBlockCount),
                    LossyBlockTransform.CmykToYcc => JpegColorSpaceTransform.CmykBlocksToYcc(blocks, readBlockCount),
                    _ => readBlockCount,
                };

                encoder.WriteBlocks(blocks, convertedBlockCount);
            }

            encoder.WriteEndImage();

            return encoder.ToByteArray();
        }

        /// <summary>
        /// Transcodes colour spaces without a dedicated planar path (e.g. ICC based colour
        /// spaces) through the generic <see cref="ColorSpace"/> conversion on interleaved
        /// samples. The image is processed in bands of one MCU row to bound the size of the
        /// interleaved buffers.
        /// </summary>
        private byte[] TranscodeLossyGeneric(JpxImageData image, CancellationToken cancellationToken)
        {
            const int BandHeight = 8;
            const int YccComponents = 3;

            var encoder = new JpegEncoder
            {
                Width = image.Width,
                Height = image.Height,
                ColorSpace = JpegColorSpace.YCbCr,
                Quality = 90,
            };

            encoder.WriteMetadata();

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, effectiveColorSpace);

            var channelCount = image.ColorChannels.Length;
            var bandSamples = new float[image.Width * BandHeight * channelCount];
            var bandYcc = new float[image.Width * BandHeight * YccComponents];

            for (var bandStart = 0; bandStart < image.Height; bandStart += BandHeight)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bandPixelCount = Math.Min(BandHeight, image.Height - bandStart) * image.Width;

                ScaledInterleave(
                    image.ColorChannels, bandStart * image.Width, bandPixelCount, bandSamples, maxSourceValue);

                decodeArray.Decode(bandSamples, 0, bandPixelCount * channelCount);
                effectiveColorSpace.ToRgb8(bandSamples, 0, bandYcc, 0, bandPixelCount);
                JpegColorSpaceTransform.RgbToYcc(bandYcc, 0, bandPixelCount * YccComponents);

                encoder.WriteImageData(bandYcc, 0, bandPixelCount * YccComponents);
            }

            encoder.WriteEndImage();

            return encoder.ToByteArray();
        }

        private byte[] TranscodeLossless(CancellationToken cancellationToken)
        {
            var image = decoder.ReadImageData(cancellationToken);
            var channels = image.ColorChannels;
            var alpha = image.AlphaChannel;
            var pixelCount = image.Width * image.Height;

            if (alpha == null)
            {
                throw new InvalidOperationException(
                    "Lossless transcode of JPEG 2000 images only intended for images with alpha");
            }

            if (IsSYccEncoded(channels.Length))
            {
                JpxColorSpaceTransform.YccToRgb(channels, pixelCount);
            }

            // The /Decode array applies only to color values (ISO 32000-2:2020 Table 87); opacity is used as decoded
            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, effectiveColorSpace);

            const int BandHeight = 8;
            const int RgbaComponents = 4;
            const int AlphaOffset = 3;
            const int FilterByte = 1;

            var width = image.Width;
            var height = image.Height;

            var pngStream = new MemoryStream();
            var pngWriter = new PngEncoder(pngStream);

            pngWriter.WriteSignature();
            pngWriter.WriteImageHeader(width, height, PngColorType.TruecolourWithAlpha, bitDepth: 8);

            var bandSamples = new float[width * BandHeight * channels.Length];
            var bandRgba = new byte[BandHeight * (FilterByte + width * RgbaComponents)];

            using (var imageStream = pngWriter.GetImageDataStream())
            {
                for (var bandStart = 0; bandStart < height; bandStart += BandHeight)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var bandRowCount = Math.Min(BandHeight, height - bandStart);
                    var bandPixelCount = bandRowCount * width;
                    var bandPixelOffset = bandStart * width;
                    var bandCursor = 0;

                    ScaledInterleave(channels, bandPixelOffset, bandPixelCount, bandSamples, maxSourceValue);
                    decodeArray.Decode(bandSamples, 0, bandPixelCount * channels.Length);

                    // Sub filter is easy to perform without keeping track of the previous row
                    for (var row = 0; row < bandRowCount; row++)
                    {
                        bandRgba[bandCursor++] = (byte)PngFilter.Sub;

                        // Start Sub filter on the second pixel
                        var filterStartIndex = bandCursor + RgbaComponents;
                        var filterEndIndex = filterStartIndex + (width - 1) * RgbaComponents;

                        effectiveColorSpace.ToRgba8(
                            // Input
                            bandSamples,
                            row * width * channels.Length,
                            // Output
                            bandRgba,
                            bandCursor,
                            width);

                        for (var x = 0; x < width; x++)
                        {
                            bandRgba[bandCursor + AlphaOffset] = ToByte(alpha[bandPixelOffset++]);
                            bandCursor += RgbaComponents;
                        }

                        // Filter Sub
                        for (var i = filterEndIndex - RgbaComponents; i >= filterStartIndex; i--)
                        {
                            bandRgba[i] = (byte)(bandRgba[i] - bandRgba[i - RgbaComponents]);
                        }
                    }

                    imageStream.Write(bandRgba, 0, bandCursor);
                }
            }

            pngWriter.WriteImageEnd();

            return pngStream.ToArray();
        }

        private static byte[] BuildJp2Palette(JpxPaletteBox palette, JpxComponentMappingBox[] mappings)
        {
            const int PaletteBytesPerColor = 3;

            static byte ScalePaletteValue(int value, JpxPaletteColumn column)
            {
                var maxValue = column.MaxValue;
                return maxValue <= 0 ? (byte)0 : ToByte((float)value / maxValue);
            }

            var result = new byte[palette.EntryCount * PaletteBytesPerColor];
            var grayscale = mappings.Length < 3;

            for (var i = 0; i < palette.EntryCount; i++)
            {
                if (grayscale)
                {
                    var column = mappings[0].PaletteColumnIndex;
                    var gray = ScalePaletteValue(palette.Values[i, column], palette.Columns[column]);
                    result[i * PaletteBytesPerColor + 0] = gray;
                    result[i * PaletteBytesPerColor + 1] = gray;
                    result[i * PaletteBytesPerColor + 2] = gray;
                }
                else
                {
                    for (var channel = 0; channel < PaletteBytesPerColor; channel++)
                    {
                        var column = mappings[channel].PaletteColumnIndex;
                        result[i * PaletteBytesPerColor + channel] =
                            ScalePaletteValue(palette.Values[i, column], palette.Columns[column]);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Interleaves a range of pixels into a pixel-major sample buffer, scaling the normalized [0, 1] samples to
        /// the raw integer domain [0, <paramref name="maxSourceValue"/>].
        /// </summary>
        private static void ScaledInterleave(
            float[][] sourcePlanes, int sourcePlaneOffset, int pixelCount, float[] destinationSamples,
            float maxSourceValue)
        {
            var channelCount = sourcePlanes.Length;

            for (var channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                var plane = sourcePlanes[channelIndex];
                var targetIndex = channelIndex;

                for (var i = 0; i < pixelCount; i++, targetIndex += channelCount)
                {
                    destinationSamples[targetIndex] = plane[sourcePlaneOffset + i] * maxSourceValue;
                }
            }
        }

        private bool IsSYccEncoded(int channelCount)
        {
            return
                channelCount == 3 &&
                colorSpace is NullColorSpace &&
                decoder.EnumeratedColorSpace == JpxEnumeratedColorSpace.SYcc;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static byte ToByte(float normalized)
        {
            return (byte)MathUtils.Clamp((int)(normalized * 255f + 0.5f), 0, 255);
        }

        public override byte[] GetContent(CancellationToken cancellationToken)
        {
            return Transcode(cancellationToken);
        }

#if HAVE_ASYNC
        public override Task<byte[]> GetContentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Transcode(cancellationToken));
        }
#endif

        public override int GetHashCode()
        {
            return
                unchecked((int)0x9b3a7c11) ^
                RuntimeHelpers.GetHashCode(imageDictionaryStream) ^
                colorSpace.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return
                obj is JpxImage jpxImage &&
                ReferenceEquals(jpxImage.imageDictionaryStream, imageDictionaryStream) &&
                jpxImage.colorSpace.Equals(colorSpace);
        }
    }
}
