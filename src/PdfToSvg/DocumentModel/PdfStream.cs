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
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    [DebuggerTypeProxy(typeof(PdfStreamDebugProxy))]
    internal abstract class PdfStream
    {
        protected readonly PdfDictionary owner;
        private IList<PdfStreamFilter>? filters;
        private PdfDictionary? cryptDecodeParms;

        public PdfStream(PdfDictionary owner)
        {
            this.owner = owner;
        }

        internal PdfDictionary? CryptDecodeParms
        {
            get { return cryptDecodeParms; }
            set { cryptDecodeParms = value; filters = null; }
        }

        /// <summary>
        /// Gets a list of the filters to be applied on the content of this stream.
        /// </summary>
        public IList<PdfStreamFilter> Filters
        {
            get => filters ??= GetFilters();
        }

        /// <summary>
        /// Opens the raw, potentially encoded, stream.
        /// </summary>
        public abstract Stream Open(CancellationToken cancellationToken);

        /// <summary>
        /// Opens the stream with all the decode filters applied.
        /// </summary>
        public Stream OpenDecoded(CancellationToken cancellationToken)
        {
            return Filters.Decode(Open(cancellationToken));
        }

#if HAVE_ASYNC
        /// <summary>
        /// Opens the raw, potentially encoded, stream.
        /// </summary>
        public abstract Task<Stream> OpenAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Opens the stream with all the decode filters applied.
        /// </summary>
        public async Task<Stream> OpenDecodedAsync(CancellationToken cancellationToken)
        {
            return Filters.Decode(await OpenAsync(cancellationToken).ConfigureAwait(false));
        }
#endif

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

        private IList<PdfStreamFilter> GetFilters()
        {
            var filterNames = GetAsArray(owner, Names.Filter);
            var decodeParms = GetAsArray(owner, Names.DecodeParms);

            var result = new List<PdfStreamFilter>(filterNames.Length + 1);
            var hasCrypt = false;

            for (var i = 0; i < filterNames.Length; i++)
            {
                var untypedFilterName = filterNames[i];

                if (untypedFilterName is PdfName filterName)
                {
                    var filter = Filter.ByName(filterName);
                    PdfDictionary? filterDecodeParms = null;

                    if (i < decodeParms.Length)
                    {
                        filterDecodeParms = decodeParms[i] as PdfDictionary;
                    }

                    if (filterName == Names.Crypt)
                    {
                        hasCrypt = true;

                        if (cryptDecodeParms != null)
                        {
                            if (filterDecodeParms == null)
                            {
                                filterDecodeParms = new PdfDictionary();
                            }

                            foreach (var prop in cryptDecodeParms)
                            {
                                filterDecodeParms[prop.Key] = prop.Value;
                            }
                        }
                    }

                    result.Add(new PdfStreamFilter(filter, filterDecodeParms));
                }
                else if (untypedFilterName != null)
                {
                    Log.WriteLine($"Unexpected filter value type {Log.TypeOf(untypedFilterName)}.");
                }
            }

            if (!hasCrypt && cryptDecodeParms != null)
            {
                result.Insert(0, new PdfStreamFilter(Filter.Crypt, cryptDecodeParms));
            }

            return new ReadOnlyCollection<PdfStreamFilter>(result);
        }
    }
}
