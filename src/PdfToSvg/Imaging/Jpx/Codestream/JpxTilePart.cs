// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    internal class JpxTilePart
    {
        // ITU-T T.800 (06/2019) Section A.4.2 Start of tile-part (SOT)

        /// <summary>Isot</summary>
        public int TileIndex;

        /// <summary>Psot</summary>
        public int TileLength;

        /// <summary>TPsot</summary>
        public int TilePartIndex;

        /// <summary>TNsot</summary>
        public int TilePartCount;
    }
}
