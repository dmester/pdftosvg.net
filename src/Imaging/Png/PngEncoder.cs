using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfToSvg.Imaging.Png
{
    internal class PngEncoder
    {
        private readonly Stream output;
        
        public PngEncoder(Stream output)
        {
            this.output = output;
        }

        public void WriteSignature()
        {
            var signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
            output.Write(signature, 0, signature.Length);
        }

        public void WriteImageHeader(int width, int height, PngColorType colorType, int bitDepth)
        {
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
                chunk.Write(palette, 0, palette.Length);
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
            var deflate = new ZlibStream(chunk);
            return deflate;
        }

        public void WriteImageEnd()
        {
            using (new PngChunkStream(output, PngChunkIdentifier.ImageEnd))
            {
            }
        }
    }
}
