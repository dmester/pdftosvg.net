using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static ColorSpace Parse(object definition, PdfDictionary colorSpaceResourcesDictionary)
        {
            return Parse(definition, colorSpaceResourcesDictionary, 0);
        }

        private static ColorSpace ParseIndexed(object[] colorSpaceParams, PdfDictionary colorSpaceResourcesDictionary, int recursionCount)
        {
            ColorSpace baseSpace;
            byte[] lookup = Array.Empty<byte>();

            if (colorSpaceParams.Length < 4)
            {
                Log.WriteLine($"/Indexed color space: Expected 4 arguments, but got {colorSpaceParams.Length}.");
            }

            if (colorSpaceParams.Length > 1)
            {
                // TODO
                baseSpace = Parse(colorSpaceParams[1], colorSpaceResourcesDictionary, recursionCount + 1);
            }
            else
            {
                baseSpace = new DeviceRgbColorSpace();
            }

            if (colorSpaceParams.Length > 3)
            {

                // TODO can be a stream
                if (colorSpaceParams[3] is PdfString lookupString)
                {
                    lookup = new byte[lookupString.Length];

                    for (var i = 0; i < lookupString.Length; i++)
                    {
                        lookup[i] = lookupString[i];
                    }
                }
                else
                {
                    Log.WriteLine("/Indexed color space: Expected lookup array, but found {0}.", Log.TypeOf(colorSpaceParams[3]));
                }
            }

            return new IndexedColorSpace(baseSpace, lookup);
        }


        private static ColorSpace Parse(object definition, PdfDictionary colorSpaceResourcesDictionary, int recursionCount)
        {
            if (recursionCount > 10)
            {
                Log.WriteLine("Too many color space jumps.");
                return null;
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
                    return Parse(colorSpaceResource, colorSpaceResourcesDictionary, recursionCount + 1);
                }

                Log.WriteLine($"Unsupported color space: {singleColorSpaceName}.");
                return null;
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

                if (colorSpaceName == Names.Indexed)
                {
                    return ParseIndexed(definitionArray, colorSpaceResourcesDictionary, recursionCount + 1);
                }

                if (colorSpaceName == Names.Separation &&
                    definitionArray.Length > 2)
                {
                    // PDF spec 1.7, 8.6.6.4, page 165
                    // Computer screen representations should use alternateSpace.

                    // TODO this won't work if support for CIE-based color spaces is implemented
                    var alternateSpace = definitionArray[2];
                    return Parse(alternateSpace, colorSpaceResourcesDictionary, recursionCount + 1);
                }

                Log.WriteLine("Unsupported color space: {0}.", colorSpaceName);
                return null;
            }

            Log.WriteLine("Unexpected color space definition type: {0}.", Log.TypeOf(definition));
            return null;
        }

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
