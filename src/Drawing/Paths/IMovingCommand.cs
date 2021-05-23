using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing.Paths
{
    internal interface IMovingCommand
    {
        double X { get; }
        double Y { get; }
    }
}
