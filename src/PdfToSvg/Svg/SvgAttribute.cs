// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Svg
{
    internal readonly struct SvgAttribute(string name, string value, bool isRaw)
    {
        public readonly string Name = name;
        public readonly string Value = value;
        public readonly bool IsRaw = isRaw;
    }
}
