// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    internal sealed class JpxResolvedTileParameters
    {
        public JpxResolvedTileParameters(
            JpxCodingProgressionOrder progressionOrder,
            int numberOfLayers,
            bool multipleComponentTransformation,
            bool sopMarkerSegments,
            bool ephMarkerSegments,
            List<JpxProgressionVolume>? progressionOrderChanges,
            JpxCodingStyleDefaults[] componentCodingStyles,
            JpxQuantizationDefaults[] componentQuantizations,
            int[] regionOfInterestShifts)
        {
            ProgressionOrder = progressionOrder;
            NumberOfLayers = numberOfLayers;
            MultipleComponentTransformation = multipleComponentTransformation;
            SopMarkerSegments = sopMarkerSegments;
            EphMarkerSegments = ephMarkerSegments;
            ProgressionOrderChanges = progressionOrderChanges;
            ComponentCodingStyles = componentCodingStyles;
            ComponentQuantizations = componentQuantizations;
            RegionOfInterestShifts = regionOfInterestShifts;
        }

        /// <summary>
        /// Progression order from the resolved tile COD. When <see cref="ProgressionOrderChanges"/>
        /// is non-null, the POC volumes override this order (ITU-T T.800 (06/2019) Section B.12.3).
        /// </summary>
        public JpxCodingProgressionOrder ProgressionOrder { get; }

        /// <summary>
        /// Number of layers from the resolved tile COD (never blindly the main header value —
        /// a tile-part COD may override it).
        /// </summary>
        public int NumberOfLayers { get; }

        public bool MultipleComponentTransformation { get; }

        public bool SopMarkerSegments { get; }

        public bool EphMarkerSegments { get; }

        /// <summary>
        /// Resolved progression order changes (tile-part POC if present, otherwise main header
        /// POC), or null when the COD progression order applies.
        /// </summary>
        public List<JpxProgressionVolume>? ProgressionOrderChanges { get; }

        public JpxCodingStyleDefaults[] ComponentCodingStyles { get; }

        public JpxQuantizationDefaults[] ComponentQuantizations { get; }

        /// <summary>Resolved RGN max-shift per component of this tile (0 when no RGN applies).</summary>
        public int[] RegionOfInterestShifts { get; }
    }
}
