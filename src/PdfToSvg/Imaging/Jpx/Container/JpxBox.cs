// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.IO;

namespace PdfToSvg.Imaging.Jpx.Container
{
    internal readonly struct JpxBox(JpxBoxType type, int length, JpxDataReader content)
    {
        public readonly JpxBoxType Type = type;
        public readonly int Length = length;
        public readonly JpxDataReader Content = content;
    }
}
