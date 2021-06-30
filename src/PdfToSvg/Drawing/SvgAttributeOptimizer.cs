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

        private class DictionaryComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>?>
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

        internal static IEnumerable<XElement> GetOptimizableElements(XElement root)
        {
            XNode cursor = root ?? throw new ArgumentNullException(nameof(root));

            while (true)
            {
                if (cursor is XElement el && optimizableContainers.Contains(el.Name))
                {
                    yield return el;

                    if (el.FirstNode != null)
                    {
                        cursor = el.FirstNode;
                        continue;
                    }
                }

                while (cursor.NextNode == null)
                {
                    cursor = cursor.Parent;

                    if (cursor == root || cursor == null)
                    {
                        yield break;
                    }
                }

                cursor = cursor.NextNode;
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

        public static void Optimize(XElement root)
        {
            foreach (var optimizeRoot in GetOptimizableElements(root))
            {
                var partitions = optimizeRoot
                    .Nodes()
                    .PartitionBy(GetInheritableAttributes, new DictionaryComparer<string, string>());

                foreach (var partition in partitions)
                {
                    if (partition.Key != null && partition.Count() > 1)
                    {
                        var container = new XElement(ns + "g");
                        var isFirstElement = true;

                        foreach (XElement el in partition)
                        {
                            // Clear inheritable attributes
                            var attribute = el.FirstAttribute;
                            while (attribute != null)
                            {
                                var nextAttribute = attribute.NextAttribute;

                                if (partition.Key.ContainsKey(attribute.Name.LocalName))
                                {
                                    attribute.Remove();

                                    if (isFirstElement)
                                    {
                                        container.Add(attribute);
                                    }
                                }

                                attribute = nextAttribute;
                            }

                            if (isFirstElement)
                            {
                                el.AddBeforeSelf(container);
                            }

                            el.Remove();
                            container.Add(el);

                            isFirstElement = false;
                        }
                    }
                }
            }
        }
    }
}
