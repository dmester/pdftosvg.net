using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal class ContentParser : Parser
    {
        private static readonly Dictionary<string, Token> keywords = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase)
        {
            { "obj", Token.Obj },
            { "endobj", Token.EndObj },
            { "null", Token.Null },
            { "true", Token.True },
            { "false", Token.False },
            { "R", Token.Ref },

            { "BI", Token.BeginImage },
            { "ID", Token.BeginImageData },
            { "EI", Token.EndImage },
        };

        public ContentParser(Stream stream) : base(new Lexer(stream, keywords))
        {
        }

        public static IEnumerable<ContentOperation> Parse(Stream stream)
        {
            var parser = new ContentParser(stream);
            return parser.ReadContentStream();
        }

        private IEnumerable<ContentOperation> ReadContentStream()
        {
            var operands = new List<object?>();

            while (true)
            {
                var nextLexeme = lexer.Peek();

                switch (nextLexeme.Token)
                {
                    case Token.EndOfInput:
                        yield break;

                    case Token.BeginImage:
                        var imageDictionary = ReadInlineImageDictionary();

                        yield return new ContentOperation("BI", imageDictionary);
                        break;

                    case Token.Keyword:
                        lexer.Read();

                        yield return new ContentOperation(nextLexeme.Value.ToString(), operands.ToArray());

                        operands.Clear();
                        break;

                    default:
                        operands.Add(ReadValue());
                        break;
                }
            }
        }
    }
}
