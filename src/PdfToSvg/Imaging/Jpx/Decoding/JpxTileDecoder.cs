// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.Coding;
using PdfToSvg.Imaging.Jpx.ImageModel;
using PdfToSvg.Imaging.Jpx.IO;
using PdfToSvg.Imaging.Jpx.Packets;
using PdfToSvg.Imaging.Jpx.Transforms;
using System;
using System.Collections.Generic;
using System.Threading;

#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
#endif

namespace PdfToSvg.Imaging.Jpx.Decoding
{
    /// <summary>
    /// Decodes one tile: Tier-2 packet decoding, Tier-1 entropy decoding, dequantization, inverse wavelet
    /// transformation and inverse multiple component transformation. The reconstructed samples of each component
    /// are transformed and stored into the caller's <see cref="JpxComponentDestination"/> planes; components
    /// without a destination plane are skipped entirely, except when needed as input to the multiple component
    /// transformation. An instance can be reused across tiles (the Tier-1 decoder and the per-component sample
    /// buffers are reused), but must not be shared between threads.
    /// </summary>
    internal sealed class JpxTileDecoder
    {
        private readonly JpxImageInfo image;
        private readonly JpxComponentDestination[] destinations;
        private readonly int resolutionReduction;
        private readonly JpxTier1Decoder tier1Decoder = new();

        private readonly float[]?[] reusableSampleBuffers;

        // Whether the image's first three components are compatible with the multiple component transformation
        // (ITU-T T.800 (06/2019) Sections G.2 and G.3), computed once since it depends only on the main header.
        private readonly bool mctComponentsCompatible;

        private bool warnedMctIncompatible;

        public JpxTileDecoder(JpxImageInfo image, JpxComponentDestination[] destinations, int resolutionReduction = 0)
        {
            if (destinations.Length != image.Components.Length)
            {
                throw new ArgumentException(
                    "Expected one destination per codestream component.", nameof(destinations));
            }

            this.image = image;
            this.destinations = destinations;
            this.resolutionReduction = resolutionReduction;
            reusableSampleBuffers = new float[image.Components.Length][];
            mctComponentsCompatible = JpxMct.AreComponentsCompatible(image.Components);
        }

