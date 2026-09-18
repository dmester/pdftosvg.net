// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PdfToSvg.DocumentModel
{
    /// <summary>
    /// Properties:
    /// <list type="bullet">
    ///     <item>Writes are not thread safe</item>
    ///     <item>Order of added values is preserved</item>
    ///     <item>Indexer does not throw on missing values</item>
    /// </list>
    /// <para>
    ///     The dictionary is intentionally not using the .NET Dictionary to lower GC pressure. The specialized
    ///     implementation is also more performant since the equality comparer is inlined.
    /// </para>
    /// </summary>
#if DEBUG
    [DebuggerTypeProxy(typeof(PdfDictionaryDebugProxy))]
    [DebuggerDisplay("{DebugView,nq}")]
#endif
    internal class PdfDictionary : IDictionary<PdfName, object?>
    {
        private const uint HashMultiplier = 0x9E3779B9u; // Golden Ratio

        private int[] indexes = ArrayUtils.Empty<int>();
        private Entry[] entries = ArrayUtils.Empty<Entry>();

        private const int Int32Bits = 32;

        // The capacity is defined as `1 << (Int32Bits - bucketShift)`
        //
        // Initial capacity was decided from frequency statistics on real-life PDFs:
        //
        //  Entry count    Cumulative percentage
        // --------------------------------------
        //  0                            0.3646%
        //  1                            9.6441%
        //  2                           19.1371%
        //  3-4                         56.0958%
        //  5-8                         88.5671%
        //  9-16                        99.8594%
        //  17-32                       99.9539%
        //  33-64                       99.9845%
        //  65-128                      99.9936%
        //  129-256                     99.9975%
        //  257-512                     99.9991%
        //
        private const int InitialCapacityLog2 = 3; // = Capacity 8
        private const int MaximumCapacityLog2 = 20;

        private int bucketShift = Int32Bits;
        private int entryCount;
        private int count;

        private struct Entry(PdfName key, object? value, int hashCode, int nextIndex = -1)
        {
            public PdfName? Key = key; // null if the value is removed
            public object? Value = value;
            public int HashCode = hashCode;
            public int NextIndex = nextIndex;
        }

        public object? this[PdfName key]
        {
            get => TryGetValue(key, out var value) ? value : null;
            set => Add(key, value);
        }

        public ICollection<PdfName> Keys => entries.Select(x => x.Key).WhereNotNull().ToList();
        public ICollection<object?> Values => entries.Where(x => x.Key is not null).Select(x => x.Value).ToList();

        /// <summary>
        /// Id, if this dictionary is an indirect object.
        /// </summary>
        public PdfObjectId Id { get; private set; }

        /// <summary>
        /// The content stream if this is an indirect object. Otherwise, <c>null</c>.
        /// </summary>
        public PdfStream? Stream { get; private set; }

        public int Count => count;
        public int Capacity => 1 << (Int32Bits - bucketShift);

        bool ICollection<KeyValuePair<PdfName, object?>>.IsReadOnly => false;

        private string DebugView
        {
            get
            {
                var id = Id.IsEmpty ? "" : (Id + " ");

                if (Count == 0)
                {
                    return id + "<< empty >>";
                }

                if (this.TryGetName(Names.Type, out var name))
                {
                    return $"{id}<< /Type: {name} ... >>";
                }

                return $"{id}<< Count: {Count} >>";
            }
        }

        internal void MakeIndirectObject(PdfObjectId id, PdfStream? stream)
        {
            Id = id;
            Stream = stream;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static uint GetBucket(int hashCode, int bucketShift)
        {
            // Fibonacci hashing
            return unchecked((uint)hashCode * HashMultiplier) >> bucketShift;
        }

        public void Add(PdfName key, object? value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var indexes = this.indexes;
            var entries = this.entries;

            var hashCode = key.GetHashCode();

            // Update existing entry
            if (indexes.Length > 0)
            {
                var bucket = GetBucket(hashCode, bucketShift);
                var cursor = indexes[bucket] - 1;
                var iterations = 0;

                while (cursor >= 0)
                {
                    ref var entry = ref entries[cursor];

                    if (entry.HashCode == hashCode && entry.Key?.Value == key.Value)
                    {
                        // Match!
                        entry.Value = value;
                        return;
                    }

                    if (iterations < entries.Length)
                    {
                        cursor = entry.NextIndex;
                        iterations++;
                    }
                    else
                    {
                        ThrowCorruptDictionary();
                    }
                }
            }

            // Ensure capacity
            var newIndex = entryCount;
            if (newIndex == entries.Length)
            {
                EnsureCapacity(count + 1);
                indexes = this.indexes;
                newIndex = entryCount; // Might have changed after entry array is compacted
            }

            // Add new entry
            {
                var bucket = GetBucket(hashCode, bucketShift);

                ref var head = ref indexes[bucket];
                this.entries[newIndex] = new Entry(key, value, hashCode, head - 1);

                var newCount = newIndex + 1;
                head = newCount;
                entryCount = newCount;
                count++;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureCapacity(int expectedCapacity)
        {
            var oldIndexes = this.indexes;
            var oldEntries = this.entries;

            // Decide capacity
            const int Padding = 8;

            var oldCapacityLog2 = Int32Bits - bucketShift;
            var newCapacityLog2 = count + Padding < oldEntries.Length
                ? oldCapacityLog2
                : MathUtils.Clamp(oldCapacityLog2 + 1, InitialCapacityLog2, MaximumCapacityLog2);
            var newCapacity = 1 << newCapacityLog2;

            // This loop will almost never run, capacity is normally doubled on each call
            while (newCapacity < expectedCapacity)
            {
                if (newCapacityLog2 >= MaximumCapacityLog2)
                {
                    throw new InvalidOperationException(
                        "The " + nameof(PdfDictionary) + " has reached its maximum capacity");
                }

                newCapacityLog2++;
                newCapacity <<= 1;
            }

            // Init new arrays
            int[] newIndexes;
            Entry[] newEntries;

            if (oldIndexes.Length == newCapacity)
            {
                newIndexes = oldIndexes;
                Array.Clear(newIndexes, 0, newIndexes.Length);
            }
            else
            {
                newIndexes = new int[newCapacity];
            }

            if (oldEntries.Length == newCapacity)
            {
                newEntries = oldEntries;
            }
            else
            {
                newEntries = new Entry[newCapacity];
            }

            // Copy entries
            var oldCursor = 0;
            var newCursor = 0;

            for (; oldCursor < oldEntries.Length; oldCursor++)
            {
                ref var oldEntry = ref oldEntries[oldCursor];

                // Null means entry is removed
                if (oldEntry.Key is not null)
                {
                    var bucket = GetBucket(oldEntry.HashCode, Int32Bits - newCapacityLog2);

                    ref var head = ref newIndexes[bucket];
                    newEntries[newCursor++] = new Entry(oldEntry.Key, oldEntry.Value, oldEntry.HashCode, head - 1);
                    head = newCursor;
                }
            }

            // If old array is reused, ensure free fields are cleared to release any GC references
            if (ReferenceEquals(oldEntries, newEntries) && newCursor < oldEntries.Length)
            {
                Array.Clear(newEntries, newCursor, oldEntries.Length - newCursor);
            }

            this.indexes = newIndexes;
            this.entries = newEntries;
            this.entryCount = newCursor;
            this.bucketShift = Int32Bits - newCapacityLog2;
        }

        public void Add(KeyValuePair<PdfName, object?> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            if (entryCount > 0)
            {
                Array.Clear(indexes, 0, indexes.Length);
                Array.Clear(entries, 0, entryCount);
            }

            count = 0;
            entryCount = 0;
        }

        public bool Contains(KeyValuePair<PdfName, object?> item)
        {
            return TryGetValue(item.Key, out var value) && Equals(value, item.Value);
        }

        public bool ContainsKey(PdfName key)
        {
            return TryGetValue(key, out _);
        }

        public bool Remove(PdfName key) => Remove(key, null, ignoreValue: true);
        public bool Remove(KeyValuePair<PdfName, object?> item) => Remove(item.Key, item.Value, ignoreValue: false);

        private bool Remove(PdfName key, object? value, bool ignoreValue)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var hashCode = key.GetHashCode();

            var indexes = this.indexes;
            if (indexes.Length > 0)
            {
                var entries = this.entries;

                var bucket = GetBucket(hashCode, bucketShift);
                var cursor = indexes[bucket] - 1;
                var iterations = 0;

                while (cursor >= 0)
                {
                    ref var entry = ref entries[cursor];

                    if (entry.HashCode == hashCode && entry.Key?.Value == key.Value)
                    {
                        // Match on key
                        if (ignoreValue || Equals(value, entry.Value))
                        {
                            // Match on key and value
                            entry.Key = null;
                            entry.Value = null;
                            count--;
                            return true;
                        }
                        else
                        {
                            // Value not matching
                            // No point in keep looking since multiple entries cannot share the same key
                            return false;
                        }
                    }

                    // Detect concurrency corruptions
                    if (iterations < entries.Length)
                    {
                        cursor = entry.NextIndex;
                        iterations++;
                    }
                    else
                    {
                        ThrowCorruptDictionary();
                    }
                }
            }

            return false;
        }

        public bool TryGetValue(PdfName key, out object? value)
        {
            if (key is not null)
            {
                var hashCode = key.GetHashCode();

                var indexes = this.indexes;
                if (indexes.Length > 0)
                {
                    var entries = this.entries;

                    var bucket = GetBucket(hashCode, bucketShift);
                    var cursor = indexes[bucket] - 1;
                    var iterations = 0;

                    while (cursor >= 0 && iterations < entries.Length)
                    {
                        ref var entry = ref entries[cursor];

                        if (entry.HashCode == hashCode && entry.Key?.Value == key.Value)
                        {
                            // Match!
                            value = entry.Value;
                            return true;
                        }

                        cursor = entry.NextIndex;
                        iterations++;
                    }
                }
            }

            value = default;
            return false;
        }

        void ICollection<KeyValuePair<PdfName, object?>>.CopyTo(KeyValuePair<PdfName, object?>[] array, int arrayIndex)
        {
            var entries = this.entries;
            var entryCount = this.entryCount;

            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (array.Length < count)
            {
                throw new ArgumentException("Array is too short", nameof(array));
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            if (arrayIndex > array.Length - this.count)
            {
                throw new ArgumentException("Array start index is too large", nameof(arrayIndex));
            }

            for (var i = 0; i < entryCount && i < entries.Length; i++)
            {
                ref var entry = ref entries[i];
                if (entry.Key is not null)
                {
                    array[arrayIndex++] = new KeyValuePair<PdfName, object?>(entry.Key, entry.Value);
                }
            }
        }

        private static void ThrowCorruptDictionary()
        {
            throw new InvalidOperationException(
                "The " + nameof(PdfDictionary) + " is corrupt, likely due to concurrent usage");
        }

        public IEnumerator<KeyValuePair<PdfName, object?>> GetEnumerator()
        {
            var entries = this.entries;
            var entryCount = this.entryCount;

            for (var i = 0; i < entryCount && i < entries.Length; i++)
            {
                var entry = entries[i];

                if (entry.Key is not null)
                {
                    yield return new KeyValuePair<PdfName, object?>(entry.Key, entry.Value);
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
