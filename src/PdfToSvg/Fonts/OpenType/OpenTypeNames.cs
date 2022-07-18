// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

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
        private readonly IEnumerable<IBaseTable> tables;

        public OpenTypeNames(IEnumerable<IBaseTable> tables)
        {
            this.tables = tables;
        }

        public string? FontFamily => GetName(OpenTypeNameID.FontFamily);
        public string? FontSubfamily => GetName(OpenTypeNameID.FontSubfamily);
        public string? Copyright => GetName(OpenTypeNameID.Copyright);
        public string? UniqueId => GetName(OpenTypeNameID.UniqueId);
        public string? FullFontName => GetName(OpenTypeNameID.FullFontName);
        public string? Version => GetName(OpenTypeNameID.Version);
        public string? PostScriptName => GetName(OpenTypeNameID.PostScriptName);
        public string? Trademark => GetName(OpenTypeNameID.Trademark);
        public string? Manufacturer => GetName(OpenTypeNameID.Manufacturer);
        public string? Designer => GetName(OpenTypeNameID.Designer);
        public string? Description => GetName(OpenTypeNameID.Description);
        public string? VendorUrl => GetName(OpenTypeNameID.VendorUrl);
        public string? DesignerUrl => GetName(OpenTypeNameID.DesignerUrl);
        public string? License => GetName(OpenTypeNameID.License);
        public string? LicenseUrl => GetName(OpenTypeNameID.LicenseUrl);

        public IEnumerator<KeyValuePair<OpenTypeNameID, string>> GetEnumerator()
        {
            return Enumerate(rec => rec.Content.Length > 0).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<KeyValuePair<OpenTypeNameID, string>> Enumerate(Func<NameRecord, bool> predicate)
        {
            return tables
                .OfType<NameTable>()
                .SelectMany(name => name.NameRecords)
                .Where(predicate)

                .GroupBy(rec => rec.NameID)
                .Select(group => group

                    // Prefer Windows and English
                    .OrderBy(name => name.PlatformID == OpenTypePlatformID.Windows ? 0 : 1)
                    .ThenBy(name => name.LanguageID == 1033 ? 0 : 1)
                    .First())

                .Select(rec =>
                {
                    var isWindows = rec.PlatformID == OpenTypePlatformID.Windows;
                    var encoding = isWindows ? Encoding.BigEndianUnicode : Encoding.ASCII;

                    return new KeyValuePair<OpenTypeNameID, string>(
                        rec.NameID,
                        encoding.GetString(rec.Content));
                });
        }

        private string? GetName(OpenTypeNameID id)
        {
            return Enumerate(name => name.NameID == id)
                .Select(x => x.Value)
                .FirstOrDefault();
        }
    }
}
