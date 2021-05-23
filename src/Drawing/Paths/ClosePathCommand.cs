using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing.Paths
{
    internal class ClosePathCommand : PathCommand
    {
        private ClosePathCommand() { }

        public static ClosePathCommand Value { get; } = new ClosePathCommand();

        public override PathCommand Transform(Matrix matrix) { return Value; }

        public override string ToString()
        {
            return "z";
        }
    }
}
