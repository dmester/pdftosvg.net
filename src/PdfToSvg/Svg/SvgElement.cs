// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Svg
{
    internal class SvgElement(string name) : SvgNode
    {
        private SvgNode? firstChild;
        private SvgNode? lastChild;

        public SvgNode? FirstChild => firstChild;
        public SvgNode? LastChild => lastChild;

        private SvgAttribute[] attributes = ArrayUtils.Empty<SvgAttribute>();
        private int attributeCount;

        public readonly string Name = name;

        public SvgElementStyle Style;

        public bool HasChildren => firstChild is not null;

        private bool PreserveInnerWhitespace => Name == "text" || Name == "tspan";
        private bool PreserveOuterWhitespace => Name == "tspan";
        private bool OptimizeChildren => Name == "svg" || Name == "g";

        public void AddChild(SvgNode node)
        {
            if (node.HasParent)
            {
                node = node.CloneNode();
            }

            node.HasParent = true;

            if (lastChild == null)
            {
                firstChild = lastChild = node;
            }
            else
            {
                lastChild.NextNode = node;
                lastChild = node;
            }
        }

        /// <summary>
        /// Replaces all child nodes with a single text node.
        /// </summary>
        public void SetValue(string text)
        {
            var node = new SvgText(text) { HasParent = true };
            firstChild = node;
            lastChild = node;
        }

        /// <summary>
        /// Adds an attribute to the element without checking for duplicates.
        /// </summary>
        /// <param name="name">Attribute name, with or without namespace alias</param>
        /// <param name="value">Attribute value</param>
        /// <param name="isRaw">If <c>true</c>, <paramref name="value"/> will be written to the resulting XML as-is,
        /// without encoding</param>
        public void AddAttribute(string name, string value, bool isRaw = false)
        {
            if (attributeCount >= attributes.Length)
            {
                Array.Resize(ref attributes, Math.Max(attributes.Length * 2, 10));
            }

            attributes[attributeCount++] = new SvgAttribute(name, value, isRaw);
        }

        /// <summary>
        /// Adds or updates an attribute on the element.
        /// </summary>
        /// <param name="name">Attribute name, with or without namespace alias</param>
        /// <param name="value">Attribute value</param>
        /// <param name="isRaw">If <c>true</c>, <paramref name="value"/> will be written to the resulting XML as-is,
        /// without encoding</param>
        public void SetAttribute(string name, string value, bool isRaw = false)
        {
            var attributes = this.attributes;
            for (var i = 0; i < attributeCount; i++)
            {
                ref var attribute = ref attributes[i];
                if (attribute.Name == name)
                {
                    attribute = new SvgAttribute(name, value, isRaw);
                    return;
                }
            }

            AddAttribute(name, value, isRaw);
        }

        public string? GetAttribute(string name)
        {
            var attributes = this.attributes;
            for (var i = 0; i < attributeCount; i++)
            {
                ref var attribute = ref attributes[i];
                if (attribute.Name == name)
                {
                    return attribute.Value;
                }
            }

            return null;
        }

        public override SvgNode CloneNode()
        {
            var clone = new SvgElement(Name);

            // Attributes
            var attributes = this.attributes;
            if (attributes.Length > 0)
            {
                clone.attributes = (SvgAttribute[])attributes.Clone();
                clone.attributeCount = this.attributeCount;
            }

            // Children
            for (var child = this.firstChild; child is not null; child = child.NextNode)
            {
                var clonedChild = child.CloneNode();
                clonedChild.HasParent = true;
                clone.lastChild?.NextNode = clonedChild;
                clone.firstChild ??= clonedChild;
                clone.lastChild = clonedChild;
            }

            // Seal the shared storage so subsequent changes to either element are isolated.
            Style.Fork();
            clone.Style = Style;

            return clone;
        }

        public override void WriteTo(SvgTextWriter writer)
        {
            WriteTo(writer, includeInheritableStyle: true);
        }

        protected void WriteTo(SvgTextWriter writer, bool includeInheritableStyle)
        {
            writer.WriteAscii('<');
            writer.WriteAscii(Name);

            // Local attributes
            var attributes = this.attributes;
            var attributeCount = this.attributeCount;
            for (var i = 0; i < attributeCount; i++)
            {
                ref var attribute = ref attributes[i];

                writer.WriteAscii(' ');
                writer.WriteAscii(attribute.Name);
                writer.WriteAscii('=', '"');

                if (attribute.IsRaw)
                {
                    writer.WriteRawText(attribute.Value);
                }
                else
                {
                    writer.WriteEncodedText(attribute.Value, escapeQuote: true);
                }

                writer.WriteAscii('"');
            }

            // Inherited attributes
            if (includeInheritableStyle)
            {
                Style.WriteTo(writer);
            }

            if (HasChildren)
            {
                if (PreserveInnerWhitespace)
                {
                    writer.WriteAscii('>');
                }
                else
                {
                    writer.WriteAscii('>', '\n');
                }

                if (OptimizeChildren)
                {
                    WriteOptimizedChildrenTo(writer);
                }
                else
                {
                    WriteChildrenTo(writer);
                }

                WriteEndTag(writer);
            }
            else
            {
                if (PreserveOuterWhitespace)
                {
                    writer.WriteAscii('/', '>');
                }
                else
                {
                    writer.WriteAscii("/>\n");
                }
            }
        }

#if HAVE_ASYNC
        public override Task WriteToAsync(SvgTextWriter writer, CancellationToken cancellationToken)
        {
            return WriteToAsync(writer, includeInheritableStyle: true, cancellationToken);
        }

        protected async Task WriteToAsync(SvgTextWriter writer, bool includeInheritableStyle, CancellationToken cancellationToken)
        {
            writer.WriteAscii('<');
            writer.WriteAscii(Name);

            // Local attributes
            var attributes = this.attributes;
            var attributeCount = this.attributeCount;
            for (var i = 0; i < attributeCount; i++)
            {
                ref var attribute = ref attributes[i];

                writer.WriteAscii(' ');
                writer.WriteAscii(attribute.Name);
                writer.WriteAscii('=', '"');

                if (attribute.Value.Length > writer.AsyncLengthThreshold)
                {
                    if (attribute.IsRaw)
                    {
                        await writer
                            .WriteRawTextAsync(attribute.Value, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await writer
                            .WriteEncodedTextAsync(attribute.Value, escapeQuote: true, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    if (writer.ShouldFlush)
                    {
                        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (attribute.IsRaw)
                    {
                        writer.WriteRawText(attribute.Value);
                    }
                    else
                    {
                        writer.WriteEncodedText(attribute.Value, escapeQuote: true);
                    }
                }

                writer.WriteAscii('"');
            }

            // Inherited attributes
            if (includeInheritableStyle)
            {
                Style.WriteTo(writer);
            }

            if (HasChildren)
            {
                if (PreserveInnerWhitespace)
                {
                    writer.WriteAscii('>');
                }
                else
                {
                    writer.WriteAscii('>', '\n');
                }

                if (OptimizeChildren)
                {
                    await WriteOptimizedChildrenToAsync(writer, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await WriteChildrenToAsync(writer, cancellationToken).ConfigureAwait(false);
                }

                WriteEndTag(writer);
            }
            else
            {
                if (PreserveOuterWhitespace)
                {
                    writer.WriteAscii('/', '>');
                }
                else
                {
                    writer.WriteAscii("/>\n");
                }
            }

            if (writer.ShouldFlush)
            {
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
#endif

        private void WriteEndTag(SvgTextWriter writer)
        {
            writer.WriteAscii('<', '/');
            writer.WriteAscii(Name);

            if (PreserveOuterWhitespace)
            {
                writer.WriteAscii('>');
            }
            else
            {
                writer.WriteAscii('>', '\n');
            }
        }

        private void WriteChildrenTo(SvgTextWriter writer)
        {
            for (var child = firstChild; child is not null; child = child.NextNode)
            {
                child.WriteTo(writer);

                if (writer.ShouldFlush)
                {
                    writer.Flush();
                }
            }
        }

        private void WriteOptimizedChildrenTo(SvgTextWriter writer)
        {
            for (var child = firstChild; child is not null; child = child.NextNode)
            {
                var childElement = child as SvgElement;
                if (childElement != null)
                {
                    var childStyle = childElement.Style;
                    var nextElement = child.NextNode as SvgElement;

                    if (!childStyle.IsEmpty && nextElement?.Style == childStyle)
                    {
                        writer.WriteAscii('<', 'g');
                        childStyle.WriteTo(writer);
                        writer.WriteAscii('>', '\n');

                        childElement.WriteTo(writer, includeInheritableStyle: false);

                        do
                        {
                            nextElement.WriteTo(writer, includeInheritableStyle: false);
                            child = nextElement;
                            nextElement = child.NextNode as SvgElement;
                        }
                        while (nextElement?.Style == childStyle);

                        writer.WriteAscii("</g>\n");
                        continue;
                    }
                }

                child.WriteTo(writer);
            }
        }

#if HAVE_ASYNC
        private async Task WriteChildrenToAsync(SvgTextWriter writer, CancellationToken cancellationToken)
        {
            for (var child = firstChild; child is not null; child = child.NextNode)
            {
                await child.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task WriteOptimizedChildrenToAsync(SvgTextWriter writer, CancellationToken cancellationToken)
        {
            for (var child = firstChild; child is not null; child = child.NextNode)
            {
                var childElement = child as SvgElement;
                if (childElement != null)
                {
                    var childStyle = childElement.Style;
                    var nextElement = child.NextNode as SvgElement;

                    if (!childStyle.IsEmpty && nextElement?.Style == childStyle)
                    {
                        writer.WriteAscii('<', 'g');
                        childStyle.WriteTo(writer);
                        writer.WriteAscii('>', '\n');

                        await childElement
                            .WriteToAsync(writer, includeInheritableStyle: false, cancellationToken)
                            .ConfigureAwait(false);

                        do
                        {
                            await nextElement
                                .WriteToAsync(writer, includeInheritableStyle: false, cancellationToken)
                                .ConfigureAwait(false);

                            child = nextElement;
                            nextElement = child.NextNode as SvgElement;
                        }
                        while (nextElement?.Style == childStyle);

                        writer.WriteAscii("</g>\n");
                        continue;
                    }
                }

                await child.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
            }
        }
#endif
    }
}
