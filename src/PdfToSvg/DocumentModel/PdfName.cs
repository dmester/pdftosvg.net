// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PdfToSvg.DocumentModel
{
    internal class PdfName : IEquatable<PdfName>
    {
        // Lower memory usage by interning common names
        private static Dictionary<string, PdfName> knownNames = typeof(Names)
            .GetTypeInfo()
            .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty)
            .Select(x => x.GetValue(null, null) as PdfName)
            .WhereNotNull()
            .ToDictionary(x => x.Value, x => x);

        public PdfName(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static PdfName Create(string value)
        {
            return knownNames.TryGetValue(value, out var knownName) ? knownName : new PdfName(value);
        }

        public static PdfNamePath operator /(PdfName name1, PdfName name2)
        {
            return new PdfNamePath(name1, name2);
        }

        public static bool operator ==(PdfName? a, PdfName? b)
        {
            if ((object?)a == null) return (object?)b == null;
            if ((object?)b == null) return false;
            return a.Value == b.Value;
        }

        public static bool operator !=(PdfName? a, PdfName? b) => !(a == b);

        public override bool Equals(object? obj)
        {
            return Equals(obj as PdfName);
        }

        public bool Equals(PdfName? other)
        {
            return other != null && other.Value == Value;
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        public override string ToString()
        {
            return "/" + Value;
        }
    }
}
