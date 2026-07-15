// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx.Container
{
    /// <summary>
    /// <c>ihdr</c> box
    /// </summary>
    internal class JpxImageHeaderBox
    {
        public int Width;
        public int Height;
        public int NumberOfComponents;
        public int BitsPerComponent;
        public bool UnknownColorSpace;
        public bool IntellectualProperty;
    }
}
