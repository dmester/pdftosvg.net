// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Functions.PostScript.SyntaxTree
{
    internal sealed class PostScriptSyntaxTree(PostScriptParameterNode[] parameters, PostScriptNode[] outputs)
    {
        public PostScriptParameterNode[] Parameters { get; } = parameters;
        public PostScriptNode[] Outputs { get; } = outputs;
    }
}
