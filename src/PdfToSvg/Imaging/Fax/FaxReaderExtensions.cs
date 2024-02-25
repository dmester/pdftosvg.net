// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Fax
{
    internal static class FaxReaderExtensions
    {
        public static bool TryReadCode(this VariableBitReader reader, FaxCodeTable codeTable, out int result)
        {
            var code = 0b1;

            for (var codeLength = 1; codeLength <= codeTable.MaxCodeLength; codeLength++)
            {
                var bit = reader.ReadBit();
                if (bit < 0)
                {
                    break;
                }

                code = (code << 1) | bit;

                if (codeTable.TryGetValue(code, out result))
                {
                    return true;
                }
            }

            result = default!;
            return false;
        }

        public static bool TryReadRunLength(this VariableBitReader reader, FaxCodeTable codeTable, out int runLength)
        {
            runLength = 0;

            while (true)
            {
                if (reader.TryReadCode(codeTable, out var iterationRunLength))
                {
                    runLength += iterationRunLength;

                    if (iterationRunLength <= FaxCodes.MaxTerminatingRunLength)
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
