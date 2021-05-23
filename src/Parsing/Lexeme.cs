using PdfToSvg.DocumentModel;
using System;
using System.Globalization;

namespace PdfToSvg
{
    internal struct Lexeme: IEquatable<Lexeme>
    {
        long position;

        public Lexeme(Token token)
        {
            position = 0;
            Token = token;
            Value = null;
        }

        public Lexeme(Token token, PdfString value)
        {
            position = 0;
            Token = token;
            Value = value;
        }

        public Lexeme(Token token, string value)
        {
            position = 0;
            Token = token;
            Value = new PdfString(value);
        }

        public Lexeme(Token token, long position)
        {
            this.position = position + 1;
            Token = token;
            Value = null;
        }

        public Lexeme(Token token, long position, PdfString value)
        {
            this.position = position + 1;
            Token = token;
            Value = value;
        }

        public Lexeme(Token token, long position, string value)
        {
            this.position = position + 1;
            Token = token;
            Value = new PdfString(value);
        }

        public Token Token { get; }
        public PdfString Value { get; }

        public long Position => position - 1;

        public int IntValue
        {
            get
            {
                if (Token == Token.Integer)
                {
                    return int.Parse(Value.ToString(), CultureInfo.InvariantCulture);
                }
                return 0;
            }
        }

        public override bool Equals(object obj) => 
            obj is Lexeme lexeme && Equals(lexeme);

        public bool Equals(Lexeme other) => 
            other.Token == Token &&
            (
                other.position == 0 ||
                position == 0 ||
                other.position == position
            ) &&
            (other.Value ?? PdfString.Empty) == (Value ?? PdfString.Empty);

        public override int GetHashCode() => (int)Token;

        public override string ToString() => position == 0 
            ? $"{Token} {Value}" 
            : $"{Token} {Value} (pos {position})";
    }
}
