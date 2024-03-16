// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg.Drawing
{
    internal class SvgAttributeOptimizer
    {
        private static readonly XNamespace ns = "http://www.w3.org/2000/svg";

        private static readonly ISet<XName> optimizableContainers = new HashSet<XName>
        {
            ns + "svg",
            ns + "g",
        };

        private static readonly ISet<string> inheritableAttributes = new HashSet<string>
        {
            // Inheritable properties according to:
            // https://www.w3.org/TR/SVG11/propidx.html
            "clip-rule",
            "color",
            "color-interpolation",
            "color-interpolation-filters",
            "color-profile",
            "color-rendering",
            "cursor",
            "direction",
            "fill",
            "fill-opacity",
            "fill-rule",
            "font",
            "font-family",
            "font-size",
            "font-size-adjust",
            "font-stretch",
            "font-style",
            "font-variant",
            "font-weight",
            "glyph-orientation-horizontal",
            "glyph-orientation-vertical",
            "image-rendering",
            "kerning",
            "letter-spacing",
            "marker",
            "marker-end",
            "pointer-events",
            "shape-rendering",
            "stroke",
            "stroke-dasharray",
            "stroke-dashoffset",
            "stroke-linecap",
            "stroke-linejoin",
            "stroke-miterlimit",
            "stroke-opacity",
            "stroke-width",
            "text-anchor",
            "text-rendering",
            "visibility",
            "word-spacing",
            "writing-mode",
            
            // Additional attributes that can be merged:
            "class",
            "style",
            "clip-path",
            "opacity",
            "transform",
        };

        private class DictionaryComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>?> where TKey : notnull
        {
            private static readonly IEqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;

            public bool Equals(Dictionary<TKey, TValue>? x, Dictionary<TKey, TValue>? y)
            {
                if (x == y)
                {
                    return true;
                }

                if (x == null)
                {
                    return y == null;
                }

                if (y == null)
                {
                    return false;
                }

                if (x.Count != y.Count)
                {
                    return false;
                }

                foreach (var pair in x)
                {
                    if (!y.TryGetValue(pair.Key, out var value))
                    {
                        return false;
                    }
                    else if (!valueComparer.Equals(pair.Value, value))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(Dictionary<TKey, TValue>? obj)
            {
                return 0;
            }
        }

        private static Dictionary<string, string>? GetInheritableAttributes(XNode node)
        {
            if (node is XElement el)
            {
                var attributeValues = new Dictionary<string, string>();

                var attribute = el.FirstAttribute;
                while (attribute != null)
                {
                    if (inheritableAttributes.Contains(attribute.Name.LocalName))
                    {
                        attributeValues[attribute.Name.LocalName] = attribute.Value;
                    }

                    attribute = attribute.NextAttribute;
                }

                if (attributeValues.Count > 0)
                {
                    return attributeValues;
                }
            }

            return null;
        }

        private static IEnumerable<object> GetOptimizedContent(XElement root)
        {
            if (optimizableContainers.Contains(root.Name))
            {
                var partitions = root
                    .Nodes()
                    .PartitionBy(GetInheritableAttributes, new DictionaryComparer<string, string>());

                foreach (var partition in partitions)
                {
                    if (partition.Key != null && partition.Count() > 1)
                    {
                        var groupAttributes = partition
                            .Cast<XElement>()
                            .First()
                            .Attributes()
                            .Where(attribute => partition.Key.ContainsKey(attribute.Name.LocalName))
                            .Select(attribute => new XAttribute(attribute.Name, attribute.Value));

                        var groupContent = partition
                            .Cast<XElement>()
                            .Select(child =>
                            {
                                var childAttributes = child
                                    .Attributes()
                                    .Where(attribute => !partition.Key.ContainsKey(attribute.Name.LocalName));
                                var childContent = GetOptimizedContent(child);

                                return new XElement(child.Name, childAttributes, childContent);
                            });

                        yield return new XElement(ns + "g", groupAttributes, groupContent);
                    }
                    else
                    {
                        foreach (var node in partition)
                        {
                            yield return GetOptimizedNode(node);
                        }
                    }
                }
            }
            else
            {
                for (var node = root.FirstNode; node != null; node = node.NextNode)
                {
                    yield return node;
                }
            }
        }

        private static XNode GetOptimizedNode(XNode root)
        {
            if (root is XElement el)
            {
                var attributes = el.Attributes();
                var content = GetOptimizedContent(el);
                root = new XElement(el.Name, attributes, content);
            }

            return root;
        }

        public static XElement Optimize(XElement root)
        {
            return (XElement)GetOptimizedNode(root);
        }
    }
}
