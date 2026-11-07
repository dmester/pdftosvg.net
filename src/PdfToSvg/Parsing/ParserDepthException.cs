// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Parsing
{
    internal sealed class ParserDepthException : ParserException
    {
        public ParserDepthException(long position) : base($"Max depth exceeded at position {position}", position)
        {
        }
    }
}
