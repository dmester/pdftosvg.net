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
using System.Threading;
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

        private static int ReadFileHeaderOffset(byte[] buffer, int offset, int count)
        {
            var str = Encoding.ASCII.GetString(buffer, offset, count);

            var version = Regex.Match(str, "%PDF-[12].\\d");
            if (!version.Success)
            {
                throw ParserExceptions.HeaderNotFound();
            }

            return version.Index;
        }

        public int ReadFileHeaderOffset()
        {
            var buffer = new byte[1024];
            var readBytes = lexer.Stream.Read(buffer, 0, buffer.Length);
            return ReadFileHeaderOffset(buffer, 0, readBytes);
        }

#if HAVE_ASYNC
        public async Task<int> ReadFileHeaderOffsetAsync()
        {
            var buffer = new byte[1024];
            var readBytes = await lexer.Stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            return ReadFileHeaderOffset(buffer, 0, readBytes);
        }
#endif

        private static long ReadStartXRef(byte[] buffer, int offset, int count)
        {
            var str = Encoding.ASCII.GetString(buffer, offset, count);

            var eof = Regex.Match(str, "startxref[\0\t\n\f\r ]*([0-9]+)[\0\t\n\f\r ]*%%EOF", RegexOptions.RightToLeft);
            if (!eof.Success)
            {
                return -1;
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

#if HAVE_ASYNC
        public async Task<long> ReadStartXRefAsync()
        {
            lexer.Stream.Seek(-1024, SeekOrigin.End);

            var buffer = new byte[1024];
            var readBytes = await lexer.Stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

            return ReadStartXRef(buffer, 0, readBytes);
        }
#endif

        public IndirectObject? ReadIndirectObject(Dictionary<PdfObjectId, object?>? objectTable = null)
        {
            if (!TryReadInteger(out var objectNumber) ||
                !TryReadInteger(out var generation) ||
                !TryReadToken(Token.Obj))
            {
                // Not a valid object
                return null;
            }

            var end = false;

            var objectId = new PdfObjectId(objectNumber, generation);
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
                            if (streamLengthObj is PdfRef streamLengthRef && objectTable != null)
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
                                var read = lexer.Stream.ReadAll(streamContent, 0, streamLength);
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
            lexer.Read(Token.Xref);

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

                        xrefTable.TryAdd(new XRef
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

        public void ReadXRefStream(XRefTable xrefTable, PdfDictionary xrefDict, CancellationToken cancellationToken)
        {
            const int ColumnCount = 3;
            const int TypeColumnIndex = 0;
            const int DefaultType = 1;

            // PDF spec 1.7, Table 17, Page 58
            if (xrefDict.TryGetArray<int>(Names.W, out var widths) &&
                widths.Length >= ColumnCount &&
                xrefDict.Stream != null)
            {
                if (!xrefDict.TryGetArray<int>(Names.Index, out var indexArr))
                {
                    indexArr = new[] { 0, int.MaxValue };
                }

                var indexArrCursor = 0;

                var nextObjectNumber = 0;
                var maxObjectNumber = -1;

                var entryBuffer = new byte[widths.Sum()];

                using var data = OpenDecodedSharedStream(xrefDict.Stream, cancellationToken);

                while (true)
                {
                    var read = data.ReadAll(entryBuffer, 0, entryBuffer.Length);
                    if (read < entryBuffer.Length)
                    {
                        break;
                    }

                    // Advance index array
                    if (nextObjectNumber > maxObjectNumber)
                    {
                        if (indexArrCursor + 1 < indexArr.Length)
                        {
                            nextObjectNumber = indexArr[indexArrCursor];
                            maxObjectNumber = nextObjectNumber - 1 + indexArr[indexArrCursor + 1];
                            indexArrCursor += 2;
                        }
                        else
                        {
                            break;
                        }
                    }

                    var cursor = 0;
                    var values = new long[ColumnCount];

                    for (var column = 0; column < ColumnCount; column++)
                    {
                        if (widths[column] > 0)
                        {
                            for (var i = 0; i < widths[column]; i++)
                            {
                                values[column] = (values[column] << 8) | entryBuffer[cursor++];
                            }
                        }
                        else if (column == TypeColumnIndex)
                        {
                            values[TypeColumnIndex] = DefaultType;
                        }
                    }

                    var xref = new XRef
                    {
                        ObjectNumber = nextObjectNumber++,
                        Type = (XRefEntryType)values[TypeColumnIndex],
                    };

                    // PDF spec 1.7, Table 18, Page 59
                    if (xref.Type == XRefEntryType.Compressed)
                    {
                        xref.CompressedObjectNumber = (int)values[1];
                        xref.CompressedObjectElementIndex = (int)values[2];
                    }
                    else if (xref.Type == XRefEntryType.NotFree)
                    {
                        xref.ByteOffset = values[1];
                        xref.Generation = unchecked((int)values[2]);
                    }

                    xrefTable.TryAdd(xref);
                }
            }
        }

        private Stream OpenDecodedSharedStream(PdfStream stream, CancellationToken cancellationToken)
        {
            Stream encodedStream;

            if (stream is PdfOnDemandStream onDemandStream)
            {
                encodedStream = new StreamSlice(lexer.Stream, onDemandStream.Offset, onDemandStream.Length, true);
            }
            else
            {
                encodedStream = stream.Open(cancellationToken);
            }

            return stream.Filters.Decode(encodedStream);
        }

        private static void InlineScalarReferences(Dictionary<PdfObjectId, object?> objects, PdfDictionary dict)
        {
            var referencedScalars = new List<KeyValuePair<PdfName, object?>>();

            foreach (var pair in dict)
            {
                if (pair.Value is PdfRef reference &&
                    objects.TryGetValue(reference.Id, out object? referencedValue))
                {
                    if (referencedValue is PdfDictionary || referencedValue is object[])
                    {
                        // Skip reference objects as they might cause circular references when all indirect objects
                        // are finally resolved.
                    }
                    else
                    {
                        referencedScalars.Add(KeyValuePair.Create(pair.Key, referencedValue));
                    }
                }
            }

            foreach (var referencedScalar in referencedScalars)
            {
                dict[referencedScalar.Key] = referencedScalar.Value;
            }
        }

        public void ReadCompressedObjects(Dictionary<PdfObjectId, object?> objects, XRefTable xrefTable, CancellationToken cancellationToken)
        {
            var compressedObjects = xrefTable
                .Where(xref => xref.Type == XRefEntryType.Compressed)
                .GroupBy(xref => new PdfObjectId(xref.CompressedObjectNumber, 0))
                .OrderBy(group => xrefTable.TryGetValue(group.Key, out var container) ? container.ByteOffset : 0);

            foreach (var compressedObject in compressedObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (objects.TryGetValue(compressedObject.Key, out var maybeObjStream) &&
                    maybeObjStream is PdfDictionary objStream &&
                    objStream.Stream != null)
                {
                    InlineScalarReferences(objects, objStream);

                    var first = objStream.GetValueOrDefault(Names.First, 0);
                    var contentObjects = new List<object?>();

                    using (var objStreamContent = OpenDecodedSharedStream(objStream.Stream, cancellationToken))
                    {
                        objStreamContent.Skip(first);

                        var parser = new DocumentParser(file, objStreamContent);

                        var maxIndex = compressedObject.Max(x => x.CompressedObjectElementIndex) + 1;
                        for (var i = 0; i < maxIndex; i++)
                        {
                            contentObjects.Add(parser.ReadValue());
                        }
                    }

                    foreach (var compressedObjectRef in compressedObject)
                    {
                        var objectId = new PdfObjectId(compressedObjectRef.ObjectNumber, 0);
                        var obj = contentObjects[compressedObjectRef.CompressedObjectElementIndex];

                        if (objects.ContainsKey(objectId))
                        {
                            continue;
                        }
                        
                        if (obj is PdfDictionary dic)
                        {
                            if (dic.Id.IsEmpty)
                            {
                                dic.MakeIndirectObject(objectId, null);
                            }
                            else
                            {
                                Log.WriteLine(
                                    "Element at index {0} in object stream {1} was referred to as {2} but was " +
                                    "previously referred to as {3}. The duplicate reference was skipped.",
                                    compressedObjectRef.CompressedObjectElementIndex, compressedObject.Key,
                                    objectId, dic.Id);
                                continue;
                            }
                        }

                        objects[objectId] = obj;
                    }
                }
            }

        }

        public void ReadUncompressedObjects(Dictionary<PdfObjectId, object?> objects, XRefTable xrefTable, CancellationToken cancellationToken)
        {
            var uncompressedObjectRefs = xrefTable
                .Where(xref => xref.Type == XRefEntryType.NotFree)
                .OrderBy(xref => xref.ByteOffset);


            foreach (var uncompressedObjectRef in uncompressedObjectRefs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (objects.ContainsKey(uncompressedObjectRef.ObjectId))
                {
                    continue;
                }

                lexer.Seek(uncompressedObjectRef.ByteOffset, SeekOrigin.Begin);

                var obj = ReadIndirectObject(objects);
                if (obj != null)
                {
                    if (uncompressedObjectRef.ObjectId == obj.ID)
                    {
                        objects[uncompressedObjectRef.ObjectId] = obj.Value;
                    }
                    else
                    { 
                        Log.WriteLine(
                            "Object at offset {0} was referred to as {1} but called itself {2}. " +
                            "The incorrect reference was ignored.",
                            uncompressedObjectRef.ByteOffset, uncompressedObjectRef.ObjectId, obj.ID);
                    }
                }
            }
        }

        private XRefTable RebuildXRefTable(CancellationToken cancellationToken)
        {
            if (ObjectScanner.TryScanObjects(lexer.Stream, out var xrefTable, out var trailerPositions, cancellationToken))
            {
                for (var i = trailerPositions.Count - 1; i >= 0; i--)
                {
                    lexer.Seek(trailerPositions[i], SeekOrigin.Begin);

                    if (lexer.Read().Token == Token.Trailer)
                    {
                        var trailerDict = ReadDictionary();
                        if (trailerDict.ContainsKey(Names.Root))
                        {
                            xrefTable.Trailer = trailerDict;
                            return xrefTable;
                        }
                    }
                }

                throw ParserExceptions.MissingTrailer(trailerPositions.First());
            }
            else
            {
                throw ParserExceptions.CorruptPdf();
            }
        }

        public XRefTable ReadXRefTables(long byteOffsetLastXRef, CancellationToken cancellationToken)
        {
            var xrefTable = new XRefTable();
            var trailerSet = false;

            var byteOffsets = new HashSet<long>();

            if (byteOffsetLastXRef < 0)
            {
                Log.WriteLine("Missing file trailer in PDF. Indexing all objects.");
                return RebuildXRefTable(cancellationToken);
            }

            while (byteOffsetLastXRef >= 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!byteOffsets.Add(byteOffsetLastXRef))
                {
                    Log.WriteLine("Circular xref in PDF. Indexing all objects.");
                    return RebuildXRefTable(cancellationToken);
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
                        Log.WriteLine("Missing trailer after cross-reference table at position {0}. Indexing all objects.", byteOffsetLastXRef);
                        return RebuildXRefTable(cancellationToken);
                    }
                }
                else if (nextLexeme.Token == Token.Integer)
                {
                    // Cross reference stream
                    var xrefTableObject = ReadIndirectObject();

                    if (xrefTableObject?.Value is PdfDictionary dict)
                    {
                        ReadXRefStream(xrefTable, dict, cancellationToken);

                        byteOffsetLastXRef = dict.GetValueOrDefault(Names.Prev, -1);

                        if (!trailerSet)
                        {
                            xrefTable.Trailer = dict;
                            trailerSet = true;
                        }
                    }
                    else
                    {
                        Log.WriteLine("Missing trailer after cross-reference stream at position {0}. Indexing all objects.", byteOffsetLastXRef);
                        return RebuildXRefTable(cancellationToken);
                    }
                }
                else
                {
                    Log.WriteLine("Corrupt PDF file. Indexing all objects.");
                    return RebuildXRefTable(cancellationToken);
                }
            }

            return xrefTable;
        }
    }
}
