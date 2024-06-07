// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if DEBUG
namespace PdfToSvg.DocumentModel
{
    internal class PdfDictionaryDebugProxy
    {
        private readonly PdfDictionary dict;

        public PdfDictionaryDebugProxy(PdfDictionary dict)
        {
            this.dict = dict ?? throw new ArgumentNullException(nameof(dict));
        }

        internal static string FormatEntry(PdfName key, object? value, bool allowRecursion)
        {
            string valueString;

            if (value == null)
            {
                valueString = "null";
            }
            else if (value is PdfDictionary dict)
            {
                if (dict.Count == 0)
                {
                    valueString = "<< empty >>";
                }
                else if (!allowRecursion)
                {
                    valueString = $"<< {dict.Count} >>";
                }
                else
                {
                    var first = dict.First();

                    valueString = "<< " + FormatEntry(first.Key, first.Value, false);

                    if (dict.Count > 1)
                    {
                        valueString += $" +{dict.Count - 1}";
                    }

                    valueString += " >>";
                }
            }
            else if (value is PdfName name)
            {
                valueString = name.ToString();
            }
            else if (value is PdfRef reference)
            {
                valueString = reference.ToString();
            }
            else if (value is string str)
            {
                valueString = "\"" + (str.Length > 100 ? str.Substring(0, 100) + "..." : str) + "\"";
            }
            else if (value is object[] arr)
            {
                valueString = $"[ Length: {arr.Length} ]";
            }
            else
            {
                valueString = value.ToString() ?? "";
            }

            var result = key.ToString();

            if (!string.IsNullOrEmpty(valueString))
            {
                result += ": " + valueString;
            }

            return result;
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        internal class Entry
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public PdfName Key;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object? Value;

            public Entry(PdfName key, object? value)
            {
                Key = key;
                Value = value;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public string DebuggerDisplay => FormatEntry(Key, Value, true);
        }

        public PdfStream? Stream => dict.Stream;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object[] Items => dict
            .Select(entry => new Entry(entry.Key, entry.Value))
            .ToArray();

        public IEnumerable<string> CompleteDebugView
        {
            get
            {
                var sb = new StringBuilder();
                FullFormatEntry(sb, 0, dict, new HashSet<object>());
                yield return sb.ToString();
            }
        }

        private static void Indent(StringBuilder sb, int indentation)
        {
            for (var i = 0; i < indentation; i++)
            {
                sb.Append("  ");
            }
        }

        private static void FullFormatEntry(StringBuilder sb, int indentation, object? value, HashSet<object> consumed)
        {
            if (value == null)
            {
                sb.Append("null");
            }
            else if (value is PdfDictionary dict)
            {
                if (!consumed.Add(dict))
                {
                    sb.Append("<< ... >>");
                }
                else if (dict.Count == 0)
                {
                    sb.Append("<< empty >>");
                }
                else
                {
                    sb.AppendLine("<<");

                    foreach (var prop in dict)
                    {
                        Indent(sb, indentation + 1);
                        sb.Append(prop.Key.ToString());
                        sb.Append(" ");
                        FullFormatEntry(sb, indentation + 1, prop.Value, consumed);
                        sb.AppendLine();
                    }

                    Indent(sb, indentation);
                    sb.Append(">>");
                }
            }
            else if (value is string str)
            {
                sb.Append("\"" + str + "\"");
            }
            else if (value is object[] arr)
            {
                if (arr.Length == 0)
                {
                    sb.Append("[ empty ]");
                }
                else
                {
                    sb.AppendLine("[");

                    foreach (var arrValue in arr)
                    {
                        Indent(sb, indentation + 1);
                        FullFormatEntry(sb, indentation + 1, arrValue, consumed);
                        sb.AppendLine();
                    }

                    Indent(sb, indentation);
                    sb.Append("]");
                }
            }
            else
            {
                sb.Append(value.ToString());
            }
        }

    }
}
#endif
