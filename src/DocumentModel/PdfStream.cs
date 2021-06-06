// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Filters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    [DebuggerTypeProxy(typeof(PdfStreamDebugProxy))]
    internal abstract class PdfStream
    {
        protected readonly PdfDictionary owner;
        private IReadOnlyList<PdfStreamFilter>? filters;

        public PdfStream(PdfDictionary owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Gets a list of the filters to be applied on the content of this stream.
        /// </summary>
        public IReadOnlyList<PdfStreamFilter> Filters
        {
            get => filters ?? (filters = GetFilters());
        }

        /// <summary>
        /// Opens the raw, potentially encoded, stream.
        /// </summary>
        public abstract Stream Open();

        /// <summary>
        /// Opens the raw, potentially encoded, stream.
        /// </summary>
        public abstract Task<Stream> OpenAsync();

        /// <summary>
        /// Opens the stream with all the decode filters applied.
        /// </summary>
        public Stream OpenDecoded()
        {
            return Filters.Decode(Open());
        }

        /// <summary>
        /// Opens the stream with all the decode filters applied.
        /// </summary>
        public async Task<Stream> OpenDecodedAsync()
        {
            return Filters.Decode(await OpenAsync());
        }

        private static object[] GetAsArray(PdfDictionary dict, PdfName key)
        {
            var value = dict[key];

            if (value == null)
            {
                return ArrayUtils.Empty<object>();
            }

            if (value is object[] array)
            {
                return array;
            }

            return new[] { value };
        }

        private IReadOnlyList<PdfStreamFilter> GetFilters()
        {
            var filterNames = GetAsArray(owner, Names.Filter);
            var decodeParms = GetAsArray(owner, Names.DecodeParms);

            var result = new List<PdfStreamFilter>(filterNames.Length);

            for (var i = 0; i < filterNames.Length; i++)
            {
                var untypedFilterName = filterNames[i];

                if (untypedFilterName is PdfName filterName)
                {
                    var filter = Filter.ByName(filterName);
                    if (filter == null)
                    {
                        Log.WriteLine($"Unsupported filter {filterName}. Filter ignored.");
                    }
                    else
                    {
                        PdfDictionary? filterDecodeParms = null;

                        if (i < decodeParms.Length)
                        {
                            filterDecodeParms = decodeParms[i] as PdfDictionary;
                        }

                        result.Add(new PdfStreamFilter(filter, filterDecodeParms));
                    }
                }
                else if (untypedFilterName != null)
                {
                    Log.WriteLine($"Unexpected filter value type {Log.TypeOf(untypedFilterName)}.");
                }
            }

            return new ReadOnlyCollection<PdfStreamFilter>(result);
        }
    }
}
