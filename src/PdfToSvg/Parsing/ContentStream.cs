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

#if HAVE_ASYNC
        public static async Task<ArraySegment<byte>> CombineAsync(PdfDictionary pageDict, CancellationToken cancellationToken)
        {
            var contents = GetContents(pageDict);
            var combinedBuffer = new MemoryStream();

            foreach (var content in contents)
            {
                // According to ISO 32000-2-2020 Table 31, multiple content streams should be concatenated together as
                // if they were separated with at least one white-space character.
                if (combinedBuffer.Length > 0)
                {
                    combinedBuffer.WriteByte((byte)' ');
                }

                var stream = content.Stream;
                if (stream != null)
                {
                    using var decodedStream = stream.OpenDecoded(cancellationToken);
                    await decodedStream.CopyToAsync(combinedBuffer, cancellationToken).ConfigureAwait(false);
                }
            }

            combinedBuffer.Position = 0;

            if (!combinedBuffer.TryGetBuffer(out var buffer))
            {
                throw new Exception("MemoryStream unexpectedly did not allow access to its buffer");
            }

            return buffer;
        }
#endif

        public static ArraySegment<byte> Combine(PdfDictionary pageDict, CancellationToken cancellationToken)
        {
            var contents = GetContents(pageDict);
            var combinedBuffer = new MemoryStream();

            foreach (var content in contents)
            {
                // According to ISO 32000-2-2020 Table 31, multiple content streams should be concatenated together as
                // if they were separated with at least one white-space character.
                if (combinedBuffer.Length > 0)
                {
                    combinedBuffer.WriteByte((byte)' ');
                }

                var stream = content.Stream;
                if (stream != null)
                {
                    using var decodedStream = stream.OpenDecoded(cancellationToken);
                    decodedStream.CopyTo(combinedBuffer, cancellationToken);
                }
            }

            combinedBuffer.Position = 0;

            if (!combinedBuffer.TryGetBuffer(out var buffer))
            {
                throw new Exception("MemoryStream unexpectedly did not allow access to its buffer");
            }

            return buffer;
        }
    }
}
