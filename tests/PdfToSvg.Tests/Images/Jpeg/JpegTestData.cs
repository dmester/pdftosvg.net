// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal class JpegTestData
    {
        public JpegColorSpace ColorSpace;
        public int Width;
        public int Height;
        public float[] Samples;

        public void Save(string path)
        {
            using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var deflateStream = new DeflateStream(fileStream, CompressionMode.Compress);
            using var bufferedStream = new BufferedStream(deflateStream, 81920);
            using var writer = new BinaryWriter(bufferedStream);

            writer.Write(Width);
            writer.Write(Height);
            writer.Write((int)ColorSpace);

            var samples = Samples ?? ArrayUtils.Empty<float>();

            writer.Write(samples.Length);

            for (var i = 0; i < samples.Length; i++)
            {
                // On-disk format stores samples as 16-bit integers.
                writer.Write((short)samples[i]);
            }
        }

        public void Load(string path)
        {
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var deflateStream = new DeflateStream(fileStream, CompressionMode.Decompress);
            using var reader = new BinaryReader(deflateStream);

            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            ColorSpace = (JpegColorSpace)reader.ReadInt32();

            var sampleCount = reader.ReadInt32();
            var samples = new float[sampleCount];

            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = reader.ReadInt16();
            }

            Samples = samples;
        }

    }
}
