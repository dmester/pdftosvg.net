using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PdfToSvg.DocumentModel
{
    [DebuggerTypeProxy(typeof(PdfDictionaryDebugProxy))]
    [DebuggerDisplay("{DebugView,nq}")]
    internal class PdfDictionary : IDictionary<PdfName, object?>
    {
        List<KeyValuePair<PdfName, object?>> ordered = new List<KeyValuePair<PdfName, object?>>();
        Dictionary<PdfName, object?> lookup = new Dictionary<PdfName, object?>();

        public object? this[PdfName key]
        {
            get => lookup.TryGetValue(key, out var value) ? value : null;
            set => Add(key, value);
        }

        public ICollection<PdfName> Keys => lookup.Keys;

        public ICollection<object?> Values => lookup.Values;

        /// <summary>
        /// Id, if this dictionary is an indirect object.
        /// </summary>
        public PdfObjectId Id { get; private set; }

        /// <summary>
        /// The content stream if this is an indirect object. Otherwise, <c>null</c>.
        /// </summary>
        public PdfStream? Stream { get; private set; }

        public int Count => ordered.Count;

        bool ICollection<KeyValuePair<PdfName, object?>>.IsReadOnly => false;

        private string DebugView
        {
            get
            {
                if (Count == 0)
                {
                    return "<< empty >>";
                }

                if (this.TryGetName(Names.Type, out var name))
                {
                    return $"<< /Type: {name} ... >>";
                }

                return $"<< Count: {Count} >>";
            }
        }

        internal void MakeIndirectObject(PdfObjectId id, PdfStream? stream)
        {
            Id = id;
            Stream = stream;
        }

        public void Add(PdfName key, object? value)
        {
            Add(new KeyValuePair<PdfName, object?>(key, value));
        }

        public void Add(KeyValuePair<PdfName, object?> item)
        {
            if (lookup.ContainsKey(item.Key))
            {
                var index = ordered.FindIndex(x => x.Key == item.Key);
                ordered[index] = item;
            }
            else
            {
                ordered.Add(item);
            }

            lookup[item.Key] = item.Value;
        }

        public void Clear()
        {
            lookup.Clear();
            ordered.Clear();
        }

        public bool Contains(KeyValuePair<PdfName, object?> item)
        {
            return lookup.TryGetValue(item.Key, out var value) && Equals(value, item.Value);
        }

        public bool ContainsKey(PdfName key)
        {
            return lookup.ContainsKey(key);
        }

        public bool Remove(PdfName key)
        {
            if (lookup.Remove(key))
            {
                var index = ordered.FindIndex(x => x.Key == key);
                ordered.RemoveAt(index);
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<PdfName, object?> item)
        {
            var index = ordered.IndexOf(item);
            if (index >= 0)
            {
                ordered.RemoveAt(index);
                lookup.Remove(item.Key);
                return true;
            }
            return false;
        }

        public bool TryGetValue(PdfName key, out object? value)
        {
            return lookup.TryGetValue(key, out value);
        }

        void ICollection<KeyValuePair<PdfName, object?>>.CopyTo(KeyValuePair<PdfName, object?>[] array, int arrayIndex)
        {
            ordered.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<PdfName, object?>> GetEnumerator() => ordered.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ordered.GetEnumerator();
    }
}
