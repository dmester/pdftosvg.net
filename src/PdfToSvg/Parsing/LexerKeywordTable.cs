// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Collections;

namespace PdfToSvg.Parsing
{
    internal class LexerKeywordTable
    {
        private static readonly uint[] emptyEntryIndexes = new uint[3];
        private static readonly StringHashTable.Entry<LexerKeyword>[] emptyKeywordEntries = [];
        private static readonly StringHashTable.Entry<int>[] emptyMaxLengthEntries = [];

        private readonly uint[] fullEntryIndexes;
        private readonly uint[] maxLengthEntryIndexes;
        private readonly StringHashTable.Entry<LexerKeyword>[] fullEntries;
        private readonly StringHashTable.Entry<int>[] maxLengthEntries;
        private readonly uint multiplier;
        private readonly int shiftCount;

        public LexerKeywordTable()
            : this(
                multiplier: 1,
                shiftCount: 31,
                fullEntryIndexes: emptyEntryIndexes,
                fullEntries: emptyKeywordEntries,
                maxLengthEntryIndexes: emptyEntryIndexes,
                maxLengthEntries: emptyMaxLengthEntries)
        {
        }

        protected LexerKeywordTable(
            uint multiplier,
            int shiftCount,
            uint[] fullEntryIndexes,
            StringHashTable.Entry<LexerKeyword>[] fullEntries,
            uint[] maxLengthEntryIndexes,
            StringHashTable.Entry<int>[] maxLengthEntries)
        {
            this.multiplier = multiplier;
            this.shiftCount = shiftCount;
            this.fullEntryIndexes = fullEntryIndexes;
            this.fullEntries = fullEntries;
            this.maxLengthEntryIndexes = maxLengthEntryIndexes;
            this.maxLengthEntries = maxLengthEntries;
        }

        public static bool IsKeywordChar(char ch)
        {
            return
                PdfCharacters.IsLetter(ch) ||
                ch == '*' ||
                ch == '\'' ||
                ch == '"';
        }

        public bool TryGetKeyword(in LexerBuffer buffer, out LexerKeyword keyword)
        {
            return StringHashTable.TryLookup(
                buffer.Buffer, buffer.Length,
                fullEntryIndexes, fullEntries,
                multiplier, shiftCount,
                out keyword);
        }

        public bool TryGetMaxLength(in LexerBuffer buffer, out int maxLength)
        {
            return StringHashTable.TryLookup(
                buffer.Buffer, buffer.Length,
                maxLengthEntryIndexes, maxLengthEntries,
                multiplier, shiftCount,
                out maxLength);
        }
    }
}
