// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Implementation of <see cref="IGrouping{TKey, TElement}"/> used by <see cref="PartitionBy{TKey, TElement}(IEnumerable{TElement}, Func{TElement, TKey})"/>.
        /// </summary>
        private class PartitionGroup<TKey, TElement> : ReadOnlyCollection<TElement>, IGrouping<TKey, TElement>
        {
            // The class implements IList<TElement> to improve performance of Linq methods on the returned grouping.

            public PartitionGroup(TKey key) : base(new List<TElement>())
            {
                Key = key;
            }

            public void AddItem(TElement el)
            {
                Items.Add(el);
            }

            public TKey Key { get; }
        }

        /// <inheritdoc cref="PartitionBy{TKey, TElement}(IEnumerable{TElement}, Func{TElement, TKey}, IEqualityComparer{TKey})"/>
        public static IEnumerable<IGrouping<TKey, TElement>> PartitionBy<TKey, TElement>(this IEnumerable<TElement> source, Func<TElement, TKey> keySelector)
        {
            return PartitionBy(source, keySelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Groups consecutive elements from <paramref name="source"/> into partitions by a key.
        /// </summary>
        /// <typeparam name="TKey">Type of the key the elements will be partitioned by.</typeparam>
        /// <typeparam name="TElement">Elements to be partitioned.</typeparam>
        /// <param name="source">The source enumerable from which elements will be partitioned.</param>
        /// <param name="keySelector">Selector returing a key by which elements will be partitioned.</param>
        /// <param name="comparer">Used for comparing whether two keys are equal. If they are, their associated elements will be added to the same partition.</param>
        /// <returns>
        ///     Enumeration of partitions. The partitions, and the elements within each partition,
        ///     are returned in the same order they appeared in <paramref name="source"/>.
        /// </returns>
        public static IEnumerable<IGrouping<TKey, TElement>> PartitionBy<TKey, TElement>(this IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var group = new PartitionGroup<TKey, TElement>(keySelector(enumerator.Current));
                    group.AddItem(enumerator.Current);

                    while (enumerator.MoveNext())
                    {
                        var key = keySelector(enumerator.Current);

                        if (!comparer.Equals(key, group.Key))
                        {
                            yield return group;
                            group = new PartitionGroup<TKey, TElement>(key);
                        }

                        group.AddItem(enumerator.Current);
                    }

                    yield return group;
                }
            }
        }

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
        {
            return DistinctBy(source, selector, EqualityComparer<TKey>.Default);
        }

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, IEqualityComparer<TKey> comparer)
        {
            var keys = new HashSet<TKey>(comparer);

            foreach (var item in source)
            {
                var key = selector(item);
                if (keys.Add(key))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Selects only elements that are not null.
        /// </summary>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
        {
            return source.Where(x => x != null)!;
        }

        /// <summary>
        /// Selects only elements that are not null.
        /// </summary>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
        {
            return source.Where(x => x.HasValue).Select(x => x!.Value);
        }
    }
}
