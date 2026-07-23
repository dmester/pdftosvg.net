// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Functions.PostScript
{
    internal class PostScriptStackOverflowException : PostScriptFunctionException
    {
        public PostScriptStackOverflowException() : base("The PostScript operand stack is full.")
        {
        }
    }
}
