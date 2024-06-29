// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace PdfToSvg.CMaps
{
    internal static class PredefinedCMaps
    {
        private static readonly Dictionary<string, CMap> cmapCache = new Dictionary<string, CMap>();
        private static readonly Dictionary<string, UnicodeMap> unicodeCache = new Dictionary<string, UnicodeMap>();
        private static CMapPack? pack;

        // PDF 1.7, Table 118
        private static readonly HashSet<string> names = new HashSet<string>
        {
            "GB-EUC-H",
            "GB-EUC-V",
            "GBpc-EUC-H",
            "GBpc-EUC-V",
            "GBK-EUC-H",
            "GBK-EUC-V",
            "GBKp-EUC-H",
            "GBKp-EUC-V",
            "GBK2K-H",
            "GBK2K-V",
            "UniGB-UCS2-H",
            "UniGB-UCS2-V",
            "UniGB-UTF16-H",
            "UniGB-UTF16-V",
            "B5pc-H",
            "B5pc-V",
            "HKscs-B5-H",
            "HKscs-B5-V",
            "ETen-B5-H",
            "ETen-B5-V",
            "ETenms-B5-H",
            "ETenms-B5-V",
            "CNS-EUC-H",
            "CNS-EUC-V",
            "UniCNS-UCS2-H",
            "UniCNS-UCS2-V",
            "UniCNS-UTF16-H",
            "UniCNS-UTF16-V",
            "83pv-RKSJ-H",
            "90ms-RKSJ-H",
            "90ms-RKSJ-V",
            "90msp-RKSJ-H",
            "90msp-RKSJ-V",
            "90pv-RKSJ-H",
            "Add-RKSJ-H",
            "Add-RKSJ-V",
            "EUC-H",
            "EUC-V",
            "Ext-RKSJ-H",
            "Ext-RKSJ-V",
            "H",
            "V",
            "UniJIS-UCS2-H",
            "UniJIS-UCS2-V",
            "UniJIS-UCS2-HW-H",
            "UniJIS-UCS2-HW-V",
            "UniJIS-UTF16-H",
            "UniJIS-UTF16-V",
            "KSC-EUC-H",
            "KSC-EUC-V",
            "KSCms-UHC-H",
            "KSCms-UHC-V",
            "KSCms-UHC-HW-H",
            "KSCms-UHC-HW-V",
            "KSCpc-EUC-H",
            "UniKS-UCS2-H",
            "UniKS-UCS2-V",
            "UniKS-UTF16-H",
            "UniKS-UTF16-V",
            "Identity-H",
            "Identity-V",
        };

        public static UnicodeMap? GetUnicodeMap(string registry, string ordering)
        {
            UnicodeMap? result;

            var name = registry + "-" + ordering + "-UCS2";

            lock (unicodeCache)
            {
                if (unicodeCache.TryGetValue(name, out result))
                {
                    return result;
                }
            }

            if (name != "Adobe-CNS1-UCS2" &&
                name != "Adobe-GB1-UCS2" &&
                name != "Adobe-Japan1-UCS2" &&
                name != "Adobe-Korea1-UCS2")
            {
                return null;
            }

            var mapData = GetPack().GetCMap(name);
            if (mapData == null)
            {
                return null;
            }

            result = UnicodeMap.Create(mapData);

            lock (unicodeCache)
            {
                unicodeCache[name] = result;
            }

            return result;
        }

        public static CMap? Get(string name, CancellationToken cancellationToken = default)
        {
            return Get(name, 0, cancellationToken);
        }

        public static bool Contains(string name) => names.Contains(name);

        private static CMapPack GetPack()
        {
            if (pack != null)
            {
                return pack;
            }

            lock (cmapCache)
            {
                if (pack == null)
                {
                    var assembly = typeof(PredefinedCMaps).GetTypeInfo().Assembly;
                    var resourceName = typeof(PredefinedCMaps).Namespace + ".PredefinedCMaps.bin";

                    using (var stream = assembly.GetManifestResourceStreamOrThrow(resourceName))
                    {
                        pack = new CMapPack(stream);
                    }
                }
            }

            return pack;
        }

        private static CMap? Get(string? name, int recursionDepth, CancellationToken cancellationToken)
        {
            if (name == null || !names.Contains(name))
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (name == "Identity-V" || name == "Identity-H")
            {
                return new TwoByteIdentityCMap();
            }

            if (recursionDepth > 10)
            {
                Log.WriteLine("Recursion depth exceeded 10 while reading predefined CMap. Ignoring more parent CMaps.");
                return null;
            }

            lock (cmapCache)
            {
                if (cmapCache.TryGetValue(name, out var cachedCMap))
                {
                    return cachedCMap;
                }
            }

            var cmapData = GetPack().GetCMap(name);
            if (cmapData == null)
            {
                return null;
            }

            var parent = Get(cmapData.UseCMap, recursionDepth + 1, cancellationToken);
            var cmap = new CustomCMap(cmapData, parent);

            lock (cmapCache)
            {
                cmapCache[name] = cmap;
            }

            return cmap;
        }
    }
}
