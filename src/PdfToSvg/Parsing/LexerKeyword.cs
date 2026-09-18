// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Parsing
{
    internal readonly struct LexerKeyword(string keyword, Token token)
    {
        public readonly string Keyword = keyword;
        public readonly Token Token = token;
    }
}
