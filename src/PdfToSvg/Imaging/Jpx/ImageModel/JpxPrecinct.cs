// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Packets;
using System.Diagnostics;

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    [DebuggerDisplay("{Index} ({X0}, {Y0}) ({X1}, {Y1})")]
    internal class JpxPrecinct
    {
        /// <summary>Precinct index in raster order within the resolution level.</summary>
        public int Index;

        // Position of the precinct in the precinct grid anchored at (0, 0).
        public int GridX;
        public int GridY;

        // Precinct area in resolution level coordinates, clipped to the resolution level area.
        public int X0;
        public int Y0;
        public int X1;
        public int Y1;

        /// <summary>Per sub-band code-block ranges, parallel to <see cref="JpxResolutionLevel.SubBands"/>.</summary>
        public JpxPrecinctSubBand[] SubBands = ArrayUtils.Empty<JpxPrecinctSubBand>();
    }
}
