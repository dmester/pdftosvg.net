// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System.Diagnostics;

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    [DebuggerDisplay("{Level} ({TrX0}, {TrY0}) ({TrX1}, {TrY1})")]
    internal class JpxResolutionLevel
    {
        /// <summary>Resolution level index r, 0..NL.</summary>
        public int Level;

        // Tile-component coordinates at this resolution level,
        // ITU-T T.800 (06/2019) Section B.5 Equation B-14.
        public int TrX0;
        public int TrY0;
        public int TrX1;
        public int TrY1;

        public int Width => TrX1 - TrX0;
        public int Height => TrY1 - TrY0;

        // Precinct partition, ITU-T T.800 (06/2019) Section B.6. The partition is anchored at
        // (0, 0) in the resolution level domain with cells of 2^PPx x 2^PPy samples.
        public int PrecinctWidth;
        public int PrecinctHeight;

        // Grid coordinates of the first precinct cell overlapping this resolution level.
        public int PrecinctGridX0;
        public int PrecinctGridY0;

        // numprecinctswide/numprecinctshigh from ITU-T T.800 (06/2019) Equation B-16.
        // Both are 0 when the resolution level is empty, in which case there are no packets for it (Section B.6).
        public int PrecinctCountX;
        public int PrecinctCountY;

        /// <summary>LL for resolution level = 0, otherwise HL, LH, HH</summary>
        public JpxResolutionLevelSubBand[] SubBands = ArrayUtils.Empty<JpxResolutionLevelSubBand>();

        public JpxPrecinct[] Precincts = ArrayUtils.Empty<JpxPrecinct>();
    }
}
