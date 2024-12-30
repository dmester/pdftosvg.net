// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Coding
{
#if DEBUG
    [DebuggerTypeProxy(typeof(JbigHuffmanTable.DebugProxy))]
    [DebuggerDisplay("{DebugView,nq}")]
#endif
    internal class JbigHuffmanTable : IEnumerable<KeyValuePair<JbigHuffmanCode, JbigHuffmanRange>>
    {
        private const int MaxPrefixLength = 24;

        private readonly Dictionary<JbigHuffmanCode, JbigHuffmanRange> prefixes = new();

        public static JbigHuffmanTable Empty { get; } = new JbigHuffmanTable([]);

        public JbigHuffmanTable(IList<JbigHuffmanRange> ranges)
        {
            if (ranges.Count == 0)
            {
                return;
            }

            // Populate prefixes
            // B.3 Assigning the prefix codes

            var codes = ranges.Count;

            // 1) and 2)
            var maxLength = ranges.Max(x => x.PrefixLength);
            var prefixLengthCount = new int[maxLength + 1];

            foreach (var range in ranges)
            {
                prefixLengthCount[range.PrefixLength]++;
            }

            // 3)
            var currentCode = 0;

            for (var currentLength = 1; currentLength <= maxLength; currentLength++)
            {
                foreach (var range in ranges)
                {
                    if (range.PrefixLength == currentLength)
                    {
                        var prefix = new JbigHuffmanCode(currentCode, currentLength);
                        prefixes[prefix] = range;
                        currentCode++;
                    }
                }

                currentCode <<= 1;
            }
        }

        public int DecodeValue(VariableBitReader reader)
        {
            var value = DecodeValueOrOob(reader);
            if (value.IsOob)
            {
                throw new JbigException("Unexpected OOB at index " + reader.Cursor);
            }

            return value.Value;
        }

        public JbigDecodedValue DecodeValueOrOob(VariableBitReader reader)
        {
            var prefix = 0;

            for (var prefixLength = 1; prefixLength <= MaxPrefixLength; prefixLength++)
            {
                prefix = (prefix << 1) | reader.ReadBit();

                if (prefixes.TryGetValue(new JbigHuffmanCode(prefix, prefixLength), out var range))
                {
                    var encodedValue = reader.ReadBitsOrThrow(range.RangeLength);
                    return range.Decode(encodedValue);
                }
            }

            throw new JbigException("No matching Huffman code found");
        }

        public IEnumerator<KeyValuePair<JbigHuffmanCode, JbigHuffmanRange>> GetEnumerator() => prefixes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#if DEBUG
        [DebuggerDisplay("{Value,nq}")]
        private class DebugProxyEntry
        {
            public string Value { get; set; } = "";
        }

        private class DebugProxy
        {
            private readonly JbigHuffmanTable table;

            public DebugProxy(JbigHuffmanTable table)
            {
                this.table = table;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object[] Items => table
                .prefixes
                .OrderBy(entry => entry.Key.CodeLength)
                .ThenBy(entry => entry.Key.Code)
                .Select(entry =>
                {
                    return new DebugProxyEntry
                    {
                        Value = entry.Key.ToString() + " => " + entry.Value,
                    };
                })
                .ToArray();
        }

        private string DebugView => "{ Huffman table, " + prefixes.Count + " codes }";
#endif
    }
}
