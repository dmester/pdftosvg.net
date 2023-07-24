// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.Common;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal class OpenTypeNames : IEnumerable<KeyValuePair<OpenTypeNameID, string>>
    {
        private readonly IList<IBaseTable> tables;
        private readonly Dictionary<OpenTypeNameID, string?> overrides = new();

        public OpenTypeNames(IList<IBaseTable> tables)
        {
            this.tables = tables;
        }

        public string? FontFamily
        {
            get => GetName(OpenTypeNameID.FontFamily);
            set => overrides[OpenTypeNameID.FontFamily] = value;
        }

        public string? FontSubfamily
        {
            get => GetName(OpenTypeNameID.FontSubfamily);
            set => overrides[OpenTypeNameID.FontSubfamily] = value;
        }

        public string? Copyright
        {
            get => GetName(OpenTypeNameID.Copyright);
            set => overrides[OpenTypeNameID.Copyright] = value;
        }

        public string? UniqueId
        {
            get => GetName(OpenTypeNameID.UniqueId);
            set => overrides[OpenTypeNameID.UniqueId] = value;
        }

        public string? FullFontName
        {
            get => GetName(OpenTypeNameID.FullFontName);
            set => overrides[OpenTypeNameID.FullFontName] = value;
        }

        public string? Version
        {
            get => GetName(OpenTypeNameID.Version);
            set => overrides[OpenTypeNameID.Version] = value;
        }

        public string? PostScriptName
        {
            get => GetName(OpenTypeNameID.PostScriptName);
            set => overrides[OpenTypeNameID.PostScriptName] = value;
        }

        public string? Trademark
        {
            get => GetName(OpenTypeNameID.Trademark);
            set => overrides[OpenTypeNameID.Trademark] = value;
        }

        public string? Manufacturer
        {
            get => GetName(OpenTypeNameID.Manufacturer);
            set => overrides[OpenTypeNameID.Manufacturer] = value;
        }

        public string? Designer
        {
            get => GetName(OpenTypeNameID.Designer);
            set => overrides[OpenTypeNameID.Designer] = value;
        }

        public string? Description
        {
            get => GetName(OpenTypeNameID.Description);
            set => overrides[OpenTypeNameID.Description] = value;
        }

        public string? VendorUrl
        {
            get => GetName(OpenTypeNameID.VendorUrl);
            set => overrides[OpenTypeNameID.VendorUrl] = value;
        }

        public string? DesignerUrl
        {
            get => GetName(OpenTypeNameID.DesignerUrl);
            set => overrides[OpenTypeNameID.DesignerUrl] = value;
        }

        public string? License
        {
            get => GetName(OpenTypeNameID.License);
            set => overrides[OpenTypeNameID.License] = value;
        }

        public string? LicenseUrl
        {
            get => GetName(OpenTypeNameID.LicenseUrl);
            set => overrides[OpenTypeNameID.LicenseUrl] = value;
        }

        public IEnumerator<KeyValuePair<OpenTypeNameID, string>> GetEnumerator() => EnumerateNames().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<KeyValuePair<OpenTypeNameID, string>> EnumerateNames()
        {
            return overrides!
                .Concat(EnumerateFontNames())
                .DistinctBy(x => x.Key)
                .Where(x => x.Value != null);
        }

        private IEnumerable<KeyValuePair<OpenTypeNameID, string>> EnumerateFontNames(OpenTypeNameID? filterId = null)
        {
            return tables
                .OfType<NameTable>()
                .Take(1)
                .SelectMany(name => name.NameRecords)
                .Where(rec => filterId == null || filterId == rec.NameID)

                // Prefer Windows and English
                .OrderBy(rec => rec.PlatformID == OpenTypePlatformID.Windows ? 0 : 1)
                .ThenBy(rec => rec.LanguageID == 1033 ? 0 : 1)
                .DistinctBy(rec => rec.NameID)

                .Select(rec =>
                {
                    var isWindows = rec.PlatformID == OpenTypePlatformID.Windows;
                    var encoding = isWindows ? Encoding.BigEndianUnicode : Encoding.ASCII;

                    return KeyValuePair.Create(
                        rec.NameID,
                        encoding.GetString(rec.Content));
                });
        }

        private string? GetName(OpenTypeNameID id)
        {
            if (overrides.TryGetValue(id, out var value))
            {
                return value;
            }
            else
            {
                return EnumerateFontNames(id)
                    .Select(x => x.Value)
                    .FirstOrDefault();
            }
        }
    }
}
