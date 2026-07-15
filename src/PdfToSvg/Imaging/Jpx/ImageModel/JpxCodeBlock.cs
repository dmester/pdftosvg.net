// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.Coding;
using System.Diagnostics;

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    [DebuggerDisplay("({X0}, {Y0}) ({X1}, {Y1})")]
    internal sealed class JpxCodeBlock
    {
        public JpxCodeBlock(
            JpxResolutionLevelSubBand subBand,
            int gridX,
            int gridY,
            int x0,
            int y0,
            int x1,
            int y1,
            int localX0,
            int localY0)
        {
            SubBand = subBand;
            GridX = gridX;
            GridY = gridY;
            X0 = x0;
            Y0 = y0;
            X1 = x1;
            Y1 = y1;
            LocalX0 = localX0;
            LocalY0 = localY0;
        }

        #region Immutable geometry info

        public JpxResolutionLevelSubBand SubBand { get; }

        // Position in the code-block grid anchored at (0, 0).
        public int GridX { get; }
        public int GridY { get; }

        // Code-block area in sub-band coordinates, clipped to the sub-band area
        // (ITU-T T.800 (06/2019) Section B.7 NOTE).
        public int X0 { get; }
        public int Y0 { get; }
        public int X1 { get; }
        public int Y1 { get; }

        public int Width => X1 - X0;
        public int Height => Y1 - Y0;

        // Position of the code-block's upper left coefficient within the tile-component's split
        // sample buffer layout (see JpxTileComponent).
        public int LocalX0 { get; }
        public int LocalY0 { get; }

        public JpxCodeBlockStyle CodeBlockStyle => SubBand.CodeBlockStyle;

        #endregion

        #region Mutable state

        /// <summary>
        /// Whether the code-block has been included in any packet so far.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Section B.10.4
        /// </remarks>
        public bool Included;

        /// <summary>
        /// Number of missing (all zero) most significant bit-planes signalled when the code-block
        /// was first included, or -1 when not yet signalled.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Section B.10.5
        /// </remarks>
        public int ZeroBitPlanes = -1;

        /// <summary>
        /// Current code-block length indicator state.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Section B.10.7.1
        /// </remarks>
        public int Lblock = 3;

        /// <summary>
        /// Cumulative number of coding passes read from packet headers so far.
        /// </summary>
        public int CodingPasses;

        /// <summary>
        /// Codeword segments accumulated from packet bodies.
        /// </summary>
        public JpxInlineList<JpxCodeBlockSegment> Segments;

        #endregion
    }
}
