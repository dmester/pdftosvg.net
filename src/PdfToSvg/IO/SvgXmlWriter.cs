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
    internal class SvgXmlWriter : XmlWriter
    {
        private readonly XmlWriter writer;
        private readonly Stack<bool> preserveSpaceState = new Stack<bool>();
        private readonly HashSet<string> preserveSpaceElements = new HashSet<string> { "tspan", "text" };

        public SvgXmlWriter(TextWriter writer, ConformanceLevel conformanceLevel)
        {
            preserveSpaceState.Push(false);

            var settings = new XmlWriterSettings
            {
                Indent = false,
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.None,
                ConformanceLevel = conformanceLevel,
            };

            this.writer = Create(writer, settings);
        }

        public SvgXmlWriter(Stream stream, ConformanceLevel conformanceLevel) :
            this(new StreamWriter(stream, Encoding.UTF8), conformanceLevel)
        {
        }

        public override WriteState WriteState => writer.WriteState;

        public override void WriteEndElement()
        {
            preserveSpaceState.Pop();
            writer.WriteEndElement();
        }

        public override void WriteFullEndElement()
        {
            if (!preserveSpaceState.Pop())
            {
                writer.WriteRaw("\n");
            }

            writer.WriteFullEndElement();
        }

        public override void WriteStartDocument()
        {
            writer.WriteStartDocument();
            writer.WriteRaw("\n");
        }

        public override void WriteStartDocument(bool standalone)
        {
            writer.WriteStartDocument(standalone);
            writer.WriteRaw("\n");
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            var currentlyPreservingSpace = preserveSpaceState.Peek();
            if (!currentlyPreservingSpace && preserveSpaceState.Count > 1)
            {
                WriteRaw("\n");
            }

            var continuePreserveSpace = currentlyPreservingSpace || preserveSpaceElements.Contains(localName);
            preserveSpaceState.Push(continuePreserveSpace);

            writer.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteComment(string? text)
        {
            var currentlyPreservingSpace = preserveSpaceState.Peek();
            if (!currentlyPreservingSpace)
            {
                WriteRaw("\n");
            }

            writer.WriteComment(text);
        }

        public override void Flush() => writer.Flush();

        public override string? LookupPrefix(string ns) => writer.LookupPrefix(ns);

        public override void WriteBase64(byte[] buffer, int index, int count) => writer.WriteBase64(buffer, index, count);

        public override void WriteCData(string? text) => writer.WriteCData(text);

        public override void WriteCharEntity(char ch) => writer.WriteCharEntity(ch);

        public override void WriteChars(char[] buffer, int index, int count) => writer.WriteChars(buffer, index, count);

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset) => writer.WriteDocType(name, pubid, sysid, subset);

        public override void WriteEndAttribute() => writer.WriteEndAttribute();

        public override void WriteEndDocument() => writer.WriteEndDocument();

        public override void WriteEntityRef(string name) => writer.WriteEntityRef(name);

        public override void WriteProcessingInstruction(string name, string? text) => writer.WriteProcessingInstruction(name, text);

        public override void WriteRaw(char[] buffer, int index, int count) => writer.WriteRaw(buffer, index, count);

        public override void WriteRaw(string data) => writer.WriteRaw(data);

        public override void WriteStartAttribute(string? prefix, string localName, string? ns) => writer.WriteStartAttribute(prefix, localName, ns);

        public override void WriteString(string? text) => writer.WriteString(text);

        public override void WriteSurrogateCharEntity(char lowChar, char highChar) => writer.WriteSurrogateCharEntity(lowChar, highChar);

        public override void WriteWhitespace(string? ws) => writer.WriteWhitespace(ws);

#if NETFRAMEWORK
        public override void Close()
        {
            writer.Close();
        }
#endif

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)writer).Dispose();
            }
        }
    }
}
