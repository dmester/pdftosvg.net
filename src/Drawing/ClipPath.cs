using PdfToSvg.Drawing.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class ClipPath
    {
        public string Id;
        public ClipPath Parent;
        public PathData Data;
        public bool IsRectangle;
        public Rectangle Rectangle;
        public bool Referenced;
        public bool EvenOdd;
        public Dictionary<string, ClipPath> Children = new Dictionary<string, ClipPath>();
    }
}
