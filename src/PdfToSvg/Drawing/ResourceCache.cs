// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing.Patterns;
using PdfToSvg.Drawing.Shadings;
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
        private readonly Dictionary<PdfName, Pattern?> patterns = new();
        private readonly Dictionary<PdfName, Shading?> shadings = new();
        private readonly Dictionary<PdfName, BaseFont?> fonts = new();
        private readonly Dictionary<PdfName, ColorSpace> colorSpaces = new();

        public ResourceCache(PdfDictionary resourcesDict)
        {
            Dictionary = resourcesDict;
        }

        public PdfDictionary Dictionary { get; }

        public Pattern? GetPattern(PdfName patternName, CancellationToken cancellationToken)
        {
            if (!patterns.TryGetValue(patternName, out var pattern))
            {
                if (Dictionary.TryGetDictionary(Names.Pattern / patternName, out var patternDict))
                {
                    try
                    {
                        pattern = Pattern.Create(patternDict, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Log.WriteLine("Failed to parse pattern. " + ex);
                        pattern = null;
                    }
                }
                patterns[patternName] = pattern;
            }

            return pattern;
        }

        public Shading? GetShading(PdfName shadingName, CancellationToken cancellationToken)
        {
            if (!shadings.TryGetValue(shadingName, out var shading))
            {
                if (Dictionary.TryGetDictionary(Names.Shading / shadingName, out var shadingDict))
                {
                    try
                    {
                        shading = Shading.Create(shadingDict, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Log.WriteLine("Failed to parse shading. " + ex);
                        shading = null;
                    }
                }
                shadings[shadingName] = shading;
            }

            return shading;
        }

        public BaseFont? GetFont(PdfName fontName, FontResolver fontResolver, FontRepository fontRepository, DocumentCache documentCache, CancellationToken cancellationToken)
        {
            if (!fonts.TryGetValue(fontName, out var font))
            {
                var factory = GetFontFactory(fontName, fontResolver, fontRepository, documentCache);
                if (factory != null)
                {
                    font = factory.GetResult(cancellationToken);
                    fonts[fontName] = font;
                }
            }

            return font;
        }

#if HAVE_ASYNC
        public async Task<BaseFont?> GetFontAsync(PdfName fontName, FontResolver fontResolver, FontRepository fontRepository, DocumentCache documentCache, CancellationToken cancellationToken)
        {
            if (!fonts.TryGetValue(fontName, out var font))
            {
                var factory = GetFontFactory(fontName, fontResolver, fontRepository, documentCache);
                if (factory != null)
                {
                    font = await factory.GetResultAsync(cancellationToken).ConfigureAwait(false);
                    fonts[fontName] = font;
                }
            }

            return font;
        }
#endif

        private SharedFactory<BaseFont>? GetFontFactory(PdfName fontName, FontResolver fontResolver, FontRepository fontRepository, DocumentCache documentCache)
        {
            if (Dictionary.TryGetDictionary(Names.Font / fontName, out var fontDict))
            {
                var factory = documentCache.GetFontFactory(fontDict, fontResolver, fontRepository, () =>
                    SharedFactory.Create(
                        cancellationToken => BaseFont.Create(fontDict, fontResolver, fontRepository, cancellationToken),
                        cancellationToken => BaseFont.CreateAsync(fontDict, fontResolver, fontRepository, cancellationToken)));
                return factory;
            }

            return null;
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
