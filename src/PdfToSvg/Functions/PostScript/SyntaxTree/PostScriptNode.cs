// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Diagnostics;

namespace PdfToSvg.Functions.PostScript.SyntaxTree
{
    [DebuggerDisplay("{DebugView,nq}")]
    internal abstract class PostScriptNode
    {
        public abstract string DebugView { get; }

        public abstract PostScriptValueType Type { get; }

        /// <summary>
        /// If this node can mathematically have an infinite result. Simple arithmetics like multiplication will report
        /// false on finite input, even if they can technically result in <see cref="float.PositiveInfinity"/>.
        /// </summary>
        public abstract bool PotentiallyInfinite { get; }

        public int References { get; protected set; }
        public int ConsumedReferences { get; private set; }
        public short Register { get; protected set; } = -1;

        public abstract PostScriptNode Simplify(PostScriptSyntaxBuilderContext context);
        public abstract bool TryCompile(PostScriptCompilationContext context, out short register);
        public abstract void MarkReferenced();
        public void Consume() => ConsumedReferences++;
    }
}
