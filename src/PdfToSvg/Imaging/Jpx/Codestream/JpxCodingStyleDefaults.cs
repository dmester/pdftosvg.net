// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    /// <summary>
    /// Coding style default (COD)
    /// </summary>
    internal class JpxCodingStyleDefaults
    {
        // ITU-T T.800 (06/2019) Section A.6.1 Coding style default (COD)
        public JpxCodingStyles CodingStyle;

        public JpxCodingProgressionOrder ProgressionOrder;
        public int NumberOfLayers;
        public bool MultipleComponentTransformation;
        public int NumberOfDecompositionLevels;
        public int CodeBlockWidth;
        public int CodeBlockHeight;
        public JpxCodeBlockStyle CodeBlockStyle;
        public bool ReversibleFilter;
        public JpxPrecinctSize[] PrecinctSize = ArrayUtils.Empty<JpxPrecinctSize>();

        public JpxCodingStyleDefaults Clone()
        {
            var clone = (JpxCodingStyleDefaults)MemberwiseClone();
            clone.PrecinctSize = (JpxPrecinctSize[])PrecinctSize.Clone();
            return clone;
        }
    }
}
