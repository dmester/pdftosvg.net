// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Diagnostics;

namespace PdfToSvg.Functions.PostScript
{
    internal enum PostScriptValueType : byte
    {
        Number,
        Boolean,
        Expression,
    }

    [DebuggerDisplay("{Type} {Number}")]
    internal struct PostScriptValue
    {
        public double Number;
        public PostScriptValueType Type;

        public PostScriptValue(double number)
        {
            Number = number;
            Type = PostScriptValueType.Number;
        }

        public PostScriptValue(bool value)
        {
            Number = value ? 1 : 0;
            Type = PostScriptValueType.Boolean;
        }

        public PostScriptValue(PostScriptValueType type, double value)
        {
            Number = value;
            Type = type;
        }

        public string TypeName
        {
            get
            {
                switch (Type)
                {
                    case PostScriptValueType.Number: return "Number";
                    case PostScriptValueType.Boolean: return "Boolean";
                    case PostScriptValueType.Expression: return "Expression";
                    default: return "Unknown";
                }
            }
        }

    }
}
