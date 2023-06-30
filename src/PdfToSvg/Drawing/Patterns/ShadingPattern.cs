// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing.Shadings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace PdfToSvg.Drawing.Patterns
{
    internal class ShadingPattern : Pattern
    {
        public Shading? Shading { get; }

        public ShadingPattern(PdfDictionary definition, CancellationToken cancellationToken) : base(definition, cancellationToken)
        {
            if (definition.TryGetDictionary(Names.Shading, out var shadingDict))
            {
                Shading = Shading.Create(shadingDict, cancellationToken);
            }
        }

        public override XElement? GetPatternElement(Matrix transform)
        {
            return Shading?.GetShadingElement(Matrix * transform, inPattern: true);
        }
    }
}
