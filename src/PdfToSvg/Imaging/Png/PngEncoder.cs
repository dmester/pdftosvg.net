// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompressionMode = System.IO.Compression.CompressionMode;

namespace PdfToSvg.Imaging.Png
{
    internal class PngEncoder
    {
        private readonly Stream output;

        private const int BytesPerSample24 = 3;
        private const int BytesPerSample32 = 4;

        private const int RedOffset = 0;
        private const int GreenOffset = 1;
        private const int BlueOffset = 2;
        private const int AlphaOffset = 3;

        public PngEncoder(Stream output)
        {
            this.output = output;
        }

        public void WriteSignature()
        {
            var signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
            output.Write(signature);
        }

        public void WriteImageHeader(int width, int height, PngColorType colorType, int bitDepth)
        {
            var componentsPerSample = colorType switch
            {
                PngColorType.TruecolourWithAlpha => 4,
                PngColorType.Truecolour => 3,
                PngColorType.GreyscaleWithAlpha => 2,
                _ => 1,
            };

            using (var chunk = new PngChunkStream(output, PngChunkIdentifier.ImageHeader))
            {
                chunk.WriteBigEndian(width);
                chunk.WriteBigEndian(height);
                chunk.WriteByte((byte)bitDepth);
                chunk.WriteByte((byte)colorType);
                chunk.WriteByte(0); // Compression
                chunk.WriteByte(0); // Filter
                chunk.WriteByte(0); // Interlace
            }
        }

        public void WritePalette(byte[] palette)
        {
            using (var chunk = new PngChunkStream(output, PngChunkIdentifier.Palette))
            {
                chunk.Write(palette);
            }
        }

        public void WriteTransparency(int red, int green, int blue)
        {
            using (var chunk = new PngChunkStream(output, PngChunkIdentifier.Transparency))
            {
                chunk.WriteByte(0);
                chunk.WriteByte((byte)red);
                chunk.WriteByte(0);
                chunk.WriteByte((byte)green);
                chunk.WriteByte(0);
                chunk.WriteByte((byte)blue);
            }
        }

        public void WriteImageGamma()
        {
            using (var chunk = new PngChunkStream(output, PngChunkIdentifier.ImageGamma))
            {
                chunk.WriteBigEndian(45455);
            }
        }

        public Stream GetImageDataStream()
        {
            var chunk = new PngChunkStream(output, PngChunkIdentifier.ImageData);
            var deflate = new ZLibStream(chunk, CompressionMode.Compress);
            return deflate;
        }

        public void WriteImageEnd()
        {
            using (new PngChunkStream(output, PngChunkIdentifier.ImageEnd))
            {
            }
        }

        private static byte[] GetPngData24Bit(byte[] rgba32Buffer, int width, int height, int alphaRed, int alphaGreen, int alphaBlue, out bool hasAlpha)
        {
            var rowSize = 1 + width * BytesPerSample24;
            var result = new byte[height * rowSize];

            var inputOffset = 0;
            var outputOffset = 1;

            hasAlpha = false;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var alpha = rgba32Buffer[inputOffset + AlphaOffset];
                    if (alpha < 127)
                    {
                        result[outputOffset + RedOffset] = (byte)alphaRed;
                        result[outputOffset + GreenOffset] = (byte)alphaGreen;
                        result[outputOffset + BlueOffset] = (byte)alphaBlue;
                        hasAlpha = true;
                    }
                    else
                    {
                        result[outputOffset + RedOffset] = rgba32Buffer[inputOffset + RedOffset];
                        result[outputOffset + GreenOffset] = rgba32Buffer[inputOffset + GreenOffset];
                        result[outputOffset + BlueOffset] = rgba32Buffer[inputOffset + BlueOffset];
                    }

                    inputOffset += BytesPerSample32;
                    outputOffset += BytesPerSample24;
                }

                outputOffset++; // Filter
            }

