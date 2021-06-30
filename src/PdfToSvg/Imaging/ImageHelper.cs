// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal static class ImageHelper
    {
        public static bool HasCustomDecodeArray(PdfDictionary imageDictionary, ColorSpace colorSpace)
        {
            if (imageDictionary.TryGetArray<double>(Names.Decode, out var decodeValues))
            {
                var bitsPerComponent = imageDictionary.GetValueOrDefault(Names.BitsPerComponent, 8);

                var decodeArray = new DecodeArray(bitsPerComponent, decodeValues);
                var defaultDecodeArray = colorSpace.GetDefaultDecodeArray(bitsPerComponent);

                return !decodeArray.Equals(defaultDecodeArray);
            }

            return false;
        }

        public static DecodeArray GetDecodeArray(PdfDictionary imageDictionary, ColorSpace colorSpace)
        {
            DecodeArray result;

            var bitsPerComponent = imageDictionary.GetValueOrDefault(Names.BitsPerComponent, 8);

            if (imageDictionary.TryGetArray<double>(Names.Decode, out var decodeValues))
            {
                result = new DecodeArray(bitsPerComponent, decodeValues);
            }
            else
            {
                result = colorSpace.GetDefaultDecodeArray(bitsPerComponent);
            }

            return result;
        }
    }
}
