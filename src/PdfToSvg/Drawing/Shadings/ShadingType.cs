// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Drawing.Shadings
{
    internal enum ShadingType
    {
        Function = 1,
        Axial = 2,
        Radial = 3,
        FreeFormGouraud = 4,
        LatticeFormGouraud = 5,
        CoonsPatchMesh = 6,
        TensorProductPatchMesh = 7,
    }
}