            return result;
        }

        private static byte[] GetPngData32Bit(byte[] rgba32Buffer, int width, int height)
        {
            var scanlineBytes = width * BytesPerSample32;
            var rowSize = 1 + scanlineBytes;

            var result = new byte[height * rowSize];

            var inputOffset = 0;
            var outputOffset = 1;

            for (var y = 0; y < height; y++)
            {
                Array.Copy(rgba32Buffer, inputOffset, result, outputOffset, scanlineBytes);
                inputOffset += scanlineBytes;
                outputOffset += rowSize;
            }

            return result;
        }

        private static void Filter(PngFilter filter, byte[] buffer, int width, int height, int bytesPerSample)
        {
            var scanlineBytes = width * bytesPerSample;
            var rowSize = 1 + scanlineBytes;
            var cursor = height * (1 + scanlineBytes) - 1;

            if (height < 1 || width < 1) return;

            switch (filter)
            {
                case PngFilter.Sub:
                    for (var y = height - 1; y >= 0; y--)
                    {
                        for (var x = scanlineBytes - 1; x >= bytesPerSample; x--)
                        {
                            buffer[cursor] = (byte)(buffer[cursor] - buffer[cursor - bytesPerSample]);
                            cursor--;
                        }
                        cursor -= bytesPerSample;

                        buffer[cursor--] = (byte)PngFilter.Sub;
                    }
                    break;

                case PngFilter.Up:
                    for (var y = height - 1; y > 0; y--)
                    {
                        for (var x = scanlineBytes - 1; x >= 0; x--)
                        {
                            buffer[cursor] = (byte)(buffer[cursor] - buffer[cursor - rowSize]);
                            cursor--;
                        }
                        buffer[cursor--] = (byte)PngFilter.Up;
                    }
                    // Skip first row
                    break;

                case PngFilter.Average:
                    for (var y = height - 1; y > 0; y--)
                    {
                        var x = scanlineBytes - 1;

                        for (; x >= bytesPerSample; x--)
                        {
                            var a = buffer[cursor - bytesPerSample];
                            var b = buffer[cursor - rowSize];
                            buffer[cursor] = (byte)(buffer[cursor] - (a + b) / 2);
                            cursor--;
                        }

                        for (; x >= 0; x--)
                        {
                            var b = buffer[cursor - rowSize];
                            buffer[cursor] = (byte)(buffer[cursor] - b / 2);
                            cursor--;
                        }

                        buffer[cursor--] = (byte)PngFilter.Average;
                    }

                    // First row
                    for (var x = scanlineBytes - 1; x >= bytesPerSample; x--)
                    {
                        var a = buffer[cursor - bytesPerSample];
                        buffer[cursor] = (byte)(buffer[cursor] - a / 2);
                        cursor--;
                    }
                    cursor -= bytesPerSample;

                    buffer[cursor] = (byte)PngFilter.Average;
                    break;

                case PngFilter.Paeth:
                    for (var y = height - 1; y > 0; y--)
                    {
                        var x = scanlineBytes - 1;

                        for (; x >= bytesPerSample; x--)
                        {
                            var a = buffer[cursor - bytesPerSample];
                            var b = buffer[cursor - rowSize];
                            var c = buffer[cursor - rowSize - bytesPerSample];
                            buffer[cursor] = (byte)(buffer[cursor] - PngUtils.PaethPredictor(a, b, c));
                            cursor--;
                        }

                        for (; x >= 0; x--)
                        {
                            var b = buffer[cursor - rowSize];
                            buffer[cursor] = (byte)(buffer[cursor] - PngUtils.PaethPredictor(0, b, 0));
                            cursor--;
                        }

                        buffer[cursor--] = (byte)PngFilter.Paeth;
                    }

                    // First row
                    for (var x = scanlineBytes - 1; x >= bytesPerSample; x--)
                    {
                        var a = buffer[cursor - bytesPerSample];
                        buffer[cursor] = (byte)(buffer[cursor] - PngUtils.PaethPredictor(a, 0, 0));
                        cursor--;
                    }
                    cursor -= bytesPerSample;

                    buffer[cursor] = (byte)PngFilter.Paeth;
                    break;
            }
        }

        public static byte[] Truecolour(
            byte[] rgba32Buffer, int width, int height, PngFilter filter,
            int alphaRed, int alphaGreen, int alphaBlue)
        {
            using var stream = new MemoryStream();
            var encoder = new PngEncoder(stream);

            encoder.WriteSignature();
            encoder.WriteImageHeader(width, height, PngColorType.Truecolour, bitDepth: 8);

            var data = GetPngData24Bit(rgba32Buffer, width, height, alphaRed, alphaGreen, alphaBlue, out var hasAlpha);
            Filter(filter, data, width, height, BytesPerSample24);

            if (hasAlpha)
            {
                encoder.WriteTransparency(alphaRed, alphaGreen, alphaBlue);
            }

            using (var pngDataStream = encoder.GetImageDataStream())
            {
                pngDataStream.Write(data);
            }

            encoder.WriteImageEnd();

            return stream.ToArray();
        }

        public static byte[] TruecolourWithAlpha(byte[] rgba32Buffer, int width, int height, PngFilter filter)
        {
            using var stream = new MemoryStream();
            var encoder = new PngEncoder(stream);

            encoder.WriteSignature();
            encoder.WriteImageHeader(width, height, PngColorType.TruecolourWithAlpha, bitDepth: 8);

            var data = GetPngData32Bit(rgba32Buffer, width, height);
            Filter(filter, data, width, height, BytesPerSample32);

            using (var pngDataStream = encoder.GetImageDataStream())
            {
                pngDataStream.Write(data);
            }

            encoder.WriteImageEnd();

            return stream.ToArray();
        }
    }
}
