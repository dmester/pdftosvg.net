// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Globalization;

namespace PdfToSvg.Parsing
{
    internal struct Lexeme : IEquatable<Lexeme>
    {
        private object? objectValue;
        private double numericValue;
        private int position;
        private Token token;

        public Lexeme(Token token, double value = 0d, long position = -1)
        {
            this.numericValue = value;
            this.token = token;
            this.position = (int)(position + 1);
        }

        public Lexeme(Token token, PdfString value, long position = -1)
        {
            this.objectValue = value;
            this.position = (int)(position + 1);
            this.token = token;
        }

        public Lexeme(Token token, string value, long position = -1)
        {
            this.objectValue = value;
            this.position = (int)(position + 1);
            this.token = token;
        }

        public Lexeme(Token token, PdfName value, long position = -1)
        {
            this.objectValue = value;
            this.position = (int)(position + 1);
            this.token = token;
        }

        public object? ObjectValue => objectValue;
        public PdfString StringValue => objectValue as PdfString ?? PdfString.Empty;
        public string KeywordValue => objectValue as string ?? "";
        public PdfName NameValue => objectValue as PdfName ?? PdfName.Empty;
        public double NumericValue => numericValue;
        public Token Token => token;
        public int Position => position - 1;

        public override bool Equals(object? obj) =>
            obj is Lexeme lexeme && Equals(lexeme);

        public bool Equals(Lexeme other) =>
            other.token == token &&
            (
                other.position == 0 ||
                position == 0 ||
                other.position == position
            ) &&
            other.numericValue == numericValue &&
            Equals(other.objectValue, objectValue);

        public override int GetHashCode() => (int)Token;

        public override string ToString()
        {
            var value = Token == Token.Integer || Token == Token.Real
                ? numericValue.ToString(CultureInfo.InvariantCulture)
                : objectValue?.ToString();

            return position == 0
                ? $"{token} {value}"
                : $"{token} {value} (pos {Position})";
        }
    }
}
