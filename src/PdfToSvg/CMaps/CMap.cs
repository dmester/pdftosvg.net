// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.CMaps
{
    internal abstract class CMap
    {
        public virtual bool IsUnicodeCMap => false;

        public abstract CMapCharCode GetCharCode(PdfString str, int offset);

        public abstract IEnumerable<uint> GetCharCodes(uint cid);

        public abstract uint? GetCid(uint charCode);
        public abstract uint? GetNotDef(uint charCode);

        public static CMap OneByteIdentity => new OneByteIdentityCMap();
        public static CMap TwoByteIdentity => new TwoByteIdentityCMap();

        public static CMap Create(CMapData cmapData) => new CustomCMap(cmapData);
        public static CMap? Create(PdfName name, CancellationToken cancellationToken) => PredefinedCMaps.Get(name.Value, cancellationToken);
        public static CMap? Create(string name, CancellationToken cancellationToken) => PredefinedCMaps.Get(name, cancellationToken);

        public static CMap? Create(PdfDictionary cmapDict, CancellationToken cancellationToken)
        {
            if (cmapDict.Stream == null)
            {
                return null;
            }

            var cmapData = CMapParser.Parse(cmapDict.Stream, cancellationToken);
            if (cmapData == null)
            {
                return null;
            }

            CMap? parent = null;
            if (cmapData.UseCMap != null)
            {
                parent = Create(cmapData.UseCMap, cancellationToken);
            }

            return new CustomCMap(cmapData, parent);
        }

        public static CMap? Create(object definition, CancellationToken cancellationToken)
        {
            if (definition is PdfName name)
            {
                return Create(name, cancellationToken);
            }
            else if (definition is string strName)
            {
                return Create(strName, cancellationToken);
            }
            else if (definition is PdfDictionary dict)
            {
                return Create(dict, cancellationToken);
            }
            else
            {
                return null;
            }
        }


    }
}
