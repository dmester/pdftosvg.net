// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace PdfToSvg.Parsing
{
    [Conditional("FOR_SOURCE_GENERATOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal sealed class LexerKeywordAttribute : Attribute
    {
        public LexerKeywordAttribute(string keyword, Token token)
        {
            Keyword = keyword;
            Token = token;
        }

        public string Keyword { get; }
        public Token Token { get; }
    }
}
