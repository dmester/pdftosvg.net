using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing.Paths
{
    internal class LineToCommand : PathCommand, IMovingCommand
    {
        public LineToCommand(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }

        public override PathCommand Transform(Matrix matrix)
        {
            var pt = matrix * new Point(X, Y);
            return new LineToCommand(pt.X, pt.Y);
        }

        public override string ToString()
        {
            return string.Format("L {0:0.####} {1:0.####}", X, Y);
        }
    }
}
