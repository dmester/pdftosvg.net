// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Functions.PostScript.SyntaxTree;
using System.Collections.Generic;

namespace PdfToSvg.Functions.PostScript
{
    internal class PostScriptCompilationContext
    {
        public List<PostScriptCompiledInstruction> Instructions = new();
        public List<PostScriptNode> Registers = new();

        public short AddToRegister(PostScriptNode node)
        {
            for (var i = 0; i < Registers.Count; i++)
            {
                var existingNode = Registers[i];
                if (existingNode.ConsumedReferences >= existingNode.References)
                {
                    Registers[i] = node;
                    return (short)i;
                }
            }

            Registers.Add(node);
            return (short)(Registers.Count - 1);
        }
    }
}