        /// <summary>
        /// Decodes one tile from the bit-stream data of its tile-parts (in codestream order), storing the
        /// reconstructed samples of the tile area into the destination planes.
        /// </summary>
        /// <param name="tileIndex">Index of the tile in raster order.</param>
        /// <param name="parameters">Resolved coding parameters of the tile.</param>
        /// <param name="tilePartData">
        /// Bit-stream data of each tile-part of the tile, in codestream order. May be empty when the codestream
        /// contains no tile-part for the tile.
        /// </param>
        /// <param name="packedHeaderReader">
        /// Reader over the tile's packed packet headers (PPM/PPT), or null when the packet headers are interleaved
        /// in the bit stream.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancels the decode. Checked once per tile, per Tier-1 code-block, and before each inverse DWT.
        /// </param>
        public void DecodeTile(
            int tileIndex,
            JpxResolvedTileParameters parameters,
            List<ArraySegment<byte>> tilePartData,
            JpxDataReader? packedHeaderReader,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tile = JpxTileLayoutBuilder.BuildTile(image, tileIndex, parameters, resolutionReduction);

            // Components without a destination plane are only decoded when they feed a multiple component
            // transformation whose output is used. This predicate must match the conditions of
            // InverseMultipleComponentTransform, which decides whether the transformation actually runs.
            var mctInputsNeeded =
                parameters.MultipleComponentTransformation &&
                mctComponentsCompatible &&
                tile.Components.Length >= JpxMct.ComponentCount &&
                AnyMctOutputNeeded();

            var decodedComponents = new bool[tile.Components.Length];
            for (var c = 0; c < decodedComponents.Length; c++)
            {
                decodedComponents[c] =
                    destinations[c].Plane != null ||
                    mctInputsNeeded && c < JpxMct.ComponentCount;
            }

            // Reconstruction buffer of every decoded component, holding the sub-band coefficients in the split
            // layout until the inverse DWT interleaves them (see JpxWaveletTransform)
            var sampleBuffers = new float[]?[tile.Components.Length];
            for (var c = 0; c < sampleBuffers.Length; c++)
            {
                if (decodedComponents[c])
                {
                    sampleBuffers[c] = GetSampleBuffer(tile.Components[c], in destinations[c]);
                }
            }

            // Tier-2 (ITU-T T.800 (06/2019) Sections B.9 to B.12)
            // A tile without any tile-part has no packets; its coefficients stay zero
            if (tilePartData.Count > 0)
            {
                var tilePartReader = tilePartData.Count == 1
                    ? new JpxDataReader(tilePartData[0])
                    : new JpxDataReader(ArrayUtils.Concat(tilePartData));
                var packetReader = new JpxPacketReader(
                    tile, image.Components, parameters, tilePartReader, packedHeaderReader, decodedComponents);

                packetReader.ReadTilePackets();
            }

            // Packet progression and header decoding are complete for the whole tile. Precinct geometry and its
            // inclusion/zero-bit-plane tag trees are not used by Tier-1 or the inverse transforms, so release them
            // before the potentially large coefficient reconstruction begins.
            ReleasePrecincts(tile);

            for (var c = 0; c < tile.Components.Length; c++)
            {
                if (!decodedComponents[c])
                {
                    ReleaseCodeBlocks(tile.Components[c]);
                    continue;
                }

                var tileComponent = tile.Components[c];
                var codingStyle = parameters.ComponentCodingStyles[c];
                var quantization = tileComponent.Quantization;
                var scalarQuantization = quantization.ScalarDerived || quantization.ScalarExpounded;

                var samples = sampleBuffers[c]!;
                var stride = tileComponent.DecodedResolution.Width;
                var roiShift = tileComponent.RegionOfInterestShift;
                var precision = image.Components[c].Precision;

                // Resolution levels above the decoded resolution have no retained codeword segments (see
                // JpxPacketReader); their code-blocks are released without entropy decoding.
                for (var r = tileComponent.DecodedResolutionLevelCount; r < tileComponent.ResolutionLevels.Length; r++)
                {
                    foreach (var subBand in tileComponent.ResolutionLevels[r].SubBands)
                    {
                        subBand.CodeBlocks = ArrayUtils.Empty<JpxCodeBlock>();
                    }
                }

                // Tier-1 (ITU-T T.800 (06/2019) Annex D) and inverse quantization (Annex E), interleaved so that
                // one reusable coefficient buffer serves every code-block of the tile
                for (var r = 0; r < tileComponent.DecodedResolutionLevelCount; r++)
                {
                    var resolutionLevel = tileComponent.ResolutionLevels[r];

                    foreach (var subBand in resolutionLevel.SubBands)
                    {
                        // ITU-T T.800 (06/2019) section E.1.2.1:
                        // With the no quantization style the step size is one. The scalar styles use the step size
                        // from Equation E-3.
                        var stepSize = scalarQuantization
                            ? JpxQuantizer.GetStepSize(subBand.Type, precision, subBand.Exponent, subBand.Mantissa)
                            : 1f;

                        foreach (var codeBlock in subBand.CodeBlocks)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var coefficients =
                                tier1Decoder.Decode(codeBlock, useMidpointReconstruction: scalarQuantization);

                            // Code-blocks without decodable coding passes keep their zeroes in the sample plane
                            if (coefficients != null)
                            {
                                JpxQuantizer.DequantizeCodeBlock(
                                    codeBlock, coefficients, samples, stride, stepSize, roiShift);
                            }
                        }

                        // Tier-1 is the final consumer of code-block geometry, state and accumulated codeword
                        // segments. Release the complete object graph before inverse DWT processes the samples.
                        subBand.CodeBlocks = ArrayUtils.Empty<JpxCodeBlock>();
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // ITU-T T.800 (06/2019) Annex F. The inverse DWT runs up to the decoded resolution level, which
                // equals the full decomposition count when no resolution reduction is applied.
                JpxWaveletTransform.Inverse(tileComponent, samples,
                    tileComponent.DecodedResolutionLevelCount - 1, codingStyle.ReversibleFilter);
            }

            InverseMultipleComponentTransform(tile, sampleBuffers, parameters);

            for (var c = 0; c < tile.Components.Length; c++)
            {
                if (destinations[c].Plane != null)
                {
                    WriteTileComponent(tile.Components[c], sampleBuffers[c]!, destinations[c]);
                }
            }
        }

        /// <summary>
        /// Returns the buffer one tile-component should be reconstructed into, covering the tile-component's decoded
        /// resolution level. When that area covers the destination plane exactly, the plane itself is used, so that
        /// no separate tile buffer or tile-to-plane copy is needed; the sample transform of
        /// <see cref="WriteTileComponent"/> then runs in place. Other tile-components (multi-tile images, subsampled
        /// components, or components decoded only as input to the multiple component transformation) use a zeroed
        /// scratch buffer reused across tiles.
        /// </summary>
        private float[] GetSampleBuffer(JpxTileComponent tileComponent, in JpxComponentDestination destination)
        {
            var decodedResolution = tileComponent.DecodedResolution;
            var sampleCount = decodedResolution.Width * decodedResolution.Height;

            // The tile grid partitions each component plane (ITU-T T.800 (06/2019) Section B.3 Equation B-12), and
            // the partition is preserved at every resolution level (Equation B-14), so a tile-component whose
            // decoded area equals the plane's exact area is the only tile-component writing to the plane.
            var plane = destination.Plane;
            if (plane != null &&
                plane.Length == sampleCount &&
                decodedResolution.Width == destination.PlaneWidth &&
                decodedResolution.TrX0 == destination.PlaneX0 &&
                decodedResolution.TrY0 == destination.PlaneY0)
            {
                return plane;
            }

            // Code-blocks without decodable coding passes rely on zero-initialized samples
            var reusableSampleBuffer = reusableSampleBuffers[tileComponent.ComponentIndex];
            if (reusableSampleBuffer == null || reusableSampleBuffer.Length < sampleCount)
            {
                reusableSampleBuffer = new float[sampleCount];
                reusableSampleBuffers[tileComponent.ComponentIndex] = reusableSampleBuffer;
            }
            else
            {
                Array.Clear(reusableSampleBuffer, 0, sampleCount);
            }

            return reusableSampleBuffer;
        }

        private static void ReleasePrecincts(JpxTile tile)
        {
            foreach (var component in tile.Components)
            {
                foreach (var resolutionLevel in component.ResolutionLevels)
                {
                    resolutionLevel.Precincts = ArrayUtils.Empty<JpxPrecinct>();
                }
            }
        }

        private static void ReleaseCodeBlocks(JpxTileComponent component)
        {
            foreach (var resolutionLevel in component.ResolutionLevels)
            {
                foreach (var subBand in resolutionLevel.SubBands)
                {
                    subBand.CodeBlocks = ArrayUtils.Empty<JpxCodeBlock>();
                }
            }
        }

        private bool AnyMctOutputNeeded()
        {
            for (var c = 0; c < JpxMct.ComponentCount && c < destinations.Length; c++)
            {
                if (destinations[c].Plane != null)
                {
                    return true;
                }
            }

            return false;
        }

        // ITU-T T.800 (06/2019) Sections G.2 and G.3
        private void InverseMultipleComponentTransform(
            JpxTile tile, float[]?[] sampleBuffers, JpxResolvedTileParameters parameters)
        {
            if (!parameters.MultipleComponentTransformation || !AnyMctOutputNeeded())
            {
                return;
            }

            if (tile.Components.Length < JpxMct.ComponentCount)
            {
                Log.WriteLine(
                    "JPEG 2000 multiple component transformation signalled for an image with " +
                    "fewer than three components; skipping the transformation.");
                return;
            }

            if (!mctComponentsCompatible)
            {
                if (!warnedMctIncompatible)
                {
                    warnedMctIncompatible = true;
                    Log.WriteLine(
                        "JPEG 2000 multiple component transformation (MCT) signalled for components of " +
                        "different separation or bit depth; skipping the transformation.");
                }

                return;
            }

            // All MCT components share the same sample separation (see AreComponentsCompatible), and the resolution
            // reduction is uniform across components, so their decoded areas are equal.
            var decodedResolution0 = tile.Components[0].DecodedResolution;

            // The MCT input components were decoded whenever this point is reached (see mctInputsNeeded)
            var planes = new[] { sampleBuffers[0]!, sampleBuffers[1]!, sampleBuffers[2]! };
            var reversible = parameters.ComponentCodingStyles[0].ReversibleFilter;

            JpxMct.InverseArray(planes, decodedResolution0.Width * decodedResolution0.Height, reversible);
        }

        /// <summary>
        /// Stores the reconstructed samples of one tile-component into its destination plane, applying the
        /// destination's sample transform. Fusing the transform into the tile-to-plane copy saves the separate
        /// passes that inverse DC level shifting and normalization would otherwise make over the samples. When
        /// <paramref name="samples"/> is the destination plane itself (see <see cref="GetSampleBuffer"/>), every
        /// row maps onto itself and the transform runs in place.
        /// </summary>
        private static void WriteTileComponent(
            JpxTileComponent tileComponent, float[] samples, in JpxComponentDestination destination)
        {
            var plane = destination.Plane!;
            var decodedResolution = tileComponent.DecodedResolution;
            var tileWidth = decodedResolution.Width;

            for (var y = 0; y < decodedResolution.Height; y++)
            {
                var sourceIndex = y * tileWidth;
                var targetIndex =
                    (decodedResolution.TrY0 - destination.PlaneY0 + y) * destination.PlaneWidth +
                    decodedResolution.TrX0 - destination.PlaneX0;

                if (destination.Normalize)
                {
                    WriteRowNormalized(samples, sourceIndex, plane, targetIndex, tileWidth,
                        destination.SampleOffset, destination.SampleScale);
                }
                else
                {
                    WriteRowOffset(samples, sourceIndex, plane, targetIndex, tileWidth,
                        destination.SampleOffset);
                }
            }
        }

        private static void WriteRowNormalized(
            float[] source, int sourceIndex, float[] target, int targetIndex, int count, float offset, float scale)
        {
            var i = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var sourceRef = ref MemoryMarshal.GetArrayDataReference(source);
                ref var targetRef = ref MemoryMarshal.GetArrayDataReference(target);

                var offsets = Vector256.Create(offset);
                var scales = Vector256.Create(scale);

                for (; i + Vector256<float>.Count <= count; i += Vector256<float>.Count)
                {
                    var values = Vector256.LoadUnsafe(ref sourceRef, (nuint)(sourceIndex + i));

                    var normalized = VectorUtils.ClampNative(
                        (values + offsets) * scales,
                        Vector256<float>.Zero, Vector256<float>.One);

                    normalized.StoreUnsafe(ref targetRef, (nuint)(targetIndex + i));
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                ref var sourceRef = ref MemoryMarshal.GetArrayDataReference(source);
                ref var targetRef = ref MemoryMarshal.GetArrayDataReference(target);

                var offsets = Vector128.Create(offset);
                var scales = Vector128.Create(scale);

                for (; i + Vector128<float>.Count <= count; i += Vector128<float>.Count)
                {
                    var values = Vector128.LoadUnsafe(ref sourceRef, (nuint)(sourceIndex + i));

                    var normalized = VectorUtils.ClampNative(
                        (values + offsets) * scales,
                        Vector128<float>.Zero, Vector128<float>.One);

                    normalized.StoreUnsafe(ref targetRef, (nuint)(targetIndex + i));
                }
            }
#endif

            for (; i < count; i++)
            {
                var value = (source[sourceIndex + i] + offset) * scale;
                target[targetIndex + i] = MathUtils.Clamp(value, 0f, 1f);
            }
        }

        private static void WriteRowOffset(
            float[] source, int sourceIndex, float[] target, int targetIndex, int count, float offset)
        {
            var i = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var sourceRef = ref MemoryMarshal.GetArrayDataReference(source);
                ref var targetRef = ref MemoryMarshal.GetArrayDataReference(target);

                var offsets = Vector256.Create(offset);

                for (; i + Vector256<float>.Count <= count; i += Vector256<float>.Count)
                {
                    var values = Vector256.LoadUnsafe(ref sourceRef, (nuint)(sourceIndex + i));

                    (values + offsets).StoreUnsafe(ref targetRef, (nuint)(targetIndex + i));
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                ref var sourceRef = ref MemoryMarshal.GetArrayDataReference(source);
                ref var targetRef = ref MemoryMarshal.GetArrayDataReference(target);

                var offsets = Vector128.Create(offset);

                for (; i + Vector128<float>.Count <= count; i += Vector128<float>.Count)
                {
                    var values = Vector128.LoadUnsafe(ref sourceRef, (nuint)(sourceIndex + i));

                    (values + offsets).StoreUnsafe(ref targetRef, (nuint)(targetIndex + i));
                }
            }
#endif

            for (; i < count; i++)
            {
                target[targetIndex + i] = source[sourceIndex + i] + offset;
            }
        }
    }
}
