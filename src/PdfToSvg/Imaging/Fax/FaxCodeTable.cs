// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Fax
{
    internal class FaxCodeTable
    {
        private Dictionary<int, int> codes = new();

        public int MaxCodeLength { get; }

        public FaxCodeTable(int[] definition)
        {
            var maxCode = 0;

            for (var i = 0; i + 1 < definition.Length; i += 2)
            {
                var value = definition[i];
                var code = definition[i + 1];

                codes[code] = value;

                if (maxCode < code)
                {
                    maxCode = code;
                }
            }

            if (maxCode > 1)
            {
                MaxCodeLength = 1;

                while ((maxCode >> MaxCodeLength) != 1)
                {
                    MaxCodeLength++;
                }
            }
        }

        public bool TryGetValue(int code, out int result) => codes.TryGetValue(code, out result);
    }
}
