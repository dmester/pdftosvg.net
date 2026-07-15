// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    /// <summary>
    /// Inline list optimized for storing <see cref="Coding.JpxCodeBlockSegment"/> in <see cref="JpxCodeBlock"/>
    /// without allocating thousands of heap objects.
    /// </summary>
    internal struct JpxInlineList<T> : IEnumerable<T>
    {
        private const int InitialCapacity = 10;
        private T headItem;
        private T[]? tailItems;
        private int count;

        public int Count => count;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (index == 0)
                {
                    return headItem;
                }
                else
                {
                    return tailItems![index - 1];
                }
            }
        }

        public void Add(T item)
        {
            if (count == 0)
            {
                headItem = item;
                count = 1;
            }
            else
            {
                if (tailItems == null)
                {
                    tailItems = new T[InitialCapacity];
                }
                else if (count - 1 >= tailItems.Length)
                {
                    var newItems = new T[tailItems.Length * 2];
                    Array.Copy(tailItems, newItems, count - 1);
                    tailItems = newItems;
                }

                tailItems[count++ - 1] = item;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            var tailItems = this.tailItems;
            var count = this.count;

            if (count > 0)
            {
                yield return this.headItem;

                if (tailItems != null)
                {
                    var tailCount = count - 1;

                    for (var i = 0; i < tailCount; i++)
                    {
                        yield return tailItems[i];
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
