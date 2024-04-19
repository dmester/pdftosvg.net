// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal static class ParserExceptions
    {
        public static Exception CorruptPdf()
        {
            return new ParserException("The PDF file is corrupt and could not be read.", 0);
        }

        public static Exception MissingTrailer(long byteOffsetXRef)
        {
            return new ParserException("Missing or corrupt trailer in xref.", byteOffsetXRef);
        }

        public static Exception UnexpectedToken(BufferedReader reader, Lexeme unexpectedLexeme)
        {
            reader.Seek(unexpectedLexeme.Position - 20, SeekOrigin.Begin);
            var extractPosition = reader.Position;

            var extractBytes = new byte[30];
            var extractLength = reader.Read(extractBytes, 0, extractBytes.Length);
            var extract = Encoding.ASCII.GetString(extractBytes, 0, extractLength);

            reader.Position = unexpectedLexeme.Position;

            var tokenName = unexpectedLexeme.Token == Token.EndOfInput ? "end of input" : "token " + unexpectedLexeme.Token;

            var errorMessage = string.Format(
                "Unexpected {0} at position {1}.\r\nContext: \"{2}\u2192{3}\"",
                tokenName, unexpectedLexeme.Position,
                extract.Substring(0, (int)(unexpectedLexeme.Position - extractPosition)),
                extract.Substring((int)(unexpectedLexeme.Position - extractPosition))
            );

            return new ParserException(errorMessage, unexpectedLexeme.Position);
        }

        public static Exception HeaderNotFound()
        {
            return new ParserException("The specified file is not a valid PDF file. No file header was found.", 0);
        }
    }
}
