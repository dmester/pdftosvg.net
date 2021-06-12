// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PdfToSvg.IO
{
    /// <summary>
    /// Enabling indentation in the produced XML breaks &lt;text&gt; elements, which should preserve space as-is. Disabling
    /// formatting makes it harder to vreify the output XML. This implementation inserts line breaks in contexts where
    /// space is not preserved.
    /// </summary>
    internal class SvgXmlWriter : XmlTextWriter
    {
        private readonly Stack<bool> preserveSpaceState = new Stack<bool>();
        private readonly HashSet<string> preserveSpaceElements = new HashSet<string> { "tspan", "text" };

        public SvgXmlWriter(TextWriter writer) : base(writer)
        {
            Formatting = Formatting.None;
            preserveSpaceState.Push(false);
        }

        public SvgXmlWriter(Stream stream, Encoding encoding) : base(stream, encoding)
        {
            Formatting = Formatting.None;
            preserveSpaceState.Push(false);
        }

        public override void WriteStartDocument()
        {
            base.WriteStartDocument();
            WriteRaw("\n");
        }

        public override void WriteStartDocument(bool standalone)
        {
            base.WriteStartDocument(standalone);
            WriteRaw("\n");
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            var currentlyPreservingSpace = preserveSpaceState.Peek();
            if (!currentlyPreservingSpace && preserveSpaceState.Count > 1)
            {
                WriteRaw("\n");
            }

            var continuePreserveSpace = currentlyPreservingSpace || preserveSpaceElements.Contains(localName);
            preserveSpaceState.Push(continuePreserveSpace);

            base.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteEndElement()
        {
            preserveSpaceState.Pop();
            base.WriteEndElement();
        }

        public override void WriteFullEndElement()
        {
            if (!preserveSpaceState.Pop())
            {
                WriteRaw("\n");
            }

            base.WriteFullEndElement();
        }
    }
}
