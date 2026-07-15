// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Codestream;
using System.Diagnostics;

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    [DebuggerDisplay("{Type} ({TbX0}, {TbY0}) ({TbX1}, {TbY1})")]
    internal class JpxResolutionLevelSubBand
    {
        public JpxSubBandType Type;

        /// <summary>Decomposition level n_b of the sub-band (ITU-T T.800 (06/2019) Section B.5).</summary>
        public int DecompositionLevel;

        // Sub-band coordinates, ITU-T T.800 (06/2019) Equation B-15.
        public int TbX0;
        public int TbY0;
        public int TbX1;
        public int TbY1;

        public int Width => TbX1 - TbX0;
        public int Height => TbY1 - TbY0;

        // Position of the sub-band's upper left coefficient within the tile-component's split
        // sample buffer layout (low-pass coefficients first; see JpxTileComponent).
        public int LocalX0;
        public int LocalY0;

        /// <summary>Resolved quantization exponent for this sub-band (epsilon_b, ITU-T T.800 (06/2019) Section E.1).</summary>
        public int Exponent;

        /// <summary>Resolved quantization mantissa for this sub-band (mu_b, ITU-T T.800 (06/2019) Section E.1).</summary>
        public int Mantissa;

        /// <summary>
        /// Number of magnitude bit-planes Mb = G + epsilon_b - 1
        /// (ITU-T T.800 (06/2019) Equation E-2).
        /// </summary>
        public int MagnitudeBitPlanes;

        public JpxCodeBlockStyle CodeBlockStyle;

        // Code-block partition. The partition is anchored at (0, 0) in the sub-band domain with cells of
        // 2^xcb' x 2^ycb' coefficients.
        // (Equations B-17 and B-18).
        public int CodeBlockWidth;
        public int CodeBlockHeight;

        // Grid coordinates of the first code-block cell overlapping this sub-band.
        public int CodeBlockGridX0;
        public int CodeBlockGridY0;

        public int CodeBlockCountX;
        public int CodeBlockCountY;

        /// <summary>
        /// Code-blocks in raster order over the grid range
        /// [<see cref="CodeBlockGridX0"/>, <see cref="CodeBlockGridX0"/> + <see cref="CodeBlockCountX"/>) x
        /// [<see cref="CodeBlockGridY0"/>, <see cref="CodeBlockGridY0"/> + <see cref="CodeBlockCountY"/>),
        /// clipped to the sub-band area.
        /// </summary>
        public JpxCodeBlock[] CodeBlocks = ArrayUtils.Empty<JpxCodeBlock>();
    }
}
