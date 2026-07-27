// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CompressionMode = System.IO.Compression.CompressionMode;

namespace PdfToSvg.Tests
{
    internal static class PngTestUtils
    {
        public static string RecompressPngsInSvg(string svgMarkup)
        {
            const string DataUrlPrefix = "data:image/png;base64,";
            XNamespace ns = "http://www.w3.org/2000/svg";
            var svg = XElement.Parse(svgMarkup, LoadOptions.PreserveWhitespace);

            var useElements = svg
                .Descendants(ns + "use")
                .ToLookup(el => el.Attribute("href").Value);

            var maskReferences = svg
                .Descendants(ns + "g")
                .ToLookup(el => el.Attribute("mask")?.Value);

            foreach (var image in svg.Descendants(ns + "image"))
            {
                var hrefAttribute = image.Attribute("href");
                var imageRenderingAttribute = image.Attribute("image-rendering");
                var interpolated = imageRenderingAttribute?.Value != "pixelated";

                if (hrefAttribute != null && hrefAttribute.Value.StartsWith(DataUrlPrefix))
                {
                    var base64Png = hrefAttribute.Value.Substring(DataUrlPrefix.Length);
                    base64Png = Convert.ToBase64String(Recompress(Convert.FromBase64String(base64Png)));
                    hrefAttribute.Value = DataUrlPrefix + base64Png;
                }
            }

            var orderedIds = new List<string>();
            var ids = new HashSet<string>();

            foreach (var el in svg.Descendants())
            {
                var id = el.Attribute("id")?.Value;
                if (id != null && ids.Add(id))
                {
                    orderedIds.Add(id);
                }

                var classNames = el.Attribute("class")?.Value;
                if (classNames != null)
                {
                    foreach (var className in Regex.Split(classNames, "\\s+"))
                    {
                        if (!string.IsNullOrEmpty(className) && ids.Add(className))
                        {
                            orderedIds.Add(className);
                        }
                    }
                }
            }

            svgMarkup = svg.ToString(SaveOptions.DisableFormatting);

            for (var i = 0; i < orderedIds.Count; i++)
            {
                var newId = "ID" + (i + 1).ToString(CultureInfo.InvariantCulture);
                svgMarkup = svgMarkup.Replace(orderedIds[i], newId);
            }

            return svgMarkup;
        }

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

                        using (var deflateStream = ZLibFactory.Compress(resultStream, true))
                        {
                            using var originalDataStream = new MemoryStream(pngData, cursor, chunkLength, false);
                            using var inflateStream = ZLibFactory.Decompress(originalDataStream);

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
