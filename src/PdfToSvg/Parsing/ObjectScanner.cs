// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

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

namespace PdfToSvg.Parsing
{
    /// <summary>
    /// Scanner used to find objects within a PDF file without using a cross-reference table.
    /// This is used as fallback when the xref table is either missing or corrupt.
    /// See PDF spec 1.7, page 658.
    /// </summary>
    internal static class ObjectScanner
    {
        private enum ScanToken
        {
            Obj,
            EndObj,
            Stream,
            EndStream,
            Trailer,
        }

        private enum ParserState
        {
            None,
            InObject,
            InStream,
            AfterStream,
        }

        private class ScanLexeme
        {
            public ScanToken Token;
            public long Position;

            public int ObjectNumber;
            public int Generation;

            public override string ToString()
            {
                if (Token == ScanToken.Obj)
                {
                    return $"obj {ObjectNumber} {Generation}";
                }

                return Token.ToString().ToLowerInvariant();
            }
        }

        private class PositionComparer : IEqualityComparer<ScanLexeme>
        {
            public bool Equals(ScanLexeme? x, ScanLexeme? y)
            {
                return x?.Position == y?.Position;
            }

            public int GetHashCode(ScanLexeme obj)
            {
                return unchecked((int)obj.Position);
            }
        }

        private static List<ScanLexeme> ScanStream(Stream stream, CancellationToken cancellationToken)
        {
            var bufferStartPosition = 0L;
            var buffer = new byte[8096];
            var bufferLength = 0;

            var lexemes = new List<ScanLexeme>();

            int read;
            do
            {
                if (bufferLength > 256)
                {
                    Buffer.BlockCopy(buffer, bufferLength - 128, buffer, 0, 128);
                    bufferStartPosition += bufferLength - 128;
                    bufferLength = 128;
                }

                read = stream.ReadAll(buffer, bufferLength, buffer.Length - bufferLength);
                bufferLength += read;

                if (read > 0)
                {
                    var bufferText = Encoding.ASCII.GetString(buffer, 0, bufferLength);

                    foreach (Match match in Regex.Matches(bufferText, "[\r\n] {0,6}(stream\\b|endstream\\b|endobj\\b|trailer\\s{0,10}<<|(\\d{1,8}) (\\d{1,8}) obj\\b)"))
                    {
                        var value = match.Groups[1].Value;
                        var lexeme = new ScanLexeme();

                        lexeme.Position = bufferStartPosition + match.Groups[1].Index;

                        if (value.StartsWith("stream"))
                        {
                            lexeme.Token = ScanToken.Stream;
                        }
                        else if (value.StartsWith("endstream"))
                        {
                            lexeme.Token = ScanToken.EndStream;
                        }
                        else if (value.StartsWith("endobj"))
                        {
                            lexeme.Token = ScanToken.EndObj;
                        }
                        else if (value.StartsWith("trailer"))
                        {
                            lexeme.Token = ScanToken.Trailer;
                        }
                        else
                        {
                            lexeme.Token = ScanToken.Obj;
                            lexeme.ObjectNumber = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                            lexeme.Generation = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                        }

                        lexemes.Add(lexeme);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            while (read > 0);

            return lexemes;
        }

        public static bool TryScanObjects(Stream stream, out XRefTable xrefs, out List<long> trailerPositions, CancellationToken cancellationToken)
        {
            stream.Position = 0;

            var lexemes = ScanStream(stream, cancellationToken);
            var state = ParserState.None;

            xrefs = new XRefTable();

            trailerPositions = new List<long>();

            foreach (var lexeme in lexemes.Distinct(new PositionComparer()))
            {
                switch (lexeme.Token)
                {
                    case ScanToken.Obj:
                        if (state != ParserState.InStream)
                        {
                            xrefs.Add(new XRef
                            {
                                ByteOffset = lexeme.Position,
                                ObjectNumber = lexeme.ObjectNumber,
                                Generation = lexeme.Generation,
                                Type = XRefEntryType.NotFree,
                            });
                            state = ParserState.InObject;
                        }
                        break;
                    case ScanToken.Stream:
                        if (state == ParserState.InObject)
                        {
                            state = ParserState.InStream;
                        }
                        break;
                    case ScanToken.EndStream:
                        if (state == ParserState.InStream)
                        {
                            state = ParserState.AfterStream;
                        }
                        break;
                    case ScanToken.EndObj:
                        if (state == ParserState.InStream)
                        {
                            state = ParserState.None;
                        }
                        break;
                    case ScanToken.Trailer:
                        if (state != ParserState.InStream)
                        {
                            trailerPositions.Add(lexeme.Position);
                        }
                        break;
                }
            }

            return trailerPositions.Count > 0 && xrefs.Count > 0;
        }

    }
}
