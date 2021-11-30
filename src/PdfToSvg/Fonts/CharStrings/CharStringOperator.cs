// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Parser = PdfToSvg.Fonts.CharStrings.Type2CharStringParser;

namespace PdfToSvg.Fonts.CharStrings
{
    /// <summary>
    /// Holds information about a char string operator.
    /// </summary>
    internal class CharStringOperator
    {
        private readonly Action<Parser> implementation;

        public CharStringOperator(Action<Parser> implementation, bool clearStack, bool subrOperator)
        {
            this.implementation = implementation;
            ClearStack = clearStack;
            SubrOperator = subrOperator;
        }

        /// <summary>
        /// Specifies whether the operator is clearing the stack. Used for detecting the leading advance width in char
        /// strings.
        /// </summary>
        public bool ClearStack { get; }

        /// <summary>
        /// Specifies whether this operator invokes or returning from a subroutine. Such operators don't affect
        /// <see cref="Parser.LastOperator"/>.
        /// </summary>
        public bool SubrOperator { get; }

        /// <summary>
        /// Invokes the operator.
        /// </summary>
        [DebuggerStepThrough]
        public void Invoke(Parser parser) => implementation.Invoke(parser);

        public static bool operator ==(CharStringOperator? op, Action<Parser>? other) => op?.implementation == other;
        public static bool operator !=(CharStringOperator? op, Action<Parser>? other) => op?.implementation != other;

        public override bool Equals(object obj)
        {
            if (obj is Action<Parser> impl)
            {
                return impl == implementation;
            }

            if (obj is CharStringOperator op)
            {
                return
                    op.implementation == implementation &&
                    op.ClearStack == ClearStack &&
                    op.SubrOperator == SubrOperator;
            }

            return false;
        }

        public override int GetHashCode() => implementation.GetHashCode();

        public override string ToString() => implementation.GetMethodInfo().Name;
    }
}
