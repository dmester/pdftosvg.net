// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PdfToSvg.Imaging.Jpx.Decoding
{
    /// <summary>
    /// A JPEG 2000 image whose tile-part data has been read and grouped, ready to decode. Each decode method runs
    /// a fresh destination-driven tile decode over the retained tile-parts, so the produced planes can be handed
    /// out to the consumers without cloning. All decodes share the resolution reduction fixed when the image was
    /// read.
    /// </summary>
    internal sealed class JpxImageDecoder
    {
        private readonly JpxImageInfo image;
        private readonly JpxCodestreamReader codestreamReader;
        private readonly Dictionary<int, List<ArraySegment<byte>>> tilePartData;
        private readonly int resolutionReduction;

        internal JpxImageDecoder(JpxImageInfo image, JpxCodestreamReader codestreamReader,
            Dictionary<int, List<ArraySegment<byte>>> tilePartData, int resolutionReduction)
        {
            this.image = image;
            this.codestreamReader = codestreamReader;
            this.tilePartData = tilePartData;
            this.resolutionReduction = resolutionReduction;
        }

        /// <summary>
        /// Decodes a single component raw: reconstructed samples at the component's native sample grid (reduced
        /// when a resolution reduction applies), DC level shifted into the component's own range, without MCT,
        /// palette lookup, normalization or clamping. The returned plane is owned by the caller.
        /// </summary>
        public float[] DecodeComponentSamples(int componentIndex, CancellationToken cancellationToken)
        {
            var usages = new JpxComponentUsage[image.Components.Length];
            usages[componentIndex] = JpxComponentUsage.Raw;

            var planes = DecodeTiles(usages, cancellationToken);

            return planes[componentIndex] ??
                throw new JpxException("The JPEG 2000 component " + componentIndex + " was not decoded");
        }

        /// <summary>
        /// Decodes a single component raw into an indexed image whose index values are intended for palette lookup.
        /// </summary>
        public JpxPaletteImage DecodePaletteImage(int componentIndex, CancellationToken cancellationToken)
        {
            var usages = new JpxComponentUsage[image.Components.Length];
            usages[componentIndex] = JpxComponentUsage.Raw;

            var planes = DecodeTiles(usages, cancellationToken);

            return new JpxPaletteImage(image, planes, usages, resolutionReduction, componentIndex);
        }

        /// <summary>
        /// Decodes the components consumed by the colour and alpha channels of <paramref name="channelMap"/> into
        /// a truecolor image. This is the only decode that consumes the channel map.
        /// </summary>
        public JpxTrueColorImage DecodeTrueColorImage(JpxChannelMap channelMap, CancellationToken cancellationToken)
        {
            var usages = channelMap.CreateComponentUsages(image);
            var planes = DecodeTiles(usages, cancellationToken);

            return new JpxTrueColorImage(image, planes, usages, resolutionReduction, channelMap);
        }

        /// <summary>
        /// Decodes every tile through <see cref="JpxTileDecoder"/> into new full-image component planes. Raw planes
        /// are inverse DC level shifted into their unsigned range but not clamped; normalized planes are clamped to
        /// [0, 1]. Skipped components have no plane.
        /// </summary>
        /// <remarks>
        /// The results are intentionally not cached, to ensure the plane ownership can be handed out to consumers
        /// without having to clone them.
        /// </remarks>
        private float[]?[] DecodeTiles(JpxComponentUsage[] usages, CancellationToken cancellationToken)
        {
            var components = image.Components;

            var destinations = CreateDestinations(usages);

            var tileDecoder = new JpxTileDecoder(image, destinations, resolutionReduction);
            var tileCount = image.NumXTiles * image.NumYTiles;
            var noData = new List<ArraySegment<byte>>();

            for (var tileIndex = 0; tileIndex < tileCount; tileIndex++)
            {
                var parameters = codestreamReader.ResolveTileParameters(tileIndex);

                if (!tilePartData.TryGetValue(tileIndex, out var dataList))
                {
                    // No tile-part for this tile: decoded best-effort with all coefficients zero, which reconstructs
                    // to mid-gray for unsigned components
                    dataList = noData;
                }

                var packedHeaderReader = codestreamReader.PackedPacketHeaders.GetTileHeaderReader(tileIndex);
                if (packedHeaderReader != null)
                {
                    // A previous decode may have consumed the cached packed-header reader
                    packedHeaderReader.Cursor = 0;
                }
                tileDecoder.DecodeTile(tileIndex, parameters, dataList, packedHeaderReader, cancellationToken);
            }

            var planes = new float[]?[components.Length];
            for (var c = 0; c < components.Length; c++)
            {
                planes[c] = destinations[c].Plane;
            }

            return planes;
        }

        /// <summary>
        /// Allocates the component planes and computes the sample transform each component should be stored with.
        /// </summary>
        private JpxComponentDestination[] CreateDestinations(JpxComponentUsage[] usages)
        {
            var components = image.Components;
            var destinations = new JpxComponentDestination[components.Length];

            // Validate the number and total size of the decoded component planes
            var planeCount = 0;
            var totalPlaneSamples = 0L;

            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                if (usages[componentIndex] == JpxComponentUsage.Skipped)
                {
                    continue;
                }

                JpxResolutionReducer.GetComponentPlaneArea(image, components[componentIndex], resolutionReduction,
                    out _, out _, out var width, out var height);
                planeCount++;
                totalPlaneSamples += (long)width * height;
            }

            if (planeCount > JpxConstraints.MaxOutputPlanes)
            {
                throw new JpxException(
                    "The JPEG 2000 decode would require " + planeCount + " component planes. " +
                    "Maximum output planes is " + JpxConstraints.MaxOutputPlanes + ".");
            }
            if (totalPlaneSamples > JpxConstraints.MaxComponentSamples)
            {
                throw new JpxException(
                    "The JPEG 2000 image component planes have too many samples (" + totalPlaneSamples + "). " +
                    "Maximum component samples is " + JpxConstraints.MaxComponentSamples + ".");
            }

            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                var usage = usages[componentIndex];
                if (usage == JpxComponentUsage.Skipped)
                {
                    continue;
                }

                var component = components[componentIndex];
                ref var destination = ref destinations[componentIndex];

                JpxResolutionReducer.GetComponentPlaneArea(image, component, resolutionReduction,
                    out var planeX0, out var planeY0, out var width, out var height);

                destination.Plane = new float[width * height];
                destination.PlaneX0 = planeX0;
                destination.PlaneY0 = planeY0;
                destination.PlaneWidth = width;

                if (usage == JpxComponentUsage.Normalized)
                {
                    // The offset translates the reconstructed samples to the unsigned application range before the
                    // normalization: the inverse DC level shift for unsigned components (ITU-T T.800 (06/2019)
                    // Section G.1.2), or the signed sample interval translation for signed components (Section
                    // A.5.1). Both equal 2^(precision-1). Reconstructed samples may exceed the dynamic range of the
                    // original samples and are clamped (Section G.1.2 NOTE).
                    destination.SampleOffset = 1 << (component.Precision - 1);
                    destination.SampleScale = 1f / component.MaxValue;
                    destination.Normalize = true;
                }
                else
                {
                    // ITU-T T.800 (06/2019) Section G.1.2: inverse DC level shift of unsigned components
                    destination.SampleOffset = component.DcShift;
                    destination.SampleScale = 1f;
                    destination.Normalize = false;
                }
            }

            return destinations;
        }
    }
}
