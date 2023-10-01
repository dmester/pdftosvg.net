// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace PdfToSvg.Drawing.Patterns
{
    internal abstract class Pattern
    {
        public PdfDictionary Definition { get; }
        public Matrix Matrix { get; }

        public Pattern(PdfDictionary definition, CancellationToken cancellationToken)
        {
            Definition = definition;
            Matrix = definition.GetValueOrDefault(Names.Matrix, Matrix.Identity);
        }

        public static Pattern? Create(PdfDictionary definition, CancellationToken cancellationToken)
        {
            var patternType = (PatternType)definition.GetValueOrDefault(Names.PatternType, 0);

            switch (patternType)
            {
                case PatternType.Shading:
                    return new ShadingPattern(definition, cancellationToken);

                case PatternType.Tiling:
                    return new TilingPattern(definition, cancellationToken);

                default:
                    Log.WriteLine("Unsupported pattern type " + patternType + ".");
                    return null;
            }
        }
    }
}
