// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System.Diagnostics;

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    [DebuggerDisplay("{Index} ({TX0}, {TY0}) ({TX1}, {TY1})")]
    internal class JpxTile
    {
        /// <summary>Tile index in raster order (Isot).</summary>
        public int Index;

        // Tile coordinates on the reference grid.
        // Follows equation B-7 to B-10 in ITU-T T.800 (06/2019).
        public int TX0;
        public int TY0;
        public int TX1;
        public int TY1;

        public JpxTileComponent[] Components = ArrayUtils.Empty<JpxTileComponent>();
    }
}
