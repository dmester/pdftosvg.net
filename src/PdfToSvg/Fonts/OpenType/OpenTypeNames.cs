// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal class OpenTypeNames
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

        private string? GetName(OpenTypeNameID id)
        {
            return tables
                .OfType<NameTable>()
                .Take(1)
                .SelectMany(table => table.NameRecords)

                .Where(name => name.NameID == id)

                // Prefer Windows and English
                .OrderBy(name => name.PlatformID == OpenTypePlatformID.Windows ? 0 : 1)
                .ThenBy(name => name.LanguageID == 1033 ? 0 : 1)

                .Select(name =>
                {
                    var encoding = name.PlatformID == OpenTypePlatformID.Windows ? Encoding.BigEndianUnicode : Encoding.ASCII;
                    return encoding.GetString(name.Content, 0, name.Content.Length);
                })

                .FirstOrDefault();
        }
    }
}
