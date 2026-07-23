// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PdfToSvg.Functions.PostScript
{
    [DebuggerDisplay("{DebugView,nq}")]
    internal readonly struct PostScriptCompiledInstruction
    {
        // Operator
        public readonly PostScriptOpCode OpCode;

        // Destination
        public readonly short DestinationRegister;

        // Operands
        public readonly short SourceRegister1;
        public readonly short SourceRegister2;
        public readonly double Immediate;

        public PostScriptCompiledInstruction(
            PostScriptOpCode opCode,
            short destinationRegister,
            short sourceRegister1,
            short sourceRegister2,
            double immediate)
        {
            DestinationRegister = destinationRegister;
            SourceRegister1 = sourceRegister1;
            SourceRegister2 = sourceRegister2;
            OpCode = opCode;
            Immediate = immediate;
        }

        private string DebugView
        {
            get
            {
                var result = "r" + DestinationRegister + " ← " + OpCode;

                var argCount = 0;

                switch (OpCode)
                {
                    case PostScriptOpCode.PushValue:
                    case PostScriptOpCode.Abs:
                    case PostScriptOpCode.Ceiling:
                    case PostScriptOpCode.Floor:
                    case PostScriptOpCode.Round:
                    case PostScriptOpCode.Truncate:
                    case PostScriptOpCode.Sqrt:
                    case PostScriptOpCode.Sin:
                    case PostScriptOpCode.Cos:
                    case PostScriptOpCode.Ln:
                    case PostScriptOpCode.Log:
                    case PostScriptOpCode.Cvi:
                    case PostScriptOpCode.Cvr:
                    case PostScriptOpCode.Neg:
                    case PostScriptOpCode.LogicalNot:
                    case PostScriptOpCode.BitwiseNot:
                        argCount = 1;
                        break;

                    case PostScriptOpCode.Add:
                    case PostScriptOpCode.Sub:
                    case PostScriptOpCode.Mul:
                    case PostScriptOpCode.Div:
                    case PostScriptOpCode.Idiv:
                    case PostScriptOpCode.Mod:
                    case PostScriptOpCode.Atan:
                    case PostScriptOpCode.Exp:
                    case PostScriptOpCode.Eq:
                    case PostScriptOpCode.Ne:
                    case PostScriptOpCode.Gt:
                    case PostScriptOpCode.Ge:
                    case PostScriptOpCode.Lt:
                    case PostScriptOpCode.Le:
                    case PostScriptOpCode.LogicalAnd:
                    case PostScriptOpCode.BitwiseAnd:
                    case PostScriptOpCode.LogicalOr:
                    case PostScriptOpCode.BitwiseOr:
                    case PostScriptOpCode.LogicalXor:
                    case PostScriptOpCode.BitwiseXor:
                    case PostScriptOpCode.BitShift:
                        argCount = 2;
                        break;
                }

                if (argCount > 0)
                {
                    if (SourceRegister1 < 0)
                    {
                        result += " " + Immediate.ToString("0.0", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        result += " r" + SourceRegister1;
                    }

                    if (argCount > 1)
                    {
                        if (SourceRegister2 < 0)
                        {
                            result += " " + Immediate.ToString("0.0", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            result += " r" + SourceRegister2;
                        }
                    }
                }

                return result;
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is PostScriptCompiledInstruction instruction &&
                instruction.OpCode == OpCode &&
                instruction.Immediate == Immediate &&
                instruction.DestinationRegister == DestinationRegister &&
                instruction.SourceRegister1 == SourceRegister1 &&
                instruction.SourceRegister2 == SourceRegister2;
        }

        public override int GetHashCode()
        {
            return
                (DestinationRegister << 24) ^
                (SourceRegister1 << 16) ^
                (SourceRegister2 << 8) ^
                ((int)OpCode);
        }
    }
}
