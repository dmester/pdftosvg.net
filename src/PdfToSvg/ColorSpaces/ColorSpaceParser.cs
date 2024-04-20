// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Functions;
using PdfToSvg.Imaging;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal static class ColorSpaceParser
    {
        public static ColorSpace Parse(object? definition, PdfDictionary? colorSpaceResourcesDictionary, CancellationToken cancellationToken)
        {
            return Parse(definition, colorSpaceResourcesDictionary, 0, cancellationToken);
        }

        private static ColorSpace ParseLab(object[] colorSpaceParams)
        {
            PdfDictionary? labDict = null;

            if (colorSpaceParams.Length > 1)
            {
                labDict = colorSpaceParams[1] as PdfDictionary;
            }

            if (labDict == null)
            {
                Log.WriteLine($"/Lab color space: Missing dictionary argument.");
                labDict = new PdfDictionary();
            }

            return new LabColorSpace(
                whitePoint: labDict.GetValueOrDefault<Matrix1x3?>(Names.WhitePoint),
                range: labDict.GetArrayOrNull<double>(Names.Range));
        }

        private static ColorSpace ParseCalRgb(object[] colorSpaceParams)
        {
            PdfDictionary? labDict = null;

            if (colorSpaceParams.Length > 1)
            {
                labDict = colorSpaceParams[1] as PdfDictionary;
            }

            if (labDict == null)
            {
                Log.WriteLine($"/CalRgb color space: Missing dictionary argument.");
                labDict = new PdfDictionary();
            }

            return new CalRgbColorSpace(
                whitePoint: labDict.GetValueOrDefault<Matrix1x3?>(Names.WhitePoint),
                gamma: labDict.GetArrayOrNull<double>(Names.Gamma),
                matrix: labDict.GetValueOrDefault<Matrix3x3?>(Names.Matrix)?.Transpose()
                );
        }

        private static ColorSpace ParseCalGray(object[] colorSpaceParams)
        {
            PdfDictionary? labDict = null;

            if (colorSpaceParams.Length > 1)
            {
                labDict = colorSpaceParams[1] as PdfDictionary;
            }

            if (labDict == null)
            {
                Log.WriteLine($"/CalGray color space: Missing dictionary argument.");
                labDict = new PdfDictionary();
            }

            return new CalGrayColorSpace(labDict.GetValueOrDefault<double>(Names.Gamma, 1));
        }

        private static ColorSpace ParseIccBased(object[] colorSpaceParams, PdfDictionary? colorSpaceResourcesDictionary, int recursionCount, CancellationToken cancellationToken)
        {
            if (colorSpaceParams.Length > 1 &&
                colorSpaceParams[1] is PdfDictionary iccStreamDict)
            {
                // ICC color profiles are not supported.
                // Use alternative approach described in PDF spec 1.7, Table 66, page 158
                if (iccStreamDict.TryGetValue(Names.Alternate, out var alternate))
                {
                    return Parse(alternate, colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);
                }

                return iccStreamDict.GetValueOrDefault(Names.N, 0) switch
                {
                    1 => new DeviceGrayColorSpace(),
                    4 => new DeviceCmykColorSpace(),
                    _ => new DeviceRgbColorSpace(),
                };
            }

            Log.WriteLine("Missing stream in /ICCBased color profile.");
            return new UnsupportedColorSpace(Names.ICCBased);
        }

        private static ColorSpace ParsePattern(object[] colorSpaceParams, PdfDictionary? colorSpaceResourcesDictionary, int recursionCount, CancellationToken cancellationToken)
        {
            ColorSpace alternateSpace;

            if (colorSpaceParams.Length > 1)
            {
                alternateSpace = Parse(colorSpaceParams[1], colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);
            }
            else
            {
                alternateSpace = new DeviceRgbColorSpace();
            }

            return new PatternColorSpace(alternateSpace);
        }

        private static ColorSpace ParseSeparation(object[] colorSpaceParams, PdfDictionary? colorSpaceResourcesDictionary, int recursionCount, CancellationToken cancellationToken)
        {
            if (colorSpaceParams.Length > 3)
            {
                // PDF spec 1.7, 8.6.6.4, page 165
                var alternateSpace = Parse(colorSpaceParams[2], colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);
                var tintTransform = Function.Parse(colorSpaceParams[3], cancellationToken);
                return new SeparationColorSpace(alternateSpace, tintTransform);
            }

            Log.WriteLine("Expected 4 parameters in /Separation color profile, but found {1}.", colorSpaceParams.Length);
            return new UnsupportedColorSpace(Names.Separation);
        }

        private static ColorSpace ParseDeviceN(object[] colorSpaceParams, PdfDictionary? colorSpaceResourcesDictionary, int recursionCount, CancellationToken cancellationToken)
        {
            if (colorSpaceParams.Length > 3)
            {
                // PDF spec 1.7, 8.6.6.5, page 167
                var componentsPerSample = colorSpaceParams[1] is object[] names ? names.Length : 1;
                var alternateSpace = Parse(colorSpaceParams[2], colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);
                var tintTransform = Function.Parse(colorSpaceParams[3], cancellationToken);
                return new DeviceNColorSpace(componentsPerSample, alternateSpace, tintTransform);
            }

            Log.WriteLine("Expected 4 parameters in /DeviceN color profile, but found {1}.", colorSpaceParams.Length);
            return new UnsupportedColorSpace(Names.DeviceN);
        }

        private static ColorSpace ParseIndexed(object[] colorSpaceParams, PdfDictionary? colorSpaceResourcesDictionary, int recursionCount, CancellationToken cancellationToken)
        {
            ColorSpace? baseSpace = null;
            var lookup = ArrayUtils.Empty<byte>();

            if (colorSpaceParams.Length < 4)
            {
                Log.WriteLine($"/Indexed color space: Expected 4 arguments, but got {colorSpaceParams.Length}.");
            }

            if (colorSpaceParams.Length > 1)
            {
                baseSpace = Parse(colorSpaceParams[1], colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);

                if (colorSpaceParams.Length > 3)
                {
                    var maxLookupLength = baseSpace.ComponentsPerSample * 256;

                    if (colorSpaceParams[3] is PdfDictionary lookupDict &&
                        lookupDict.Stream != null)
                    {
                        using var lookupStream = lookupDict.Stream.OpenDecoded(cancellationToken);

                        var buffer = new byte[maxLookupLength];
                        var lookupLength = lookupStream.ReadAll(buffer, 0, buffer.Length, cancellationToken);

                        lookup = new byte[lookupLength];
                        Buffer.BlockCopy(buffer, 0, lookup, 0, lookupLength);
                    }
                    else if (colorSpaceParams[3] is PdfString lookupString)
                    {
                        lookup = new byte[Math.Min(maxLookupLength, lookupString.Length)];

                        for (var i = 0; i < lookup.Length; i++)
                        {
                            lookup[i] = lookupString[i];
                        }
                    }
                    else
                    {
                        Log.WriteLine("/Indexed color space: Expected lookup array, but found {0}.", Log.TypeOf(colorSpaceParams[3]));
                    }
                }
            }

            return new IndexedColorSpace(baseSpace ?? new DeviceRgbColorSpace(), lookup);
        }

        private static ColorSpace Parse(object? definition, PdfDictionary? colorSpaceResourcesDictionary, int recursionCount, CancellationToken cancellationToken)
        {
            if (recursionCount > 10)
            {
                Log.WriteLine("Too many color space jumps.");
                return new UnsupportedColorSpace(new PdfName("TooDeep"));
            }

            if (definition is PdfName singleColorSpaceName)
            {
                switch (singleColorSpaceName.Value)
                {
                    case nameof(Names.DeviceRGB):
                    case nameof(AbbreviatedNames.RGB):
                        return new DeviceRgbColorSpace();

                    case nameof(Names.DeviceCMYK):
                    case nameof(AbbreviatedNames.CMYK):
                        return new DeviceCmykColorSpace();

                    case nameof(Names.DeviceGray):
                    case nameof(AbbreviatedNames.G):
                        return new DeviceGrayColorSpace();

                    case nameof(Names.Pattern):
                        return new PatternColorSpace(new DeviceRgbColorSpace());
                }

                if (colorSpaceResourcesDictionary != null &&
                    colorSpaceResourcesDictionary.TryGetValue(singleColorSpaceName, out var colorSpaceResource))
                {
                    return Parse(colorSpaceResource, colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);
                }

                Log.WriteLine($"Unsupported color space: {singleColorSpaceName}.");
                return new UnsupportedColorSpace(singleColorSpaceName);
            }

            if (definition is object[] definitionArray &&
                definitionArray.Length > 0 &&
                definitionArray[0] is PdfName colorSpaceName)
            {
                switch (colorSpaceName.Value)
                {
                    case nameof(Names.DeviceRGB):
                    case nameof(AbbreviatedNames.RGB):
                        return new DeviceRgbColorSpace();

                    case nameof(Names.DeviceCMYK):
                    case nameof(AbbreviatedNames.CMYK):
                        return new DeviceCmykColorSpace();

                    case nameof(Names.DeviceGray):
                    case nameof(AbbreviatedNames.G):
                        return new DeviceGrayColorSpace();

                    case nameof(Names.Lab):
                        return ParseLab(definitionArray);

                    case nameof(Names.CalGray):
                        return ParseCalGray(definitionArray);

                    case nameof(Names.CalRGB):
                        return ParseCalRgb(definitionArray);

                    case nameof(Names.Indexed):
                    case nameof(AbbreviatedNames.I):
                        return ParseIndexed(definitionArray, colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);

                    case nameof(Names.ICCBased):
                        return ParseIccBased(definitionArray, colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);

                    case nameof(Names.Separation):
                        return ParseSeparation(definitionArray, colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);

                    case nameof(Names.DeviceN):
                        return ParseDeviceN(definitionArray, colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);

                    case nameof(Names.Pattern):
                        return ParsePattern(definitionArray, colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);
                }

                Log.WriteLine("Unsupported color space: {0}.", colorSpaceName);
                return new UnsupportedColorSpace(colorSpaceName);
            }

            Log.WriteLine("Unexpected color space definition type: {0}.", Log.TypeOf(definition));
            return new UnsupportedColorSpace(new PdfName("Undefined"));
        }
    }
}
