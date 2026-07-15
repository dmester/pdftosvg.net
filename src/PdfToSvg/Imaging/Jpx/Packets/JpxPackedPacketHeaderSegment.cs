// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;

namespace PdfToSvg.Imaging.Jpx.Packets
{
    internal sealed class JpxPackedPacketHeaderSegment
    {
        /// <summary>Zppm</summary>
        public int Index;
        public ArraySegment<byte> Data = ArrayUtils.EmptySegment<byte>();
    }
}
