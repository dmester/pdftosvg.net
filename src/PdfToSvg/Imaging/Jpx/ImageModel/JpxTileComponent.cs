// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Codestream;
using System.Diagnostics;

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    [DebuggerDisplay("({TcX0}, {TcY0}) ({TcX1}, {TcY1})")]
    internal class JpxTileComponent
    {
        public int ComponentIndex;

        // Tile-component coordinates, ITU-T T.800 (06/2019) Equation B-12.
        public int TcX0;
        public int TcY0;
        public int TcX1;
        public int TcY1;

        public int Width => TcX1 - TcX0;
        public int Height => TcY1 - TcY0;

        /// <summary>
        /// Resolution levels r = 0..NL, ITU-T T.800 (06/2019) Section B.5.
        /// </summary>
        public JpxResolutionLevel[] ResolutionLevels = ArrayUtils.Empty<JpxResolutionLevel>();

        public int DecodedResolutionLevelCount;

        public JpxResolutionLevel DecodedResolution => ResolutionLevels[DecodedResolutionLevelCount - 1];

        public required JpxCodingStyleDefaults CodingStyle;

        public required JpxQuantizationDefaults Quantization;

        /// <summary>Resolved RGN max-shift (ITU-T T.800 (06/2019) Annex H), or 0 when no ROI.</summary>
        public int RegionOfInterestShift;
    }
}
