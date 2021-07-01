// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            var buffer = memoryStream.GetBufferOrArray();
            return new MemoryStream(buffer, 0, (int)memoryStream.Length, false);
        }

#if HAVE_ASYNC
        public static async Task<Stream> CombineAsync(PdfDictionary pageDict, CancellationToken cancellationToken)
        {
            var contents = GetContents(pageDict);
            var combinedBuffer = new MemoryStream();

            foreach (var content in contents)
            {
                var stream = content.Stream;
                if (stream != null)
                {
                    using var decodedStream = stream.OpenDecoded(cancellationToken);
                    await decodedStream.CopyToAsync(combinedBuffer, cancellationToken).ConfigureAwait(false);
                }
            }

            return CreateReadOnlyStream(combinedBuffer);
        }
#endif

        public static Stream Combine(PdfDictionary pageDict, CancellationToken cancellationToken)
        {
            var contents = GetContents(pageDict);
            var combinedBuffer = new MemoryStream();

            foreach (var content in contents)
            {
                var stream = content.Stream;
                if (stream != null)
                {
                    using var decodedStream = stream.OpenDecoded(cancellationToken);
                    decodedStream.CopyTo(combinedBuffer, cancellationToken);
                }
            }

            return CreateReadOnlyStream(combinedBuffer);
        }
    }
}
