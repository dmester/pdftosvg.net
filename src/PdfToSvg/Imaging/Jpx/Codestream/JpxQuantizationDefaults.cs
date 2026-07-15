// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    internal class JpxQuantizationDefaults
    {
        public bool ScalarDerived;
        public bool ScalarExpounded;
        public bool ComponentSpecific;
        public int NumberOfGuardBits;
        public JpxQuantizationDefaultValues[] Values = ArrayUtils.Empty<JpxQuantizationDefaultValues>();

        public JpxQuantizationDefaults Clone()
        {
            var clone = (JpxQuantizationDefaults)MemberwiseClone();
            clone.Values = (JpxQuantizationDefaultValues[])Values.Clone();
            return clone;
        }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal struct JpxQuantizationDefaultValues
    {
        public int Exponent;
        public int Mantissa;
        public string DebuggerDisplay => $"Mantissa µb={Mantissa}, Exponent, εb={Exponent}";
    }
}
