// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal static class PdfCharacters
    {
        public static int ParseHexDigit(char ch)
        {
            if (ch >= '0' && ch <= '9')
            {
                return ch - '0';
            }
            else if (ch >= 'a' && ch <= 'f')
            {
                return ch - 'a' + 10;
            }
            else if (ch >= 'A' && ch <= 'F')
            {
                return ch - 'A' + 10;
            }
            else
            {
                return -1;
            }
        }

        public static bool IsLetter(char value)
        {
            return
                value >= 'a' && value <= 'z' ||
                value >= 'A' && value <= 'Z';
        }

        public static bool IsLetterOrDigit(char value)
        {
            return IsLetter(value) || IsDigit(value);
        }

        public static bool IsDigit(char ch)
        {
            return ch >= '0' && ch <= '9';
        }

        public static bool IsWhiteSpace(char ch)
        {
            // PDF spec 1.7, table 1, page 20
            return
                ch == ' ' ||
                ch == '\t' ||
                ch == '\n' ||
                ch == '\f' ||
                ch == '\r' ||
                ch == '\0';
        }

        public static bool IsDelimiter(char ch)
        {
            // PDF spec 1.7, table 2, page 21
            return
                ch == '(' ||
                ch == ')' ||
                ch == '<' ||
                ch == '>' ||
                ch == '[' ||
                ch == ']' ||
                ch == '{' ||
                ch == '}' ||
                ch == '/' ||
                ch == '%';
        }
    }
}
