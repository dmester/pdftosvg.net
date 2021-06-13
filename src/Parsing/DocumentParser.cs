// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal class DocumentParser : Parser
    {
        private static readonly Dictionary<string, Token> keywords = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase)
        {
            // Cross reference tables
            { "f", Token.Free },
            { "n", Token.NotFree },

            // Object types
            { "stream", Token.Stream },
            { "endstream", Token.EndStream },
            { "obj", Token.Obj },
            { "endobj", Token.EndObj },
            { "null", Token.Null },
            { "true", Token.True },
            { "false", Token.False },
            { "R", Token.Ref },

            // Document commands
            { "xref", Token.Xref },
            { "trailer", Token.Trailer },
        };

        private InputFile file;

        public DocumentParser(InputFile file, Stream stream) : base(new Lexer(stream, keywords))
        {
            this.file = file;
        }

        private void ReadFileHeader(byte[] buffer, int offset, int count)
        {
            var str = Encoding.ASCII.GetString(buffer, offset, count);

            var version = Regex.Match(str, "%PDF-1.\\d");
            if (!version.Success)
            {
                throw Exceptions.HeaderNotFound();
            }
        }

        public void ReadFileHeader()
        {
            var buffer = new byte[1024];
            var readBytes = lexer.Stream.Read(buffer, 0, buffer.Length);
            ReadFileHeader(buffer, 0, readBytes);
        }

        public async Task ReadFileHeaderAsync()
        {
            var buffer = new byte[1024];
            var readBytes = await lexer.Stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            ReadFileHeader(buffer, 0, readBytes);
        }

        private long ReadStartXRef(byte[] buffer, int offset, int count)
        {
            var str = Encoding.ASCII.GetString(buffer, offset, count);

            var eof = Regex.Match(str, "startxref[\0\t\n\f\r ]*([0-9]+)[\0\t\n\f\r ]*%%EOF", RegexOptions.RightToLeft);
            if (!eof.Success)
            {
                throw Exceptions.XRefTableNotFound();
            }

            return long.Parse(eof.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        public long ReadStartXRef()
        {
            lexer.Stream.Seek(-1024, SeekOrigin.End);

            var buffer = new byte[1024];
            var readBytes = lexer.Stream.Read(buffer, 0, buffer.Length);

            return ReadStartXRef(buffer, 0, readBytes);
        }

        public async Task<long> ReadStartXRefAsync()
        {
            lexer.Stream.Seek(-1024, SeekOrigin.End);

            var buffer = new byte[1024];
            var readBytes = await lexer.Stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

            return ReadStartXRef(buffer, 0, readBytes);
        }

        public IndirectObject? ReadIndirectObject(Dictionary<PdfObjectId, object?>? objectTable = null)
        {
            if (!TryReadInteger(out var objectIdNum) ||
                !TryReadInteger(out var generation) ||
                !TryReadToken(Token.Obj))
            {
                // Not a valid object
                return null;
            }

            var end = false;

            var objectId = new PdfObjectId(objectIdNum, generation);
            object? objectValue = null;
            PdfStream? objectStream = null;

            while (!end)
            {
                switch (lexer.Peek().Token)
                {
                    case Token.Stream:
                        if (objectValue is PdfDictionary dict)
                        {
                            var streamLengthObj = dict.GetValueOrDefault<object?>(Names.Length);
                            var streamLength = -1;
                            var invalidLength = false;

                            // The length can be a reference. The referenced value might or might not have been read yet.
                            var streamLengthRef = streamLengthObj as PdfRef;
                            if (streamLengthRef != null && objectTable != null)
                            {
                                objectTable.TryGetValue(streamLengthRef.Id, out streamLengthObj);
                            }

                            if (streamLengthObj is int streamLengthInt)
                            {
                                streamLength = Math.Max(0, streamLengthInt);
                            }
                            else if (streamLengthObj != null)
                            {
                                invalidLength = true;
                            }

                            if (invalidLength)
                            {
                                Log.WriteLine(
                                    $"Encountered an indirect object ({objectId}) containing a stream with the unexpected /Length value type {Log.TypeOf(streamLengthObj)}. " +
                                    "The stream is ignored.");
                            }
                            else if (streamLength >= 0 && streamLength < 1024)
                            {
                                // Read and cache small objects
                                var streamContent = new byte[streamLength];
                                var read = lexer.Stream.Read(streamContent, 0, streamLength);
                                objectStream = new PdfMemoryStream(dict, streamContent, read);
                            }
                            else
                            {
                                // Larger objects, and objects without a currently known length, are read on demand when they are needed
                                objectStream = new PdfOnDemandStream(dict, file, lexer.Stream.Position);
                            }
                        }
                        else
                        {
                            Log.WriteLine(
                                $"Encountered an indirect object ({objectId}) containing a stream without an associated dictionary. " +
                                "The stream is ignored.");
                        }

                        end = true;
                        break;

                    case Token.EndObj:
                        end = true;
                        break;

                    default:
                        objectValue = ReadValue();
                        break;
                }
            }

            if (objectValue is PdfDictionary objectValueDict)
            {
                objectValueDict.MakeIndirectObject(objectId, objectStream);
            }

            return new IndirectObject(objectId, objectValue);
        }

        public void ReadXRefTable(XRefTable xrefTable)
        {
            lexer.Read(); // TODO assert Xref

            while (TryReadInteger(out var startObjectNumber) && TryReadInteger(out var entryCount))
            {
                for (var i = 0; i < entryCount; i++)
                {
                    if (!TryReadInteger(out var byteOffset) || !TryReadInteger(out var generation))
                    {
                        break;
                    }

                    var nextLexeme = lexer.Peek();

                    if (nextLexeme.Token == Token.Free ||
                        nextLexeme.Token == Token.NotFree)
                    {
                        lexer.Read();

                        xrefTable.Add(new XRef
                        {
                            ObjectNumber = startObjectNumber + i,
                            ByteOffset = byteOffset,
                            Generation = generation,
                            Type = nextLexeme.Token == Token.Free ? XRefEntryType.Free : XRefEntryType.NotFree,
                        });
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public void ReadXRefStream(XRefTable xrefTable, PdfDictionary xrefDict)
        {
            if (xrefDict.TryGetArray<int>(Names.W, out var widths) && xrefDict.Stream != null)
            {
                var nextObjectNumber = 0;

                if (xrefDict.TryGetArray(Names.Index, out var indexArr) &&
                    indexArr.Length > 1 &&
                    indexArr[0] is int startIndexInt)
                {
                    nextObjectNumber = startIndexInt;
                }

                var entryBuffer = new byte[widths.Sum()];

                using var data = OpenDecodedSharedStream(xrefDict.Stream);

                while (true)
                {
                    var read = data.Read(entryBuffer, 0, entryBuffer.Length);
                    if (read < entryBuffer.Length)
                    {
                        break;
                    }

                    var cursor = 0;
                    var values = new long[widths.Length];

                    for (var column = 0; column < widths.Length; column++)
                    {
                        for (var i = 0; i < widths[column]; i++)
                        {
                            values[column] = (values[column] << 8) | entryBuffer[cursor++];
                        }
                    }

                    var xref = new XRef
                    {
                        ObjectNumber = nextObjectNumber++,
                        Type = (XRefEntryType)values[0],
                    };

                    if (xref.Type == XRefEntryType.Compressed)
                    {
                        xref.CompressedObjectNumber = (int)values[1];
                        xref.CompressedObjectElementIndex = (int)values[2];
                    }
                    else
                    {
                        xref.ByteOffset = values[1];
                        xref.Generation = unchecked((int)values[2]);
                    }

                    xrefTable.Add(xref);
                }
            }
        }

        private Stream OpenDecodedSharedStream(PdfStream stream)
        {
            Stream encodedStream;

            if (stream is PdfOnDemandStream onDemandStream)
            {
                encodedStream = new StreamSlice(lexer.Stream, onDemandStream.Offset, onDemandStream.Length, true);
            }
            else
            {
                encodedStream = stream.Open();
            }

            return stream.Filters.Decode(encodedStream);
        }

        public Dictionary<PdfObjectId, object?> ReadAllObjects(XRefTable xrefTable)
        {
            var objects = new Dictionary<PdfObjectId, object?>();

            foreach (var xref in xrefTable
                .Where(xref => xref.Type == XRefEntryType.NotFree)
                .OrderBy(xref => xref.ByteOffset))
            {
                lexer.Seek(xref.ByteOffset, SeekOrigin.Begin);

                var obj = ReadIndirectObject(objects);
                if (obj != null)
                {
                    objects[obj.ID] = obj.Value;
                }
            }

            foreach (var group in xrefTable
                .Where(xref => xref.Type == XRefEntryType.Compressed)
                .GroupBy(xref => xref.CompressedObjectNumber)
                .OrderBy(group => group.Key))
            {
                var containerId = new PdfObjectId(group.Key, 0);

                if (objects.TryGetValue(containerId, out var maybeObjStream) &&
                    maybeObjStream is PdfDictionary objStream &&
                    objStream.Stream != null)
                {
                    var first = objStream.GetValueOrDefault(Names.First, 0);
                    var contentObjects = new List<object?>();

                    using (var objStreamContent = OpenDecodedSharedStream(objStream.Stream))
                    {
                        objStreamContent.Skip(first);

                        var parser = new DocumentParser(file, objStreamContent);

                        var maxIndex = group.Max(x => x.CompressedObjectElementIndex) + 1;
                        for (var i = 0; i < maxIndex; i++)
                        {
                            contentObjects.Add(parser.ReadValue());
                        }
                    }

                    foreach (var xref in group)
                    {
                        var objectId = new PdfObjectId(xref.ObjectNumber, 0);
                        objects[objectId] = contentObjects[xref.CompressedObjectElementIndex];
                    }
                }
            }

            return objects;
        }

        public XRefTable ReadXRefTables(long byteOffsetLastXRef)
        {
            var xrefTable = new XRefTable();
            var trailerSet = false;

            var byteOffsets = new HashSet<long>();

            while (byteOffsetLastXRef >= 0)
            {
                if (!byteOffsets.Add(byteOffsetLastXRef))
                {
                    throw Exceptions.CircularXref(byteOffsetLastXRef);
                }

                lexer.Seek(byteOffsetLastXRef, SeekOrigin.Begin);

                var nextLexeme = lexer.Peek();

                if (nextLexeme.Token == Token.Xref)
                {
                    // Cross reference table
                    ReadXRefTable(xrefTable);

                    if (lexer.Peek().Token == Token.Trailer)
                    {
                        lexer.Read();

                        var trailerDict = ReadDictionary();
                        byteOffsetLastXRef = trailerDict.GetValueOrDefault(Names.Prev, -1);

                        if (!trailerSet)
                        {
                            xrefTable.Trailer = trailerDict;
                            trailerSet = true;
                        }
                    }
                    else
                    {
                        throw Exceptions.MissingTrailer(byteOffsetLastXRef);
                    }
                }
                else if (nextLexeme.Token == Token.Integer)
                {
                    // Cross reference stream
                    var xrefTableObject = ReadIndirectObject();

                    if (xrefTableObject?.Value is PdfDictionary dict)
                    {
                        ReadXRefStream(xrefTable, dict);

                        byteOffsetLastXRef = dict.GetValueOrDefault(Names.Prev, -1);

                        if (!trailerSet)
                        {
                            xrefTable.Trailer = dict;
                            trailerSet = true;
                        }
                    }
                    else
                    {
                        throw Exceptions.MissingTrailer(byteOffsetLastXRef);
                    }
                }
            }

            return xrefTable;
        }
    }
}
