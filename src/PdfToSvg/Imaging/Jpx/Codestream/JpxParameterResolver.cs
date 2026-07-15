// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    /// <summary>
    /// Implemented precedence rules (ITU-T T.800 (06/2019)):
    /// <list type="bullet">
    ///    <item>Coding style (Sections A.6.1/A.6.2):  Tile-part COC &gt; Tile-part COD &gt; Main COC &gt; Main COD</item>
    ///    <item>Quantization (Sections A.6.4/A.6.5):  Tile-part QCC &gt; Tile-part QCD &gt; Main QCC &gt; Main QCD</item>
    ///    <item>Progression (Sections A.6.6/B.12.3):  Tile-part POC &gt; Main POC &gt; Tile-part COD &gt; Main COD</item>
    ///    <item>Region of interest (Section A.6.3):   Tile-part RGN &gt; Main RGN</item>
    /// </list>
    /// </summary>
    internal static class JpxParameterResolver
    {
        /// <summary>
        /// Resolves the effective per-tile, per-component parameters from the main header markers and the markers
        /// accumulated from the tile's tile-part headers.
        /// </summary>
        /// <param name="main">Markers from the main header.</param>
        /// <param name="tilePart">
        /// Markers accumulated from the tile-part headers of the tile being resolved, or null when no tile-part header
        /// has been read yet (e.g. resolving defaults during phase 1).
        /// </param>
        /// <param name="componentCount">Csiz from the SIZ marker segment.</param>
        public static JpxResolvedTileParameters Resolve(
            JpxHeaderParameters main,
            JpxHeaderParameters? tilePart,
            int componentCount)
        {
            if (componentCount < 1)
            {
                throw new JpxException("Invalid component count");
            }

            // ITU-T T.800 (06/2019) Section A.6.1: there shall be one and only one COD in the main header.
            // Likewise Section A.6.4 for QCD.
            var mainCod = main.CodingStyleDefaults ??
                throw new JpxException("Missing COD marker in JPEG 2000 main header");
            var mainQcd = main.QuantizationDefaults ??
                throw new JpxException("Missing QCD marker in JPEG 2000 main header");

            // ITU-T T.800 (06/2019) Section A.6.1: a COD in the tile-part header overrides the main header COD
            // (and main header COCs) for all components of this tile.
            var tileCod = tilePart?.CodingStyleDefaults;
            var effectiveCod = tileCod ?? mainCod;

            // ITU-T T.800 (06/2019) Section A.6.4: a QCD in the tile-part header overrides the main header QCD
            // (and main header QCCs) for all components of this tile.
            var tileQcd = tilePart?.QuantizationDefaults;

            var componentCodingStyles = new JpxCodingStyleDefaults[componentCount];
            var componentQuantizations = new JpxQuantizationDefaults[componentCount];
            var regionOfInterestShifts = new int[componentCount];

            for (var c = 0; c < componentCount; c++)
            {
                // ITU-T T.800 (06/2019) Section A.6.2:
                // Tile-part COC > Tile-part COD > Main COC > Main COD.
                // A main header COC survives only when the tile has neither COC nor COD.
                JpxCodingStyleComponent? coc;
                if (tilePart == null || !tilePart.ComponentCodingStyles.TryGetValue(c, out coc))
                {
                    if (tileCod != null || !main.ComponentCodingStyles.TryGetValue(c, out coc))
                    {
                        coc = null;
                    }
                }

                componentCodingStyles[c] = coc == null ? effectiveCod.Clone() : Overlay(effectiveCod, coc);

                // ITU-T T.800 (06/2019) Section A.6.5:
                // Tile-part QCC > Tile-part QCD > Main QCC > Main QCD.
                // A QCC is a complete quantization specification, so no overlay is needed.
                JpxQuantizationDefaults? quantization;
                if (tilePart == null || !tilePart.ComponentQuantizations.TryGetValue(c, out quantization))
                {
                    quantization = tileQcd;

                    if (quantization == null && !main.ComponentQuantizations.TryGetValue(c, out quantization))
                    {
                        quantization = mainQcd;
                    }
                }

                componentQuantizations[c] = quantization.Clone();

                // ITU-T T.800 (06/2019) Section A.6.3: an RGN in a tile-part header overrides a
                // main header RGN for that component in this tile.
                int maxShift;
                if (tilePart == null || !tilePart.RegionOfInterestShifts.TryGetValue(c, out maxShift))
                {
                    if (!main.RegionOfInterestShifts.TryGetValue(c, out maxShift))
                    {
                        maxShift = 0;
                    }
                }

                regionOfInterestShifts[c] = maxShift;
            }

            // ITU-T T.800 (06/2019) Sections A.6.6 and B.12.3:
            // Tile-part POC > Main POC > Tile-part COD > Main COD.
            var poc = tilePart?.ProgressionOrderChanges ?? main.ProgressionOrderChanges;

            List<JpxProgressionVolume>? progressionOrderChanges = null;
            if (poc != null && poc.Count > 0)
            {
                progressionOrderChanges = new List<JpxProgressionVolume>(poc.Count);

                foreach (var volume in poc)
                {
                    // A main header POC may reference more layers than the resolved tile COD provides (a tile-part COD
                    // may lower the layer count). Layer indices beyond the actual layers cannot occur in the packet
                    // sequence (ITU-T T.800 (06/2019) Section B.12.2), so bound LYEpoc by the resolved layer count.
                    var boundedVolume = volume;
                    if (boundedVolume.LayerEnd > effectiveCod.NumberOfLayers)
                    {
                        boundedVolume.LayerEnd = effectiveCod.NumberOfLayers;
                    }

                    progressionOrderChanges.Add(boundedVolume);
                }
            }

            return new JpxResolvedTileParameters(
                progressionOrder: effectiveCod.ProgressionOrder,
                numberOfLayers: effectiveCod.NumberOfLayers,
                multipleComponentTransformation: effectiveCod.MultipleComponentTransformation,
                sopMarkerSegments: effectiveCod.CodingStyle.SopMarkerSegments,
                ephMarkerSegments: effectiveCod.CodingStyle.EphMarkerSegments,
                progressionOrderChanges: progressionOrderChanges,
                componentCodingStyles: componentCodingStyles,
                componentQuantizations: componentQuantizations,
                regionOfInterestShifts: regionOfInterestShifts);
        }

        /// <summary>
        /// Returns a copy of <paramref name="baseStyle"/> with the component-scoped parameters
        /// replaced by the COC values, per ITU-T T.800 (06/2019) Section A.6.2. The tile-scoped
        /// SGcod parameters (progression order, layers, MCT) and the SOP/EPH flags are kept from
        /// the base COD, since a COC cannot carry them.
        /// </summary>
        public static JpxCodingStyleDefaults Overlay(
            JpxCodingStyleDefaults baseStyle,
            JpxCodingStyleComponent componentStyle)
        {
            var result = baseStyle.Clone();
            componentStyle.CopyTo(result);
            return result;
        }
    }
}
