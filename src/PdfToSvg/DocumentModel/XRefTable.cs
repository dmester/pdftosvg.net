// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal class XRefTable : ICollection<XRef>
    {
        private readonly Dictionary<PdfObjectId, XRef> items = new Dictionary<PdfObjectId, XRef>();

        public PdfDictionary Trailer { get; set; } = new PdfDictionary();

        public int Count => items.Count;

        public bool IsReadOnly => false;

        public bool TryAdd(XRef xref)
        {
            return items.TryAdd(xref.ObjectId, xref);
        }

        public void Add(XRef xref)
        {
            items[xref.ObjectId] = xref;
        }

        public void Clear() => items.Clear();

        public bool TryGetValue(PdfObjectId id, [MaybeNullWhen(false)] out XRef result) => items.TryGetValue(id, out result);

        public bool Contains(XRef item) => items.ContainsKey(item.ObjectId);

        public void CopyTo(XRef[] array, int arrayIndex) => items.Values.CopyTo(array, arrayIndex);

        public bool Remove(XRef item) => items.Remove(item.ObjectId);

        public IEnumerator<XRef> GetEnumerator() => items.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.Values.GetEnumerator();
    }
}
