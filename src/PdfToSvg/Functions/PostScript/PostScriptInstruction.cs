// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Functions.PostScript
{
    [DebuggerDisplay("{OpCode} {Operand}")]
    internal readonly struct PostScriptInstruction
    {
        public readonly PostScriptOpCode OpCode;
        public readonly PostScriptValue Operand;

        public PostScriptInstruction(PostScriptOpCode opCode)
        {
            OpCode = opCode;
            Operand = default;
        }

        public PostScriptInstruction(PostScriptOpCode opCode, in PostScriptValue operand)
        {
            OpCode = opCode;
            Operand = operand;
        }
    }
}
