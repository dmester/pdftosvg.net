// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.CMaps
{
    internal class CMapParser : Parser
    {
        // https://adobe-type-tools.github.io/font-tech-notes/pdfs/5014.CIDFont_Spec.pdf

        private static readonly Dictionary<string, Token> keywords = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase)
        {
            { "begincodespacerange", Token.BeginCodeSpaceRange },
            { "endcodespacerange", Token.EndCodeSpaceRange },
            { "beginbfchar", Token.BeginBfChar },
            { "endbfchar", Token.EndBfChar },
            { "beginbfrange", Token.BeginBfRange },
            { "endbfrange", Token.EndBfRange },
            { "begincidchar", Token.BeginCidChar },
            { "endcidchar", Token.EndCidChar },
            { "begincidrange", Token.BeginCidRange },
            { "endcidrange", Token.EndCidRange },
            { "beginnotdefchar", Token.BeginNotDefChar },
            { "endnotdefchar", Token.EndNotDefChar },
            { "beginnotdefrange", Token.BeginNotDefRange },
            { "endnotdefrange", Token.EndNotDefRange },
            { "usecmap", Token.UseCMap },
        };

        private CMapParser(Stream stream) : base(new Lexer(stream, keywords))
        {
        }

        public static CMapData Parse(PdfStream stream, CancellationToken cancellationToken)
        {
            using (var decodedStream = stream.OpenDecoded(cancellationToken))
            {
                var parser = new CMapParser(decodedStream);
                return parser.ReadCMap();
            }
        }

        public static CMapData Parse(Stream stream, CancellationToken cancellationToken)
        {
            var parser = new CMapParser(stream);
            return parser.ReadCMap();
        }

        public CMapData ReadCMap()
        {
            var cmap = new CMapData();
            Lexeme lexeme;

            do
            {
                lexeme = lexer.Read();

                switch (lexeme.Token)
                {
                    case Token.BeginCodeSpaceRange:
                        ReadCodeSpaceRange(cmap);
                        break;

                    case Token.BeginBfChar:
                        ReadBfChar(cmap);
                        break;

                    case Token.BeginBfRange:
                        ReadBfRange(cmap);
                        break;

                    case Token.BeginCidChar:
                        ReadCidChar(cmap);
                        break;

                    case Token.BeginCidRange:
                        ReadCidRange(cmap);
                        break;

                    case Token.BeginNotDefChar:
                        ReadNotDefChar(cmap);
                        break;

                    case Token.BeginNotDefRange:
                        ReadNotDefRange(cmap);
                        break;

                    default:
                        if (lexeme.Token == Token.Name)
                        {
                            if (lexeme.Value.ToString() == "CMapName" && lexer.TryRead(Token.Name, out var name))
                            {
                                cmap.Name = name.Value.ToString();
                            }
                            else if (lexer.TryRead(Token.UseCMap))
                            {
                                cmap.UseCMap = lexeme.Value.ToString();
                            }
                        }
                        break;
                }
            }
            while (lexeme.Token != Token.EndOfInput);

            return cmap;
        }

        private uint ReadHex32() => ReadHex32(out _);

        private uint ReadHex32(out int length)
        {
            var lexeme = lexer.Read(Token.HexString);
            var arr = lexeme.Value;

            uint value = 0;

            for (var i = 0; i < arr.Length; i++)
            {
                value = (value << 8) | arr[i];
            }

            length = arr.Length;

            return value;
        }

        private int ReadInt32()
        {
            var lexeme = lexer.Read(Token.Integer);
            return lexeme.IntValue;
        }

        private string ReadUnicode()
        {
            var lexeme = lexer.Read(Token.HexString);
            return lexeme.Value.ToString(Encoding.BigEndianUnicode);
        }

        private uint ReadCodePoint()
        {
            var unicode = ReadUnicode();
            return Utf16Encoding.DecodeCodePoint(unicode, 0, out _);
        }

        private void ReadNotDefChar(CMapData cmap)
        {
            while (!lexer.TryRead(Token.EndNotDefChar))
            {
                var src = ReadHex32(out var srcLength);
                var dst = ReadInt32();

                cmap.NotDefChars.Add(new CMapChar(src, srcLength, (uint)dst));
            }
        }

        private void ReadNotDefRange(CMapData cmap)
        {
            while (!lexer.TryRead(Token.EndNotDefRange))
            {
                var srcLo = ReadHex32(out var srcLength);
                var srcHi = ReadHex32();
                var dst = ReadInt32();

                cmap.NotDefRanges.Add(new CMapRange(srcLo, srcHi, srcLength, (uint)dst));
            }
        }

        private void ReadCidChar(CMapData cmap)
        {
            while (!lexer.TryRead(Token.EndCidChar))
            {
                var src = ReadHex32(out var srcLength);
                var dst = ReadInt32();

                cmap.CidChars.Add(new CMapChar(src, srcLength, (uint)dst));
            }
        }

        private void ReadCidRange(CMapData cmap)
        {
            while (!lexer.TryRead(Token.EndCidRange))
            {
                var srcLo = ReadHex32(out var srcLength);
                var srcHi = ReadHex32();
                var dst = ReadInt32();

                cmap.CidRanges.Add(new CMapRange(srcLo, srcHi, srcLength, (uint)dst));
            }
        }

        private void ReadBfChar(CMapData cmap)
        {
            while (!lexer.TryRead(Token.EndBfChar))
            {
                var src = ReadHex32(out var srcLength);
                var unicode = ReadUnicode();

                cmap.BfChars.Add(new CMapChar(src, srcLength, unicode));
            }
        }

        private void ReadBfRange(CMapData cmap)
        {
            while (!lexer.TryRead(Token.EndBfRange))
            {
                var srcLo = ReadHex32(out var srcLength);
                var srcHi = ReadHex32();

                if (lexer.TryRead(Token.BeginArray))
                {
                    var dstStrings = new List<PdfString>();

                    while (!lexer.TryRead(Token.EndArray))
                    {
                        dstStrings.Add(lexer.Read(Token.HexString).Value);
                    }

                    var count = Math.Min(dstStrings.Count, srcHi - srcLo + 1);

                    for (var i = 0; i < count; i++)
                    {
                        var unicode = dstStrings[i].ToString(Encoding.BigEndianUnicode);

                        cmap.BfChars.Add(new CMapChar(srcLo + unchecked((uint)i), srcLength, unicode));
                    }
                }
                else
                {
                    var startUnicode = ReadCodePoint();

                    cmap.BfRanges.Add(new CMapRange(srcLo, srcHi, srcLength, startUnicode));
                }
            }
        }

        private void ReadCodeSpaceRange(CMapData cmap)
        {
            while (!lexer.TryRead(Token.EndCodeSpaceRange))
            {
                var lo = ReadHex32(out var loLength);
                var hi = ReadHex32();

                cmap.CodeSpaceRanges.Add(new CMapCodeSpaceRange(lo, hi, loLength));
            }
        }
    }
}
