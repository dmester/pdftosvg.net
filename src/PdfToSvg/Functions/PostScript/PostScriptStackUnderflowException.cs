// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Functions.PostScript
{
    internal class PostScriptStackUnderflowException : PostScriptFunctionException
    {
        public PostScriptStackUnderflowException() : base("Attempt to get values from an empty stack in a PostScript function.")
        {
        }
    }
}
