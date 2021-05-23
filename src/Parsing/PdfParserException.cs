using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal class PdfParserException : Exception
    {
        public PdfParserException(string message, long position) : base(message)
        {
            Position = position;
        }

        public long Position { get; }
    }
}
