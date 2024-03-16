// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CompressionMode = System.IO.Compression.CompressionMode;

namespace PdfToSvg.Tests
{
    internal static class PngTestUtils
    {
        public static byte[] Recompress(byte[] pngData)
        {
            const int SignatureLength = 8;
            const int Int32Length = 4;
            const int NameLength = 4;

            var cursor = SignatureLength;
            while (cursor + Int32Length * 2 + NameLength < pngData.Length)
            {
                var chunkLength =
                    (pngData[cursor + 0] << 24) |
                    (pngData[cursor + 1] << 16) |
                    (pngData[cursor + 2] << 8) |
                    (pngData[cursor + 3]);

                cursor += Int32Length;

                var name = Encoding.ASCII.GetString(pngData, cursor, NameLength);
                cursor += NameLength;

                if (name == PngChunkIdentifier.ImageData)
                {
                    using (var resultStream = new MemoryStream())
                    {
                        resultStream.Write(pngData, 0, cursor);

                        var dataStartIndex = (int)resultStream.Position;

                        using (var deflateStream = new ZLibStream(resultStream, CompressionMode.Compress, true))
                        {
                            using var originalDataStream = new MemoryStream(pngData, cursor, chunkLength, false);
                            using var inflateStream = new ZLibStream(originalDataStream, CompressionMode.Decompress);

                            // CopyTo is not used, since it seems to use different chunk sizes in ZLibStream, which
                            // causes non-equal compressed output.
                            var buffer = new byte[64 << 10]; // 64 kB
                            int count;

                            while ((count = inflateStream.ReadAll(buffer, 0, buffer.Length)) != 0)
                            {
                                deflateStream.Write(buffer, 0, count);
                            }
                        }

                        var dataEndIndex = (int)resultStream.Position;

                        cursor += chunkLength;
                        resultStream.Write(pngData, cursor, pngData.Length - cursor);

                        var resultData = resultStream.ToArray();

                        // Update length
                        var chunkLengthOffset = dataStartIndex - Int32Length - NameLength;
                        var newChunkLength = dataEndIndex - dataStartIndex;
                        resultData[chunkLengthOffset + 0] = unchecked((byte)(newChunkLength >> 24));
                        resultData[chunkLengthOffset + 1] = unchecked((byte)(newChunkLength >> 16));
                        resultData[chunkLengthOffset + 2] = unchecked((byte)(newChunkLength >> 8));
                        resultData[chunkLengthOffset + 3] = unchecked((byte)(newChunkLength));

                        var crc32 = new Crc32();
                        crc32.Update(resultData, dataStartIndex - NameLength, dataEndIndex - dataStartIndex + NameLength);

                        var checksum = crc32.Value;

                        // Update checksum
                        resultData[dataEndIndex + 0] = unchecked((byte)(checksum >> 24));
                        resultData[dataEndIndex + 1] = unchecked((byte)(checksum >> 16));
                        resultData[dataEndIndex + 2] = unchecked((byte)(checksum >> 8));
                        resultData[dataEndIndex + 3] = unchecked((byte)(checksum));

                        return resultData;
                    }
                }

                cursor += chunkLength;
                cursor += Int32Length; // crc
            }

            throw new Exception("Could not find any " + PngChunkIdentifier.ImageData + " chunk in the specified PNG.");
        }
    }
}
