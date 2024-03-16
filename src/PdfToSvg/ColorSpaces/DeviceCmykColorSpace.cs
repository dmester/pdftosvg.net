// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal class DeviceCmykColorSpace : ColorSpace, IEquatable<DeviceCmykColorSpace>
    {
        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            var c = input[inputOffset++];
            var m = input[inputOffset++];
            var y = input[inputOffset++];
            var k = input[inputOffset++];

            ToRgb(c, m, y, k, out red, out green, out blue);
        }

        public static void ToRgb(float c, float m, float y, float k, out float red, out float green, out float blue)
        {
            // Since ICC color profiles are currently not supported, we will do an approximated color conversion from CMYK to sRGB.
            //
            // The approximation is based on the samples provided in
            // "Color Characterization Data for SWOP proofing on Grade 3 coated paper", which can be found here:
            // http://www.npes.org/programs/standardsworkroom/toolsbestpractices/technicalreports.aspx
            //
            // The constants were generated with the ApproximateCmyk tool included in the tools folder.

            red = 1f +
                c * (
                    -1.0131f +
                    c * (-0.0474f + c * -0.0701f) +
                    m * 0.3576f +
                    y * 0.0042f +
                    k * 0.7488f
                ) +
                m * (
                    0.0252f +
                    m * (-0.1693f + m * 0.0704f) +
                    y * -0.0409f +
                    k * 0.0333f
                ) +
                y * (
                    0.0388f +
                    y * (0.03f + y * -0.0248f) +
                    k * 0.0028f
                ) +
                k * (
                    -0.7428f +
                    k * (-0.1789f + k * -0.0285f)
                );

            green = 1f +
                c * (
                    -0.3344f +
                    c * (0.0548f + c * -0.0408f) +
                    m * 0.3254f +
                    y * 0.0079f +
                    k * 0.185f
                ) +
                m * (
                    -0.6792f +
                    m * (-0.2034f + m * 0.0367f) +
                    y * 0.0646f +
                    k * 0.5394f
                ) +
                y * (
                    -0.0247f +
                    y * (-0.0727f + y * 0.0328f) +
                    k * 0.042f
                ) +
                k * (
                    -0.6876f +
                    k * (-0.2106f + k * -0.0134f)
                );

            blue = 1f +
                c * (
                    -0.1558f +
                    c * (0.0928f + c * -0.0163f) +
                    m * 0.0723f +
                    y * 0.1612f +
                    k * 0.0176f
                ) +
                m * (
                    -0.4502f +
                    m * (-0.0255f + m * 0.0337f) +
                    y * 0.2973f +
                    k * 0.2174f
                ) +
                y * (
                    -0.6536f +
                    y * (-0.2041f + y * 0.057f) +
                    k * 0.4823f
                ) +
                k * (
                    -0.6077f +
                    k * (-0.1838f + k * -0.1004f)
                );
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f });
        }

        public override int ComponentsPerSample => 4;

        public override float[] DefaultColor => new[] { 0f, 0f, 0f, 1f };

        public override int GetHashCode() => 559395;
        public bool Equals(DeviceCmykColorSpace? other) => other != null;
        public override bool Equals(object? obj) => obj is DeviceCmykColorSpace;

        public override string ToString() => "CMYK";
    }
}
