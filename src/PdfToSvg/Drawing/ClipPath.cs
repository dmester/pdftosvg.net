// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Drawing.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg.Drawing
{
    internal class ClipPath
    {
        public readonly string Id;
        public readonly ClipPath? Parent;

        public readonly object ClipElement;

        public readonly bool IsRectangle;
        public readonly Rectangle Rectangle;

        public readonly Dictionary<string, ClipPath> Children = new Dictionary<string, ClipPath>();
        public bool Referenced;

        public ClipPath(ClipPath? parent, string id, object clipElement)
        {
            Parent = parent;
            Id = id;
            ClipElement = clipElement;
        }

        public ClipPath(ClipPath? parent, string id, object clipElement, Rectangle rectangle)
        {
            Parent = parent;
            Id = id;
            ClipElement = clipElement;
            IsRectangle = true;
            Rectangle = rectangle;
        }
    }
}
