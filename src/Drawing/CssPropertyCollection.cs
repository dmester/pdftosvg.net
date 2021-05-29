using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class CssPropertyCollection : ICollection<CssProperty>, IDictionary<string, string?>
    {
        private readonly IEqualityComparer<string> comparer;
        private readonly List<CssProperty> ordered;
        private readonly Dictionary<string, string?> lookup;

        public CssPropertyCollection()
        {
            comparer = StringComparer.OrdinalIgnoreCase;
            ordered = new List<CssProperty>();
            lookup = new Dictionary<string, string?>(comparer);
        }

        public int Count => ordered.Count;

        bool ICollection<CssProperty>.IsReadOnly => false;
        bool ICollection<KeyValuePair<string, string?>>.IsReadOnly => false;

        public ICollection<string> Keys => lookup.Keys;
        public ICollection<string?> Values => lookup.Values;

        /// <summary>
        /// Gets or sets the value of a CSS property in this collection. Setting a property to <c>null</c> or
        /// a string containing only whitespace will remove the property.
        /// </summary>
        public string? this[string propertyName]
        {
            get => lookup.TryGetValue(propertyName, out var value) ? value : null;
            set => Add(propertyName, value);
        }

        /// <summary>
        /// Adds or updates the value of a CSS property in this collection. Setting a property to <c>null</c> or
        /// a string containing only whitespace will remove the property.
        /// </summary>
        public void Add(string propertyName, string? value)
        {
            value = value?.Trim();

            if (value == null || value == "")
            {
                Remove(propertyName);
            }
            else
            {
                var countBefore = lookup.Count;

                lookup[propertyName] = value;

                if (lookup.Count == countBefore)
                {
                    for (var i = 0; i < ordered.Count; i++)
                    {
                        if (comparer.Equals(ordered[i].Name, propertyName))
                        {
                            ordered[i] = new CssProperty(propertyName, value);
                            break;
                        }
                    }
                }
                else
                {
                    ordered.Add(new CssProperty(propertyName, value));
                }
            }
        }

        /// <summary>
        /// Adds or updates all properties from <paramref name="other"/> to this collection.
        /// </summary>
        public void Add(CssPropertyCollection other)
        {
            foreach (var property in other)
            {
                Add(property);
            }
        }

        /// <summary>
        /// Adds or updates the value of a CSS property in this collection. Setting a property to <c>null</c> or
        /// a string containing only whitespace will remove the property.
        /// </summary>
        public void Add(CssProperty item)
        {
            Add(item.Name, item.Value);
        }

        /// <summary>
        /// Adds or updates the value of a CSS property in this collection. Setting a property to <c>null</c> or
        /// a string containing only whitespace will remove the property.
        /// </summary>
        void ICollection<KeyValuePair<string, string?>>.Add(KeyValuePair<string, string?> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            ordered.Clear();
            lookup.Clear();
        }

        public bool TryGetValue(string propertyName, out string? value)
        {
            return lookup.TryGetValue(propertyName, out value);
        }

        bool ICollection<KeyValuePair<string, string?>>.Contains(KeyValuePair<string, string?> item)
        {
            return lookup.TryGetValue(item.Key, out var value) && value == item.Value;
        }

        bool ICollection<CssProperty>.Contains(CssProperty item)
        {
            return lookup.TryGetValue(item.Name, out var value) && value == item.Value;
        }

        public bool ContainsKey(string propertyName)
        {
            return lookup.ContainsKey(propertyName);
        }

        void ICollection<CssProperty>.CopyTo(CssProperty[] array, int arrayIndex)
        {
            ordered.CopyTo(array, arrayIndex);
        }

        void ICollection<KeyValuePair<string, string?>>.CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (arrayIndex + ordered.Count > array.Length) throw new ArgumentException("The specified array is too small.", nameof(array));

            for (var i = 0; i < ordered.Count; i++)
            {
                array[arrayIndex + i] = new KeyValuePair<string, string?>(ordered[i].Name, ordered[i].Value);
            }
        }

        public bool Remove(string propertyName, string? value)
        {
            if (lookup.TryGetValue(propertyName, out var existingValue) && existingValue == value)
            {
                return Remove(propertyName);
            }

            return false;
        }

        public bool Remove(CssProperty item)
        {
            return Remove(item.Name, item.Value);
        }

        bool ICollection<KeyValuePair<string, string?>>.Remove(KeyValuePair<string, string?> item)
        {
            return Remove(item.Key, item.Value);
        }

        public bool Remove(string propertyName)
        {
            if (lookup.Remove(propertyName))
            {
                for (var i = 0; i < ordered.Count; i++)
                {
                    if (comparer.Equals(ordered[i].Name, propertyName))
                    {
                        ordered.RemoveAt(i);
                        break;
                    }
                }

                return true;
            }

            return false;
        }

        public IEnumerator<CssProperty> GetEnumerator() => ordered.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ordered.GetEnumerator();

        IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator()
        {
            return ordered
                .Select(item => new KeyValuePair<string, string?>(item.Name, item.Value))
                .GetEnumerator();
        }

        public override string ToString()
        {
            return string.Concat(ordered.Select(x => x.Name + ":" + x.Value + ";"));
        }
    }
}
