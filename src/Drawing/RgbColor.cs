// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal struct RgbColor : IEquatable<RgbColor>
    {
        public RgbColor(ColorSpace colorSpace, params float[] components)
        {
            if (colorSpace == null)
            {
                throw new ArgumentNullException(nameof(colorSpace));
            }
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }
            
            // If the color space is not supported, or if the page is corrupted, there might be some
            // components missing. Don't make the whole page or document fail parsing because of this.
            var fullComponents = components;
            if (fullComponents.Length < colorSpace.ComponentsPerSample)
            {
                fullComponents = new float[colorSpace.ComponentsPerSample];
                components.CopyTo(fullComponents, 0);
            }

            colorSpace.ToRgb(fullComponents, out var red, out var green, out var blue);

            Red = MathUtils.Clamp(red, 0f, 1f);
            Green = MathUtils.Clamp(green, 0f, 1f);
            Blue = MathUtils.Clamp(blue, 0f, 1f);
        }

        public RgbColor(float red, float green, float blue)
        {
            Red = MathUtils.Clamp(red, 0f, 1f);
            Green = MathUtils.Clamp(green, 0f, 1f);
            Blue = MathUtils.Clamp(blue, 0f, 1f);
        }

        /// <summary>
        /// Red component in range [0f, 1f].
        /// </summary>
        public float Red { get; }

        /// <summary>
        /// Green component in range [0f, 1f].
        /// </summary>
        public float Green { get; }

        /// <summary>
        /// Blue component in range [0f, 1f].
        /// </summary>
        public float Blue { get; }


        public static RgbColor Black => new RgbColor(0f, 0f, 0f);
        public static RgbColor White => new RgbColor(1f, 1f, 1f);


        public override int GetHashCode()
        {
            return 
                (((int)Red * 255) << 16) |
                (((int)Green * 255) << 8) |
                ((int)Blue * 255);
        }

        public static bool operator ==(RgbColor a, RgbColor b) => a.Equals(b);
        public static bool operator !=(RgbColor a, RgbColor b) => !a.Equals(b);

        public bool Equals(RgbColor other)
        {
            return 
                other.Red == Red &&
                other.Green == Green &&
                other.Blue == Blue;
        }

        public override bool Equals(object obj) => obj is RgbColor color && Equals(color);

        public override string ToString()
        {
            return $"RGB {Red:0.00} {Green:0.00} {Blue:0.00}";
        }
    }
}
