// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Functions.PostScript
{
    internal class PostScriptExpression
    {
        private readonly IList<PostScriptInstruction> instructions;

        public PostScriptExpression(IList<PostScriptInstruction> instructions)
        {
            this.instructions = instructions;
        }

        public void Execute(PostScriptStack stack)
        {
            foreach (var instruction in instructions)
            {
                instruction(stack);
            }
        }
    }
}
