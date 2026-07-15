// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.Packets;

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    internal struct JpxPrecinctSubBand
    {
        public int CodeBlockX0;
        public int CodeBlockY0;
        public int CodeBlockX1;
        public int CodeBlockY1;

        // State spans all quality layers of this precinct sub-band (ITU-T T.800 (06/2019) Sections B.10.4/B.10.5).
        // Kept null until the first non-empty packet visits the sub-band.
        public JpxTagTree? InclusionTree;
        public JpxTagTree? ZeroBitPlanesTree;

        public int CodeBlockCountX => CodeBlockX1 > CodeBlockX0 ? CodeBlockX1 - CodeBlockX0 : 0;
        public int CodeBlockCountY => CodeBlockY1 > CodeBlockY0 ? CodeBlockY1 - CodeBlockY0 : 0;
    }
}
