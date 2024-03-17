// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class TextSpan
    {
        public StringBuilder Value = new();
        public double Width;
        public List<double> SpaceBefore = new();
        public GraphicsState Style;

        public TextSpan(GraphicsState style)
        {
            Style = style;
        }

        public void AddText(double spaceBefore, string text, double width)
        {
            AddSpace(spaceBefore);
            Value.Append(text);
            Width += spaceBefore + width;
        }

        private void AddSpace(double space)
        {
            if (space < 0.01 && space > -0.01)
            {
                return;
            }

            while (SpaceBefore.Count < Value.Length)
            {
                SpaceBefore.Add(0);
            }

            SpaceBefore.Add(space);
        }
    }
}
