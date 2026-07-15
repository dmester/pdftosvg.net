// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx.Container
{
    /// <summary>
    /// <c>cdef</c> box
    /// </summary>
    internal class JpxChannelDefinitionBox
    {
        public int ChannelIndex;
        public JpxChannelType Type;
        public int Association;
    }
}
