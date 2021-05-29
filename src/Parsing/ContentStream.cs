// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal static class ContentStream
    {
        private static List<PdfDictionary> GetContents(PdfDictionary pageDict)
        {
            var contents = new List<PdfDictionary>();

            if (pageDict.TryGetValue(Names.Contents, out var objContents))
            {
                if (objContents is PdfDictionary stream)
                {
                    contents.Add(stream);
                }

                if (objContents is object[] arr)
                {
                    foreach (var stream2 in arr.OfType<PdfDictionary>())
                    {
                        contents.Add(stream2);
                    }
                }
            }

            return contents;
        }

        private static MemoryStream CreateReadOnlyStream(MemoryStream memoryStream)
        {
            return new MemoryStream(memoryStream.GetBuffer(), 0, (int)memoryStream.Length, false);
        }

        public static async Task<Stream> CombineAsync(PdfDictionary pageDict)
        {
            var contents = GetContents(pageDict);
            var combinedBuffer = new MemoryStream();

            foreach (var content in contents)
            {
                var stream = content.Stream;
                if (stream != null)
                {
                    using (var decodedStream = stream.OpenDecoded())
                    {
                        await decodedStream.CopyToAsync(combinedBuffer);
                    }
                }
            }

            return CreateReadOnlyStream(combinedBuffer);
        }

        public static Stream Combine(PdfDictionary pageDict)
        {
            var contents = GetContents(pageDict);
            var combinedBuffer = new MemoryStream();

            foreach (var content in contents)
            {
                var stream = content.Stream;
                if (stream != null)
                {
                    using (var decodedStream = stream.OpenDecoded())
                    {
                        decodedStream.CopyTo(combinedBuffer);
                    }
                }
            }

            return CreateReadOnlyStream(combinedBuffer);
        }
    }
}
