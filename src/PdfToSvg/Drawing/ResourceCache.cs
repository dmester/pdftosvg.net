// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.Fonts;
using PdfToSvg.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class ResourceCache
    {
        private readonly Dictionary<PdfName, BaseFont?> fonts = new();
        private readonly Dictionary<PdfName, ColorSpace> colorSpaces = new();

        public ResourceCache(PdfDictionary resourcesDict)
        {
            Dictionary = resourcesDict;
        }

        public PdfDictionary Dictionary { get; }

        public BaseFont? GetFont(PdfName fontName, FontResolver fontResolver, DocumentCache documentCache, CancellationToken cancellationToken)
        {
            if (!fonts.TryGetValue(fontName, out var font))
            {
                if (Dictionary.TryGetDictionary(Names.Font / fontName, out var fontDict))
                {
                    SharedFactory<BaseFont> factory;

                    lock (documentCache.Fonts)
                    {
                        if (!documentCache.Fonts.TryGetValue(fontResolver, out var factories))
                        {
                            factories = new Dictionary<PdfDictionary, SharedFactory<BaseFont>>();
                            documentCache.Fonts.Add(fontResolver, factories);
                        }

                        if (!factories.TryGetValue(fontDict, out factory))
                        {
                            factory = SharedFactory.Create(factoryCancellationToken =>
                                BaseFont.CreateAsync(fontDict, fontResolver, factoryCancellationToken));
                            factories[fontDict] = factory;
                        }
                    }

                    font = factory.GetResult(cancellationToken);
                }

                fonts[fontName] = font;
            }

            return font;
        }

        public ColorSpace GetColorSpace(PdfName colorSpaceName, CancellationToken cancellationToken)
        {
            if (!colorSpaces.TryGetValue(colorSpaceName, out var colorSpace))
            {
                colorSpace = ColorSpaceParser.Parse(
                    colorSpaceName, Dictionary.GetDictionaryOrNull(Names.ColorSpace), cancellationToken);
                colorSpaces[colorSpaceName] = colorSpace;
            }

            return colorSpace;
        }
    }
}
