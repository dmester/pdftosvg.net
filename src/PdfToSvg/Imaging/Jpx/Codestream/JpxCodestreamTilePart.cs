// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    internal sealed class JpxCodestreamTilePart
    {
        // SOT fields:

        /// <summary>Isot: index of the tile this tile-part belongs to, in raster order.</summary>
        public int TileIndex;

        /// <summary>TPsot: index of this tile-part within its tile, as declared in the SOT.</summary>
        public int TilePartIndex;

        /// <summary>TNsot: declared number of tile-parts of this tile, or 0 when unspecified.</summary>
        public int TilePartCount;

        /// <summary>
        /// Whether this was the first tile-part encountered for its tile. Functional marker
        /// segments (COD/COC/QCD/QCC/RGN/POC) are only accepted from the first tile-part
        /// (ITU-T T.800 (06/2019) Sections A.6.1–A.6.6).
        /// </summary>
        public bool IsFirstTilePart;

        /// <summary>Packet data following the SOD marker.</summary>
        public ArraySegment<byte> Data = ArrayUtils.EmptySegment<byte>();
    }
}
