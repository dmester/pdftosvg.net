// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.Container;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PdfToSvg.Imaging.Jpx.Decoding
{
    /// <summary>
    /// A JPEG 2000 image whose container metadata and codestream main header, but no image data, have been read.
    /// </summary>
    internal sealed class JpxImageReader
    {
        private readonly JpxImageInfo image;
        private readonly JpxCodestreamReader codestreamReader;
        private readonly JpxCodingStyleDefaults codingStyleDefaults;

        private bool imageRead;

        private JpxImageReader(JpxImageInfo image, JpxCodestreamReader codestreamReader,
            JpxCodingStyleDefaults codingStyleDefaults)
        {
            this.image = image;
            this.codestreamReader = codestreamReader;
            this.codingStyleDefaults = codingStyleDefaults;
        }

        /// <summary>
        /// Parses the JP2 container and the codestream main header
        /// (phase 1, ITU-T T.800 (06/2019) Annex A).
        /// </summary>
        public static JpxImageReader Create(byte[] data, int offset, int count)
        {
            var container = JpxBoxParser.Parse(data, offset, count);

            var image = new JpxImageInfo { Codestream = container.Codestream };

            if (container.IsJp2)
            {
                image.ImageHeader = container.ImageHeader;
                image.BitsPerComponent = container.BitsPerComponent;
                image.EnumeratedColorSpace = container.EnumeratedColorSpace;
                image.Palette = container.Palette;
                image.ComponentMappings = container.ComponentMappings;
                image.ChannelDefinitions = container.ChannelDefinitions;
            }

            var codestreamReader = new JpxCodestreamReader(container.Codestream);
            codestreamReader.ReadMainHeader(image);

            var codingStyleDefaults =
                codestreamReader.MainHeaderParameters.CodingStyleDefaults ?? new JpxCodingStyleDefaults();

            return new JpxImageReader(image, codestreamReader, codingStyleDefaults);
        }

        public JpxImageInfo Image => image;

        public JpxCodingStyleDefaults CodingStyleDefaults => codingStyleDefaults;

        /// <summary>Width of the image area at full resolution.</summary>
        public int NativeWidth => image.Xsiz - image.XOsiz;

        /// <summary>Height of the image area at full resolution.</summary>
        public int NativeHeight => image.Ysiz - image.YOsiz;

        public JpxChannelInfo GetChannelInfo(JpxAlphaMode alphaMode)
        {
            var colorChannelCount = 0;
            var hasAlpha = false;

            if (image.ChannelDefinitions.Length == 0)
            {
                colorChannelCount = image.Components.Length;

                if (alphaMode == JpxAlphaMode.Opacity ||
                    alphaMode == JpxAlphaMode.Premultiplied)
                {
                    // Assume one of the components are alpha
                    colorChannelCount -= 1;
                    hasAlpha = true;
                }
            }
            else
            {
                foreach (var def in image.ChannelDefinitions)
                {
                    switch (def.Type)
                    {
                        case JpxChannelType.Color:
                            colorChannelCount++;
                            break;

                        case JpxChannelType.Opacity:
                        case JpxChannelType.PremultipliedOpacity:
                            hasAlpha = true;
                            break;
                    }
                }

                switch (alphaMode)
                {
                    case JpxAlphaMode.Opacity:
                    case JpxAlphaMode.Premultiplied:
                        if (!hasAlpha)
                        {
                            var addressableComponentCount = image.ComponentMappings.Length > 0
                                ? image.ComponentMappings.Length
                                : image.Components.Length;

                            hasAlpha = addressableComponentCount > colorChannelCount;
                        }
                        break;

                    case JpxAlphaMode.None:
                        hasAlpha = false;
                        break;
                }
            }

            return new JpxChannelInfo(colorChannelCount, hasAlpha);
        }

        /// <summary>
        /// Computes the resolution reduction a decode limited to <paramref name="maxResolution"/> can use.
        /// </summary>
        public int ComputeReduction(int maxResolution)
        {
            return JpxResolutionReducer.ComputeReduction(image, codestreamReader.MainHeaderParameters, maxResolution);
        }

        /// <summary>
        /// Reads image data and returns a decoder that can be used to decode it. The reader will  discard the highest
        /// <paramref name="resolutionReduction"/> levels. The limit is best-effort, since it cannot cut deeper than
        /// the decomposition levels signalled in the main header allow. The method can only be called once.
        /// </summary>
        public JpxImageDecoder ReadImage(int resolutionReduction, CancellationToken cancellationToken)
        {
            if (imageRead)
            {
                throw new InvalidOperationException("The JPEG 2000 image has already been read.");
            }

            if (image.Components.Length == 0)
            {
                throw new JpxException("The JPEG 2000 image contains no components");
            }

            imageRead = true;

            var tilePartData = ReadTileParts(cancellationToken);

            return new JpxImageDecoder(image, codestreamReader, tilePartData, resolutionReduction);
        }

        private Dictionary<int, List<ArraySegment<byte>>> ReadTileParts(CancellationToken cancellationToken)
        {
            var tilePartData = new Dictionary<int, List<ArraySegment<byte>>>();

            for (; ; )
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tilePart = codestreamReader.ReadTilePart();
                if (tilePart == null)
                {
                    break;
                }

                if (!tilePartData.TryGetValue(tilePart.TileIndex, out var dataList))
                {
                    dataList = new List<ArraySegment<byte>>();
                    tilePartData[tilePart.TileIndex] = dataList;
                }

                dataList.Add(tilePart.Data);
            }

            return tilePartData;
        }
    }
}
