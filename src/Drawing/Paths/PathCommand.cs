using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing.Paths
{
    internal abstract class PathCommand
    {
        public abstract PathCommand Transform(Matrix matrix);
    }
}
