// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal static class PdfDictionaryExtensions
    {
        public static bool TryGetRectangle(this PdfDictionary dict, PdfNamePath path, out Rectangle result)
        {
            if (dict.TryGetArray<double>(path, out var array) && array.Length == 4)
            {
                result = new Rectangle(array[0], array[1], array[2], array[3]);
                return true;
            }

            result = new Rectangle();
            return false;
        }
    }
}
