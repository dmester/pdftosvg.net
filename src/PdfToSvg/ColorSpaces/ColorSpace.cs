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
    internal abstract class ColorSpace
    {
        public void ToRgb8(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int count)
        {
            float red, green, blue;

            for (var i = 0; i < count; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(red);
                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(green);
                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(blue);
            }
        }

        public void ToRgba8(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int count)
        {
            float red, green, blue;

            for (var i = 0; i < count; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(red);
                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(green);
                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(blue);
                rgbBuffer[rgbBufferOffset++] = 0;
            }
        }

        // TODO test
        public void ToRgb16BE(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int count)
        {
            float red, green, blue;

            for (var i = 0; i < count; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                var intRed = ToRgb16Component(red);
                var intGreen = ToRgb16Component(green);
                var intBlue = ToRgb16Component(blue);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intRed >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intRed);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intGreen >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intGreen);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intBlue >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intBlue);
            }
        }

        // TODO test
        public void ToRgba16BE(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int count)
        {
            float red, green, blue;

            for (var i = 0; i < count; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                var intRed = ToRgb16Component(red);
                var intGreen = ToRgb16Component(green);
                var intBlue = ToRgb16Component(blue);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intRed >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intRed);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intGreen >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intGreen);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intBlue >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intBlue);

                rgbBuffer[rgbBufferOffset++] = 0;
                rgbBuffer[rgbBufferOffset++] = 0;
            }
        }

        public void ToRgb(float[] input, out float red, out float green, out float blue)
        {
            var index = 0;
            ToRgb(input, ref index, out red, out green, out blue);
        }

        public abstract void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue);

        public abstract DecodeArray GetDefaultDecodeArray(int bitsPerComponent);

        public abstract int ComponentsPerSample { get; }

        public abstract float[] DefaultColor { get; }

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
                        var lookupLength = lookupStream.ReadAll(buffer, 0, buffer.Length);

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
                if (singleColorSpaceName == Names.DeviceRGB)
                {
                    return new DeviceRgbColorSpace();
                }

                if (singleColorSpaceName == Names.DeviceCMYK)
                {
                    return new DeviceCmykColorSpace();
                }

                if (singleColorSpaceName == Names.DeviceGray)
                {
                    return new DeviceGrayColorSpace();
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
                if (colorSpaceName == Names.DeviceRGB)
                {
                    return new DeviceRgbColorSpace();
                }

                if (colorSpaceName == Names.DeviceCMYK)
                {
                    return new DeviceCmykColorSpace();
                }

                if (colorSpaceName == Names.DeviceGray)
                {
                    return new DeviceGrayColorSpace();
                }

                if (colorSpaceName == Names.Lab)
                {
                    return ParseLab(definitionArray);
                }

                if (colorSpaceName == Names.CalGray)
                {
                    return ParseCalGray(definitionArray);
                }

                if (colorSpaceName == Names.CalRGB)
                {
                    return ParseCalRgb(definitionArray);
                }

                if (colorSpaceName == Names.Indexed)
                {
                    return ParseIndexed(definitionArray, colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);
                }

                if (colorSpaceName == Names.ICCBased &&
                    definitionArray.Length > 1 &&
                    definitionArray[1] is PdfDictionary iccStreamDict)
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

                if (colorSpaceName == Names.Separation &&
                    definitionArray.Length > 3)
                {
                    // PDF spec 1.7, 8.6.6.4, page 165
                    var alternateSpace = Parse(definitionArray[2], colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);
                    var tintTransform = Function.Parse(definitionArray[3], cancellationToken);
                    return new SeparationColorSpace(alternateSpace, tintTransform);
                }

                if (colorSpaceName == Names.DeviceN &&
                    definitionArray.Length > 3)
                {
                    // PDF spec 1.7, 8.6.6.5, page 167
                    var componentsPerSample = definitionArray[1] is object[] names ? names.Length : 1;
                    var alternateSpace = Parse(definitionArray[2], colorSpaceResourcesDictionary, recursionCount + 1, cancellationToken);
                    var tintTransform = Function.Parse(definitionArray[3], cancellationToken);
                    return new DeviceNColorSpace(componentsPerSample, alternateSpace, tintTransform);
                }

                Log.WriteLine("Unsupported color space: {0}.", colorSpaceName);
                return new UnsupportedColorSpace(colorSpaceName);
            }

            Log.WriteLine("Unexpected color space definition type: {0}.", Log.TypeOf(definition));
            return new UnsupportedColorSpace(new PdfName("Undefined"));
        }

#if HAVE_AGGRESSIVE_INLINING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static byte ToRgb8Component(float value)
        {
            var intValue = unchecked((int)(value * 255f + 0.5f));

            if (intValue < 0)
            {
                intValue = 0;
            }
            else if (intValue > 255)
            {
                intValue = 255;
            }

            return unchecked((byte)intValue);
        }

#if HAVE_AGGRESSIVE_INLINING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static int ToRgb16Component(float value)
        {
            var intValue = unchecked((int)(value * ushort.MaxValue + 0.5f));

            if (intValue < ushort.MinValue)
            {
                intValue = ushort.MinValue;
            }
            else if (intValue > ushort.MaxValue)
            {
                intValue = ushort.MaxValue;
            }

            return intValue;
        }
    }
}
