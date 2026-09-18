// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToSvg.SourceGenerators.Collections
{
    // The idea for this is to generate dictionaries containing static data at build time instead of in runtime.
    // The difference is not enormous, but it can lower library boot time a little.
    internal static class StringHashTableGenerator
    {
        private const int EntriesInHashCode = 3;
        private const uint InitialMultiplier = 0x9E3779B9u; // Golden Ratio
        private const uint MultiplierStep = 0x3C6EF372u;
        private const int CandidateCount = 256;

        public static Table<T> CreateTable<T>(IReadOnlyCollection<SourceEntry<T>> sourceEntries)
        {
            var exponent = 1;

            while ((1 << exponent) < sourceEntries.Count)
            {
                exponent++;
            }

            var bucketCount = 1 << exponent;
            var shiftCount = 32 - exponent;
            var entries = CreateEntries(sourceEntries);
            var multiplier = FindMultiplier(entries, bucketCount, shiftCount);

            return CreateTable(entries, bucketCount, multiplier, shiftCount);
        }

        public static Table<T> CreateTable<T>(
            IReadOnlyCollection<SourceEntry<T>> sourceEntries,
            int bucketCount,
            uint multiplier,
            int shiftCount)
        {
            return CreateTable(CreateEntries(sourceEntries), bucketCount, multiplier, shiftCount);
        }

        private static List<TableEntry<T>> CreateEntries<T>(IEnumerable<SourceEntry<T>> sourceEntries)
        {
            return sourceEntries
                .Select(entry => new TableEntry<T>(entry.Key, GetHashCode(entry.Key), entry.Value))
                .ToList();
        }

        private static Table<T> CreateTable<T>(
            IReadOnlyCollection<TableEntry<T>> sourceEntries,
            int bucketCount,
            uint multiplier,
            int shiftCount)
        {
            var sortedEntries = sourceEntries
                .Select(entry => new BucketEntry<T>(GetBucket(entry.HashCode, multiplier, shiftCount), entry))
                .OrderBy(entry => entry.Bucket)
                .ToList();
            var indexes = new uint[bucketCount + 1];
            var entries = new TableEntry<T>[sourceEntries.Count];
            var bucketCursor = 0;
            var entryCursor = 0;

            foreach (var entry in sortedEntries)
            {
                while (bucketCursor < entry.Bucket)
                {
                    bucketCursor++;
                    indexes[bucketCursor] = (uint)entryCursor;
                }

                entries[entryCursor++] = entry.Entry;
            }

            while (bucketCursor < indexes.Length - 1)
            {
                bucketCursor++;
                indexes[bucketCursor] = (uint)entryCursor;
            }

            return new Table<T>(multiplier, shiftCount, indexes, entries);
        }

        private static uint FindMultiplier<T>(
            IReadOnlyCollection<TableEntry<T>> entries,
            int bucketCount,
            int shiftCount)
        {
            var bestMaxChain = int.MaxValue;
            var bestTotalProbes = long.MaxValue;
            var bestMultiplier = 0u;
            var bucketCounts = new int[bucketCount];

            for (var candidateIndex = 0; candidateIndex < CandidateCount && bestMaxChain > 1; candidateIndex++)
            {
                var multiplier = unchecked(InitialMultiplier + (uint)candidateIndex * MultiplierStep);

                Array.Clear(bucketCounts, 0, bucketCounts.Length);

                foreach (var entry in entries)
                {
                    bucketCounts[GetBucket(entry.HashCode, multiplier, shiftCount)]++;
                }

                var maxChain = 0;
                var totalProbes = 0L;

                foreach (var count in bucketCounts)
                {
                    maxChain = Math.Max(maxChain, count);
                    totalProbes += count * (count + 1L) / 2;
                }

                if (maxChain < bestMaxChain ||
                    maxChain == bestMaxChain && totalProbes < bestTotalProbes)
                {
                    bestMaxChain = maxChain;
                    bestTotalProbes = totalProbes;
                    bestMultiplier = multiplier;
                }
            }

            return bestMultiplier;
        }

        private static int GetHashCode(string value)
        {
            var hashCode = value.Length;

            for (var i = 0; i < value.Length && i < EntriesInHashCode; i++)
            {
                hashCode = unchecked((hashCode << 8) | value[i]);
            }

            return hashCode;
        }

        private static int GetBucket(int hashCode, uint multiplier, int shiftCount)
        {
            return (int)(unchecked((uint)hashCode * multiplier) >> shiftCount);
        }

        public static void GenerateIndexes(
            SourceWriter writer,
            string fieldName,
            IReadOnlyCollection<uint> indexes)
        {
            writer.WriteLine($"private static readonly uint[] {fieldName} =");

            using (writer.BeginBlock(";"))
            {
                writer.WriteLine(string.Join(", ", indexes) + ",");
            }
        }

        public sealed class SourceEntry<T>(string key, T value)
        {
            public string Key { get; } = key;
            public T Value { get; } = value;
        }

        public sealed class TableEntry<T>(string key, int hashCode, T value)
        {
            public string Key { get; } = key;
            public int HashCode { get; } = hashCode;
            public T Value { get; } = value;
        }

        public sealed class Table<T>(uint multiplier, int shiftCount, uint[] indexes, TableEntry<T>[] entries)
        {
            public uint Multiplier { get; } = multiplier;
            public int ShiftCount { get; } = shiftCount;
            public uint[] Indexes { get; } = indexes;
            public TableEntry<T>[] Entries { get; } = entries;
            public int BucketCount => Indexes.Length - 1;
        }

        private sealed class BucketEntry<T>(int bucket, TableEntry<T> entry)
        {
            public int Bucket { get; } = bucket;
            public TableEntry<T> Entry { get; } = entry;
        }
    }
}
