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

        private static MemoryStream CreateReadOnlyStream(List<byte[]> chunks, int length)
        {
            var result = new byte[length];
            var resultCursor = 0;

            foreach (var chunk in chunks)
            {
                var bytesThisIteration = Math.Min(length - resultCursor, chunk.Length);
                Buffer.BlockCopy(chunk, 0, result, resultCursor, bytesThisIteration);
                resultCursor += bytesThisIteration;
            }

            return new MemoryStream(result, false);
        }

        public static async Task<Stream> CombineAsync(PdfDictionary pageDict)
        {
            var contents = GetContents(pageDict);

            var chunks = new List<byte[]>();
            var totalBytes = 0;

            foreach (var content in contents)
            {
                using (var stream = await content.Stream.OpenDecodedAsync())
                {
                    int bytesThisIteration;
                    do
                    {
                        var chunk = new byte[4096];
                        bytesThisIteration = await stream.ReadAsync(chunk, 0, chunk.Length);
                        totalBytes += bytesThisIteration;
                        chunks.Add(chunk);
                    }
                    while (bytesThisIteration > 0);
                }
            }

            return CreateReadOnlyStream(chunks, totalBytes);
        }

        public static Stream Combine(PdfDictionary pageDict)
        {
            var contents = GetContents(pageDict);

            var chunks = new List<byte[]>();
            var totalBytes = 0;

            foreach (var content in contents)
            {
                using (var stream = content.Stream.OpenDecoded())
                {
                    int bytesThisIteration;
                    do
                    {
                        var chunk = new byte[4096];
                        bytesThisIteration = stream.Read(chunk, 0, chunk.Length);
                        totalBytes += bytesThisIteration;
                        chunks.Add(chunk);
                    }
                    while (bytesThisIteration > 0);
                }
            }

            return CreateReadOnlyStream(chunks, totalBytes);
        }
    }
}
