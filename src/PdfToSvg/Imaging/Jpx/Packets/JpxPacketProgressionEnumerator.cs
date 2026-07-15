// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;
using System.Collections.Generic;

namespace PdfToSvg.Imaging.Jpx.Packets
{
    /// <summary>
    /// Computes the order in which the packets of one tile appear in the codestream. The enumeration is driven entirely
    /// by the resolved per-tile parameters — never by the main header defaults, since a tile-part COD may override e.g.
    /// the number of layers.
    /// (ITU-T T.800 (06/2019) Section B.12)
    /// </summary>
    internal static class JpxPacketProgressionEnumerator
    {
        /// <summary>
        /// Returns the packets of <paramref name="tile"/> in codestream order. When the resolved parameters contain
        /// progression order changes, the POC volumes drive the ordering (Section B.12.3); otherwise the COD
        /// progression order is used over the full ranges. <paramref name="components"/> supplies the SIZ sub-sampling
        /// factors needed by the position dependent progression orders (Sections B.12.1.3 to B.12.1.5).
        /// </summary>
        public static IEnumerable<JpxPacketKey> Enumerate(
            JpxTile tile,
            JpxComponent[] components,
            JpxResolvedTileParameters parameters)
        {
            if (components.Length != tile.Components.Length)
            {
                throw new JpxException("The JPEG 2000 component list does not match the tile geometry");
            }

            var volumes = EnumerateProgressionVolumes(tile, parameters);

            // ITU-T T.800 (06/2019) Section B.12.2:
            // No packet is ever repeated in the codestream. A progression volume bounds only the resolution level,
            // component and layer ranges, never precinct positions (Equation B-21), so all precincts of one
            // tile-component resolution level share the same set of already emitted layers. Retaining the next layer
            // per (component, resolution level) is therefore enough to filter out repeated packets.
            if (volumes.Count > 1)
            {
                var nextLayers = new int[tile.Components.Length, GetResolutionCount(tile)];

                foreach (var volume in volumes)
                {
                    foreach (var key in EnumerateVolume(tile, components, volume))
                    {
                        if (key.Layer >= nextLayers[key.Component, key.Resolution])
                        {
                            yield return key;
                        }
                    }

                    // The thresholds must not be raised while the volume is being enumerated: layer 0 of the first
                    // precinct would otherwise filter out layer 0 of the remaining precincts in the same volume.
                    for (var component = volume.ComponentStart; component < volume.ComponentEnd; component++)
                    {
                        for (var resolution = volume.ResolutionStart; resolution < volume.ResolutionEnd; resolution++)
                        {
                            if (nextLayers[component, resolution] < volume.LayerEnd)
                            {
                                nextLayers[component, resolution] = volume.LayerEnd;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var volume in volumes)
                {
                    foreach (var key in EnumerateVolume(tile, components, volume))
                    {
                        yield return key;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the progression order volumes that drive the packet ordering of the tile:
        /// 
        /// <list type="bullet">
        ///    <item>The resolved POC volumes bounded per Equation B-21, or</item>
        ///    <item>A single full volume using the COD progression order when no POC applies</item>
        /// </list>
        ///
        /// (ITU-T T.800 (06/2019) Sections B.12.2, B.12.3)
        /// </summary>
        public static List<JpxProgressionVolume> EnumerateProgressionVolumes(
            JpxTile tile,
            JpxResolvedTileParameters parameters)
        {
            var resolutionCount = GetResolutionCount(tile);
            var componentCount = tile.Components.Length;
            var layerCount = parameters.NumberOfLayers;

            var volumes = new List<JpxProgressionVolume>();
            var changes = parameters.ProgressionOrderChanges;

            if (changes == null || changes.Count == 0)
            {
                // ITU-T T.800 (06/2019) Section B.12.2:
                // Without a POC, the progression loops of B.12.1 all go from zero to the maximum value.
                volumes.Add(new JpxProgressionVolume
                {
                    ResolutionStart = 0,
                    ResolutionEnd = resolutionCount,
                    ComponentStart = 0,
                    ComponentEnd = componentCount,
                    LayerEnd = layerCount,
                    ProgressionOrder = parameters.ProgressionOrder,
                });
                return volumes;
            }

            foreach (var change in changes)
            {
                var volume = new JpxProgressionVolume
                {
                    // ITU-T T.800 (06/2019) Equation B-21:
                    ResolutionStart = Math.Max(change.ResolutionStart, 0),
                    ResolutionEnd = Math.Min(change.ResolutionEnd, resolutionCount),
                    ComponentStart = Math.Max(change.ComponentStart, 0),
                    ComponentEnd = Math.Min(change.ComponentEnd, componentCount),
                    LayerEnd = Math.Min(change.LayerEnd, layerCount),
                    ProgressionOrder = change.ProgressionOrder,
                };

                if (volume.ResolutionStart < volume.ResolutionEnd &&
                    volume.ComponentStart < volume.ComponentEnd &&
                    volume.LayerEnd > 0)
                {
                    volumes.Add(volume);
                }
            }

            return volumes;
        }

        /// <summary>
        /// Nmax + 1, where Nmax is the maximum number of decomposition levels used in any component of the tile
        /// (ITU-T T.800 (06/2019) section B.12.1.1)
        /// </summary>
        private static int GetResolutionCount(JpxTile tile)
        {
            var count = 0;

            foreach (var component in tile.Components)
            {
                if (count < component.ResolutionLevels.Length)
                {
                    count = component.ResolutionLevels.Length;
                }
            }

            return count;
        }

        private static IEnumerable<JpxPacketKey> EnumerateVolume(
            JpxTile tile,
            JpxComponent[] components,
            JpxProgressionVolume volume)
        {
            switch (volume.ProgressionOrder)
            {
                case JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition:
                    return EnumerateLayerResolutionComponentPosition(tile, volume);

                case JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition:
                    return EnumerateResolutionLayerComponentPosition(tile, volume);

                case JpxCodingProgressionOrder.ResolutionLevelPositionComponentLayer:
                    return EnumerateResolutionPositionComponentLayer(tile, components, volume);

                case JpxCodingProgressionOrder.PositionComponentResolutionLevelLayer:
                    return EnumeratePositionComponentResolutionLayer(tile, components, volume);

                case JpxCodingProgressionOrder.ComponentPositionResolutionLevelLayer:
                    return EnumerateComponentPositionResolutionLayer(tile, components, volume);

                default:
                    throw new JpxException(
                        "Unknown JPEG 2000 progression order (" + (int)volume.ProgressionOrder + ").");
            }
        }

        /// <summary>
        /// Returns the precincts of one component at one resolution level, or an empty array when the component does
        /// not have that resolution level or the level is empty
        /// (in both cases there are no packets, ITU-T T.800 (06/2019) Sections B.6 and B.12)
        /// </summary>
        private static JpxPrecinct[] GetPrecincts(JpxTile tile, int component, int resolution)
        {
            var resolutionLevels = tile.Components[component].ResolutionLevels;

            return resolution < resolutionLevels.Length
                ? resolutionLevels[resolution].Precincts
                : ArrayUtils.Empty<JpxPrecinct>();
        }

        // ITU-T T.800 (06/2019) Section B.12.1.1:
        //   for each l = 0, ..., L - 1
        //     for each r = 0, ..., Nmax
        //       for each i = 0, ..., Csiz - 1
        //         for each precinct k in raster order
        //           packet for component i, resolution level r, layer l, precinct k
        private static IEnumerable<JpxPacketKey> EnumerateLayerResolutionComponentPosition(
            JpxTile tile, JpxProgressionVolume volume)
        {
            for (var layer = 0; layer < volume.LayerEnd; layer++)
            {
                for (var resolution = volume.ResolutionStart; resolution < volume.ResolutionEnd; resolution++)
                {
                    for (var component = volume.ComponentStart; component < volume.ComponentEnd; component++)
                    {
                        var precincts = GetPrecincts(tile, component, resolution);
                        for (var precinct = 0; precinct < precincts.Length; precinct++)
                        {
                            yield return new JpxPacketKey(component, resolution, precinct, layer);
                        }
                    }
                }
            }
        }

        // ITU-T T.800 (06/2019) Section B.12.1.2:
        //   for each r = 0, ..., Nmax
        //     for each l = 0, ..., L - 1
        //       for each i = 0, ..., Csiz - 1
        //         for each precinct k in raster order
        //           packet for component i, resolution level r, layer l, precinct k
        private static IEnumerable<JpxPacketKey> EnumerateResolutionLayerComponentPosition(
            JpxTile tile, JpxProgressionVolume volume)
        {
            for (var resolution = volume.ResolutionStart; resolution < volume.ResolutionEnd; resolution++)
            {
                for (var layer = 0; layer < volume.LayerEnd; layer++)
                {
                    for (var component = volume.ComponentStart; component < volume.ComponentEnd; component++)
                    {
                        var precincts = GetPrecincts(tile, component, resolution);
                        for (var precinct = 0; precinct < precincts.Length; precinct++)
                        {
                            yield return new JpxPacketKey(component, resolution, precinct, layer);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The reference grid position at which the position dependent progression loops of
        /// ITU-T T.800 (06/2019) Sections B.12.1.3 to B.12.1.5 emit one precinct.
        /// </summary>
        private struct PrecinctPosition
        {
            public long Y;
            public long X;
            public int Component;
            public int Resolution;
            public int Precinct;
        }

        // The B.12.1.3-B.12.1.5 loops iterate every reference grid point (x, y) of the tile and emit the precinct given
        // by Equation B-20 when
        //
        //   (y mod (YRsiz * 2^(PPy + NL - r)) == 0) or
        //   (y == ty0 and try0 * 2^(NL - r) mod 2^(PPy + NL - r) != 0)
        //
        // and correspondingly for x. The first condition holds exactly at the reference grid projection of each
        // precinct boundary (one resolution level sample covers XRsiz * 2^(NL - r) reference grid columns), and the
        // second condition emits the first precinct row/column at the tile edge when try0/trx0 is not aligned to the
        // precinct grid — in which case its boundary projection lies before the tile edge. Each precinct is therefore
        // emitted at exactly one (x, y) position: its boundary projection clamped to the tile edge. Instead of scanning
        // every grid point (which the NOTE in B.12.1.3 discourages), that position is computed directly for each
        // precinct and the precincts are sorted by it.
        private static void CollectPositions(
            List<PrecinctPosition> positions,
            JpxTile tile,
            JpxComponent[] components,
            int component,
            int resolution)
        {
            var resolutionLevels = tile.Components[component].ResolutionLevels;
            if (resolution >= resolutionLevels.Length)
            {
                return;
            }

            var resolutionLevel = resolutionLevels[resolution];

            // One sample at resolution level r covers 2^(NL - r) tile-component samples, each covering XRsiz x YRsiz
            // reference grid points (ITU-T T.800 (06/2019) Sections B.2 and B.5).
            // 64-bit arithmetic since NL may be up to 32.
            var levelShift = resolutionLevels.Length - 1 - resolution;
            var unitX = (long)components[component].XRsizi << levelShift;
            var unitY = (long)components[component].YRsizi << levelShift;

            foreach (var precinct in resolutionLevel.Precincts)
            {
                positions.Add(new PrecinctPosition
                {
                    Y = Math.Max(tile.TY0, precinct.GridY * (long)resolutionLevel.PrecinctHeight * unitY),
                    X = Math.Max(tile.TX0, precinct.GridX * (long)resolutionLevel.PrecinctWidth * unitX),
                    Component = component,
                    Resolution = resolution,
                    Precinct = precinct.Index,
                });
            }
        }

        // Orders precinct positions the way the scans of ITU-T T.800 (06/2019) Sections B.12.1.3-B.12.1.5 visit them:
        // y outermost, then x, then the component loop, then the resolution loop. Callers that hold some of these fixed
        // are unaffected by the later comparison steps. The key is total (two precincts of the same component and
        // resolution level never share a position), so the unstable List.Sort is deterministic.
        private static int ComparePositions(PrecinctPosition a, PrecinctPosition b)
        {
            if (a.Y != b.Y)
            {
                return a.Y < b.Y ? -1 : 1;
            }

            if (a.X != b.X)
            {
                return a.X < b.X ? -1 : 1;
            }

            if (a.Component != b.Component)
            {
                return a.Component - b.Component;
            }

            if (a.Resolution != b.Resolution)
            {
                return a.Resolution - b.Resolution;
            }

            return a.Precinct - b.Precinct;
        }

        // ITU-T T.800 (06/2019) Section B.12.1.3:
        //   for each r = 0, ..., Nmax
        //     for each y = ty0, ..., ty1 - 1
        //       for each x = tx0, ..., tx1 - 1
        //         for each i = 0, ..., Csiz - 1
        //           if the precinct boundary conditions hold
        //             for the precinct k of Equation B-20
        //               for each l = 0, ..., L - 1
        //                 packet for component i, resolution level r, layer l, precinct k
        private static IEnumerable<JpxPacketKey> EnumerateResolutionPositionComponentLayer(
            JpxTile tile,
            JpxComponent[] components,
            JpxProgressionVolume volume)
        {
            var positions = new List<PrecinctPosition>();

            for (var resolution = volume.ResolutionStart; resolution < volume.ResolutionEnd; resolution++)
            {
                positions.Clear();

                for (var component = volume.ComponentStart; component < volume.ComponentEnd; component++)
                {
                    CollectPositions(positions, tile, components, component, resolution);
                }

                positions.Sort(ComparePositions);

                foreach (var position in positions)
                {
                    for (var layer = 0; layer < volume.LayerEnd; layer++)
                    {
                        yield return new JpxPacketKey(position.Component, resolution, position.Precinct, layer);
                    }
                }
            }
        }

        // ITU-T T.800 (06/2019) Section B.12.1.4:
        //   for each y = ty0, ..., ty1 - 1
        //     for each x = tx0, ..., tx1 - 1
        //       for each i = 0, ..., Csiz - 1
        //         for each r = 0, ..., NL(i)
        //           if the precinct boundary conditions hold
        //             for the precinct k of Equation B-20
        //               for each l = 0, ..., L - 1
        //                 packet for component i, resolution level r, layer l, precinct k
        private static IEnumerable<JpxPacketKey> EnumeratePositionComponentResolutionLayer(
            JpxTile tile,
            JpxComponent[] components,
            JpxProgressionVolume volume)
        {
            var positions = new List<PrecinctPosition>();

            for (var component = volume.ComponentStart; component < volume.ComponentEnd; component++)
            {
                for (var resolution = volume.ResolutionStart; resolution < volume.ResolutionEnd; resolution++)
                {
                    CollectPositions(positions, tile, components, component, resolution);
                }
            }

            positions.Sort(ComparePositions);

            foreach (var position in positions)
            {
                for (var layer = 0; layer < volume.LayerEnd; layer++)
                {
                    yield return new JpxPacketKey(
                        position.Component, position.Resolution, position.Precinct, layer);
                }
            }
        }

        // ITU-T T.800 (06/2019) Section B.12.1.5:
        //   for each i = 0, ..., Csiz - 1
        //     for each y = ty0, ..., ty1 - 1
        //       for each x = tx0, ..., tx1 - 1
        //         for each r = 0, ..., NL(i)
        //           if the precinct boundary conditions hold
        //             for the precinct k of Equation B-20
        //               for each l = 0, ..., L - 1
        //                 packet for component i, resolution level r, layer l, precinct k
        private static IEnumerable<JpxPacketKey> EnumerateComponentPositionResolutionLayer(
            JpxTile tile,
            JpxComponent[] components,
            JpxProgressionVolume volume)
        {
            var positions = new List<PrecinctPosition>();

            for (var component = volume.ComponentStart; component < volume.ComponentEnd; component++)
            {
                positions.Clear();

                for (var resolution = volume.ResolutionStart; resolution < volume.ResolutionEnd; resolution++)
                {
                    CollectPositions(positions, tile, components, component, resolution);
                }

                positions.Sort(ComparePositions);

                foreach (var position in positions)
                {
                    for (var layer = 0; layer < volume.LayerEnd; layer++)
                    {
                        yield return new JpxPacketKey(component, position.Resolution, position.Precinct, layer);
                    }
                }
            }
        }
    }
}
