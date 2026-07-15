// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;

namespace PdfToSvg.Imaging.Jpx.Container
{
    internal class JpxContainer
    {
        /// <summary>
        /// The JPEG 2000 code-stream (starting with the SOC marker), regardless of whether the input was a bare
        /// code-stream or a JP2 file.
        /// </summary>
        public ArraySegment<byte> Codestream = ArrayUtils.EmptySegment<byte>();

        /// <summary>
        /// <c>true</c> if the input was a JP2 file (box structure); <c>false</c> if it was a bare code-stream.
        /// </summary>
        public bool IsJp2;

        // The following are only populated when IsJp2 is true; a bare code-stream carries no JP2
        // header boxes and the codestream's own SIZ segment is the sole source of this metadata.

        public JpxImageHeaderBox ImageHeader = new JpxImageHeaderBox();

        public int[] BitsPerComponent = ArrayUtils.Empty<int>();

        public JpxEnumeratedColorSpace EnumeratedColorSpace = JpxEnumeratedColorSpace.Unknown;

        public JpxPaletteBox? Palette;

        public JpxComponentMappingBox[] ComponentMappings = ArrayUtils.Empty<JpxComponentMappingBox>();

        public JpxChannelDefinitionBox[] ChannelDefinitions = ArrayUtils.Empty<JpxChannelDefinitionBox>();
    }
}
