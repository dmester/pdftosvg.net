// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Svg
{
    internal class SvgComment(string value) : SvgNode
    {
        public readonly string Value = value;

        public override void WriteTo(SvgTextWriter writer)
        {
            writer.WriteAscii("<!--");
            writer.WriteRawText(Value);
            writer.WriteAscii("-->\n");
        }

        public override SvgNode CloneNode() => new SvgComment(Value);
    }
}
