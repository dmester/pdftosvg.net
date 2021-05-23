using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing.Paths
{
    internal class MoveToCommand : PathCommand, IMovingCommand
    {
        public MoveToCommand(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }

        public override PathCommand Transform(Matrix matrix)
        {
            var pt = matrix * new Point(X, Y);
            return new MoveToCommand(pt.X, pt.Y);
        }

        public override string ToString()
        {
            return string.Format("M {0:0.####} {1:0.####}", X, Y);
        }
    }
}
