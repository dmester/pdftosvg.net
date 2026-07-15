// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Container;
using System;

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    internal class JpxImageInfo
    {
        public JpxImageHeaderBox ImageHeader = new JpxImageHeaderBox();

        public int[] BitsPerComponent = ArrayUtils.Empty<int>();

        public JpxEnumeratedColorSpace EnumeratedColorSpace = JpxEnumeratedColorSpace.Unknown;

        public JpxChannelDefinitionBox[] ChannelDefinitions = ArrayUtils.Empty<JpxChannelDefinitionBox>();

        public JpxPaletteBox? Palette;

        public JpxComponentMappingBox[] ComponentMappings = ArrayUtils.Empty<JpxComponentMappingBox>();

        // From SIZ marker segment (ITU-T T.800 (06/2019) section A.5.1):
        public int Rsiz;
        public int Xsiz;
        public int Ysiz;
        public int XOsiz;
        public int YOsiz;
        public int XTsiz;
        public int YTsiz;
        public int XTOsiz;
        public int YTOsiz;
        public JpxComponent[] Components = ArrayUtils.Empty<JpxComponent>();

        // ITU-T T.800 (06/2019) Section B.3 tile-component division
        public int NumXTiles => (Xsiz - XTOsiz - 1) / XTsiz + 1;
        public int NumYTiles => (Ysiz - YTOsiz - 1) / YTsiz + 1;

        public ArraySegment<byte> Codestream = ArrayUtils.EmptySegment<byte>();
    }
}
