// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace PdfToSvg.Imaging.Jpx.Coding
{
    internal struct JpxTier1CoefficientState
    {
        // Significance of the eight neighbours. A bit is set when that neighbour of this coefficient is
        // significant.
        private const int SignificantW = 1 << 0;
        private const int SignificantE = 1 << 1;
        private const int SignificantN = 1 << 2;
        private const int SignificantS = 1 << 3;
        private const int SignificantNW = 1 << 4;
        private const int SignificantNE = 1 << 5;
        private const int SignificantSW = 1 << 6;
        private const int SignificantSE = 1 << 7;

        // Signs of the four horizontal/vertical neighbours. A bit is set when that neighbour is significant and
        // negative.
        private const int NegativeW = 1 << 8;
        private const int NegativeE = 1 << 9;
        private const int NegativeN = 1 << 10;
        private const int NegativeS = 1 << 11;

        // Coding state of the coefficient itself
        private const int SignificantSelf = 1 << 12;
        private const int Visited = 1 << 13;
        private const int Refined = 1 << 14;

        private const int SignificantNeighborsMask = 0xff;
        private const int SignContextMask = 0xfff;

        public const int SignificanceContextIndexCount = SignificantNeighborsMask + 1;

        public const int SignContextIndexCount = SignContextMask + 1;

        private ushort value;

        public JpxTier1CoefficientState(ushort value)
        {
            this.value = value;
        }

        public bool IsSignificant => (value & SignificantSelf) != 0;

        /// <summary>
        /// The coefficient is still insignificant, and its bit of the current bit-plane has not been decoded by the
        /// significance propagation pass, so its significance remains to be coded.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Sections D.3.1 and D.3.4
        /// </remarks>
        public bool IsUndecoded => (value & (SignificantSelf | Visited)) == 0;

        /// <summary>
        /// The coefficient takes part in the magnitude refinement pass: it is significant, but did not just become
        /// significant in the current bit-plane's significance propagation pass.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Section D.3.3
        /// </remarks>
        public bool NeedsRefinement => (value & (SignificantSelf | Visited)) == SignificantSelf;

        /// <summary>The coefficient has been refined in an earlier magnitude refinement pass (Table D.4).</summary>
        public bool IsRefined => (value & Refined) != 0;

        public bool HasSignificantNeighbors => (value & SignificantNeighborsMask) != 0;

        /// <summary>
        /// Index into a significance coding context table of <see cref="JpxTier1LookupTables"/>, formed by the
        /// significance of the eight neighbours.
        /// </summary>
        public int SignificanceContextIndex => value & SignificantNeighborsMask;

        /// <summary>
        /// Index into the sign coding context tables of <see cref="JpxTier1LookupTables"/>, formed by the significance
        /// and signs of the neighbours.
        /// </summary>
        public int SignContextIndex => value & SignContextMask;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkSignificant()
        {
            value |= SignificantSelf;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkVisited()
        {
            value |= Visited;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void ClearVisited()
        {
            value = (ushort)(value & ~Visited);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkRefined()
        {
            value |= Refined;
        }

        // Records that a neighbour of this coefficient became significant, with the decoded sign bit
        // (1 = negative) for the horizontal/vertical neighbours whose signs contribute to the sign coding contexts
        // (ITU-T T.800 (06/2019) Table D.2)

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkWestNeighborSignificant(int negative)
        {
            value |= (ushort)(SignificantW | (NegativeW * negative));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkEastNeighborSignificant(int negative)
        {
            value |= (ushort)(SignificantE | (NegativeE * negative));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkNorthNeighborSignificant(int negative)
        {
            value |= (ushort)(SignificantN | (NegativeN * negative));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkSouthNeighborSignificant(int negative)
        {
            value |= (ushort)(SignificantS | (NegativeS * negative));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkNorthWestNeighborSignificant()
        {
            value |= SignificantNW;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkNorthEastNeighborSignificant()
        {
            value |= SignificantNE;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkSouthWestNeighborSignificant()
        {
            value |= SignificantSW;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void MarkSouthEastNeighborSignificant()
        {
            value |= SignificantSE;
        }

        /// <summary>
        /// Union of the states of two coefficients, letting e.g., <see cref="IsUndecoded"/> and
        /// <see cref="HasSignificantNeighbors"/> be evaluated for a whole column at once.
        /// </summary>
        public static JpxTier1CoefficientState operator |(JpxTier1CoefficientState a, JpxTier1CoefficientState b)
        {
            return new JpxTier1CoefficientState((ushort)(a.value | b.value));
        }

        // Per-neighbour states, used when building the context lookup tables

        public bool IsWestNeighborSignificant => (value & SignificantW) != 0;
        public bool IsEastNeighborSignificant => (value & SignificantE) != 0;
        public bool IsNorthNeighborSignificant => (value & SignificantN) != 0;
        public bool IsSouthNeighborSignificant => (value & SignificantS) != 0;

        public bool IsWestNeighborNegative => (value & NegativeW) != 0;
        public bool IsEastNeighborNegative => (value & NegativeE) != 0;
        public bool IsNorthNeighborNegative => (value & NegativeN) != 0;
        public bool IsSouthNeighborNegative => (value & NegativeS) != 0;

        /// <summary>Number of significant horizontal neighbours (ΣH of ITU-T T.800 (06/2019) Table D.1).</summary>
        public int SignificantHorizontalCount => CountBits(value & (SignificantW | SignificantE));

        /// <summary>Number of significant vertical neighbours (ΣV of ITU-T T.800 (06/2019) Table D.1).</summary>
        public int SignificantVerticalCount => CountBits(value & (SignificantN | SignificantS));

        /// <summary>Number of significant diagonal neighbours (ΣD of ITU-T T.800 (06/2019) Table D.1).</summary>
        public int SignificantDiagonalCount =>
            CountBits(value & (SignificantNW | SignificantNE | SignificantSW | SignificantSE));

        private static int CountBits(int bits)
        {
            var count = 0;

            while (bits != 0)
            {
                bits &= bits - 1;
                count++;
            }

            return count;
        }
    }
}
