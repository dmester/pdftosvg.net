// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    /// <summary>
    /// From SIZ marker segment (ITU-T T.800 (06/2019) section A.5.1)
    /// </summary>
    internal class JpxComponent
    {
        public int Ssizi;

        /// <summary>Horizontal sample separation</summary>
        public int XRsizi;

        /// <summary>Vertical sample separation</summary>
        public int YRsizi;

        public int Precision => (Ssizi & 0x7f) + 1;

        public bool Signed => (Ssizi & 0x80) != 0;

        // ITU-T T.800 (06/2019) section G.1.2:
        // Shifting is performed on unsigned components only
        public int DcShift => Signed ? 0 : 1 << (Precision - 1);

        public int MaxValue => (1 << Precision) - 1;
    }
}
