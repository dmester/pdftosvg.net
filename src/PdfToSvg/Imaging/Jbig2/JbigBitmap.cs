// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jbig2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2
{
    internal class JbigBitmap
    {
        private const int MaxBitmapSize = 10000;

        private bool[] pixels;

        public int Width { get; }
        public int Height { get; }

        public bool this[int x, int y]
        {
            get => pixels[y * Width + x];
            set => pixels[y * Width + x] = value;
        }

        public static JbigBitmap Empty { get; } = new JbigBitmap(0, 0);

        public JbigBitmap(int width, int height)
        {
            if (width < 0 || width > MaxBitmapSize)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 0 || height > MaxBitmapSize)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (width == 0)
            {
                height = 0;
            }

            if (height == 0)
            {
                width = 0;
            }

            this.Width = width;
            this.Height = height;
            this.pixels = new bool[width * height];
        }

        public void Draw(JbigBitmap otherBitmap, int destX, int destY, JbigCombinationOperator combinationOperator)
        {
            var srcIndex = 0;

            var width = Math.Min(otherBitmap.Width, this.Width - destX);
            var height = Math.Min(otherBitmap.Height, this.Height - destY);

            if (destX < 0)
            {
                width += destX;
                srcIndex -= destX;
                destX = 0;
            }

            if (destY < 0)
            {
                height += destY;
                srcIndex -= destY * otherBitmap.Width;
                destY = 0;
            }

            var destRowDiff = this.Width;
            var srcRowDiff = otherBitmap.Width;

            var destIndex = destX + destY * this.Width;

            switch (combinationOperator)
            {
                case JbigCombinationOperator.Or:
                    destRowDiff -= width;
                    srcRowDiff -= width;

                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            if (otherBitmap.pixels[srcIndex])
                            {
                                this.pixels[destIndex] = true;
                            }

                            srcIndex++;
                            destIndex++;
                        }

                        destIndex += destRowDiff;
                        srcIndex += srcRowDiff;
                    }
                    break;

                case JbigCombinationOperator.And:
                    destRowDiff -= width;
                    srcRowDiff -= width;

                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            if (!otherBitmap.pixels[srcIndex])
                            {
                                this.pixels[destIndex] = false;
                            }

                            srcIndex++;
                            destIndex++;
                        }

                        destIndex += destRowDiff;
                        srcIndex += srcRowDiff;
                    }
                    break;

                case JbigCombinationOperator.Xnor:
                    destRowDiff -= width;
                    srcRowDiff -= width;

                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            this.pixels[destIndex] = this.pixels[destIndex] == otherBitmap.pixels[srcIndex];
                            srcIndex++;
                            destIndex++;
                        }

                        destIndex += destRowDiff;
                        srcIndex += srcRowDiff;
                    }
                    break;


                case JbigCombinationOperator.Xor:
                    destRowDiff -= width;
                    srcRowDiff -= width;

                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            this.pixels[destIndex] = this.pixels[destIndex] != otherBitmap.pixels[srcIndex];
                            srcIndex++;
                            destIndex++;
                        }

                        destIndex += destRowDiff;
                        srcIndex += srcRowDiff;
                    }
                    break;

                case JbigCombinationOperator.Replace:
                    for (var y = 0; y < height; y++)
                    {
                        Array.Copy(otherBitmap.pixels, srcIndex, pixels, destIndex, width);
                        destIndex += destRowDiff;
                        srcIndex += srcRowDiff;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(combinationOperator));
            }
        }

        public void Fill(bool pixelValue)
        {
#if NET5_0_OR_GREATER
            Array.Fill(pixels, pixelValue);
#else
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = pixelValue;
            }
#endif
        }

        public bool[] GetBuffer() => pixels;

        public JbigBitmap Crop(int srcX, int srcY, int width, int height)
        {
            var result = new JbigBitmap(width, height);

            var copyWidth = Math.Min(width, this.Width - srcX);
            var copyHeight = Math.Min(height, this.Height - srcY);

            var destIndex = 0;
            var srcIndex = 0;

            if (srcX < 0)
            {
                copyWidth += srcX;
                destIndex -= srcX;
            }
            else
            {
                srcIndex += srcX;
            }

            if (srcY < 0)
            {
                copyHeight += srcY;
                destIndex -= srcY * width;
            }
            else
            {
                srcIndex += srcY * Width;
            }

            if (copyWidth > 0)
            {
                for (var y = 0; y < copyHeight; y++)
                {
                    Array.Copy(pixels, srcIndex, result.pixels, destIndex, copyWidth);
                    srcIndex += Width;
                    destIndex += width;
                }
            }

            return result;
        }

        public override string ToString()
        {
            return "Bitmap " + Width + "x" + Height;
        }

        public string DebugView
        {
            get
            {
                if (Width == 0 || Height == 0)
                {
                    return "<empty>";
                }

                var result = new char[Width * Height + Height - 1];

                for (var i = 1; i < Height; i++)
                {
                    result[i * (Width + 1) - 1] = '\n';
                }

                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        result[y * (Width + 1) + x] = this[x, y] ? '\u25FC' : '\u25FB';
                    }
                }

                return new string(result);
            }
        }
    }
}
