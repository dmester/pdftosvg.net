// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Coding
{
    internal struct JbigArithmeticContextEntry
    {
        public byte Index;
        public bool Mps;
    }

    internal class JbigArithmeticContext
    {
        private JbigArithmeticContextEntry[] entries;
        private int index;

        public int EntryIndex
        {
            get => index;
            set
            {
                if (value < 0 || value >= entries.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(EntryIndex));
                }

                index = value;
            }
        }

        public int Size => entries.Length;

        public ref JbigArithmeticContextEntry Current => ref entries[index];

        public JbigArithmeticContext(int size)
        {
            entries = new JbigArithmeticContextEntry[size];
        }

        public void EnsureSize(int size)
        {
            if (entries.Length < size)
            {
                var oldEntries = entries;
                entries = new JbigArithmeticContextEntry[size];
                Array.Copy(oldEntries, 0, entries, 0, oldEntries.Length);
            }
        }
    }
}
