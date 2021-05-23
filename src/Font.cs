using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    public abstract class Font
    {
        // Don't allow external implementations
        internal Font() { }

        public abstract string FontFamily { get; }

        public override int GetHashCode() => FontFamily?.GetHashCode() ?? 0;

        public override string ToString() => FontFamily;
    }
}
