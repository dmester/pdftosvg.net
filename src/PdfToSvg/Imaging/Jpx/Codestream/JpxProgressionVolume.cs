// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Diagnostics;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    // ITU-T T.800 (06/2019) Section A.6.6 and B.12.2 progression order volume. A POC marker segment describes one or
    // more progression order volumes. Each volume bounds the packet progression loops to a resolution, component and
    // layer range, using its own progression order.
    [DebuggerDisplay("R[{ResolutionStart}-{ResolutionEnd}) C[{ComponentStart}-{ComponentEnd}) L[0-{LayerEnd}) {ProgressionOrder}")]
    internal struct JpxProgressionVolume
    {
        /// <summary>RSpoc, inclusive</summary>
        public int ResolutionStart;

        /// <summary>REpoc, exclusive</summary>
        public int ResolutionEnd;

        /// <summary>CSpoc, inclusive</summary>
        public int ComponentStart;

        /// <summary>CEpoc, exclusive</summary>
        public int ComponentEnd;

        /// <summary>LYEpoc, exclusive (layer start is always 0)</summary>
        public int LayerEnd;

        /// <summary>Ppoc</summary>
        public JpxCodingProgressionOrder ProgressionOrder;
    }
}
