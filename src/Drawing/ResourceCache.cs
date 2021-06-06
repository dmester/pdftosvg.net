// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class ResourceCache
    {
        private readonly Dictionary<PdfName, InternalFont?> fonts = new Dictionary<PdfName, InternalFont?>();
        private readonly Dictionary<PdfName, ColorSpace?> colorSpaces = new Dictionary<PdfName, ColorSpace?>();

        public ResourceCache(PdfDictionary resourcesDict)
        {
            Dictionary = resourcesDict;
        }

        public PdfDictionary Dictionary { get; }

        public InternalFont? GetFont(PdfName fontName, IFontResolver fontResolver)
        {
            if (!fonts.TryGetValue(fontName, out var font))
            {
                if (Dictionary.TryGetDictionary(Names.Font / fontName, out var fontDict))
                {
                    font = new InternalFont(fontDict, fontResolver);
                }

                fonts[fontName] = font;
            }

            return font;
        }

        public ColorSpace? GetColorSpace(PdfName colorSpaceName)
        {
            if (!colorSpaces.TryGetValue(colorSpaceName, out var colorSpace))
            {
                colorSpace = ColorSpace.Parse(colorSpaceName, Dictionary.GetDictionaryOrNull(Names.ColorSpace));
                colorSpaces[colorSpaceName] = colorSpace;
            }

            return colorSpace;
        }
    }
}
