// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx.Container
{
    internal enum JpxComponentMappingType
    {
        Direct = 0,
        Palette = 1,
    }

    /// <summary>
    /// <c>cmap</c> box
    /// </summary>
    internal class JpxComponentMappingBox
    {
        public int ComponentIndex;
        public JpxComponentMappingType Type;
        public int PaletteColumnIndex;
    }
}
