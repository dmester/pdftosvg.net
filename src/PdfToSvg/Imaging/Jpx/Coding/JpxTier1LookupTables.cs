// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;

namespace PdfToSvg.Imaging.Jpx.Coding
{
    /// <summary>
    /// Lookup tables for ITU-T T.800 (06/2019) sections D.3
    /// </summary>
    internal static class JpxTier1LookupTables
    {
        private static readonly byte[] SignificanceContextsLLAndLH = BuildSignificanceContextTable(JpxSubBandType.LL);
        private static readonly byte[] SignificanceContextsHL = BuildSignificanceContextTable(JpxSubBandType.HL);
        private static readonly byte[] SignificanceContextsHH = BuildSignificanceContextTable(JpxSubBandType.HH);

        /// <summary>
        /// Sign coding context labels and XORbit, indexed by <see cref="JpxTier1CoefficientState.SignContextIndex"/>.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Table D.3
        /// </remarks>
        public static readonly JpxTier1SignContext[] SignContextTable =
            new JpxTier1SignContext[JpxTier1CoefficientState.SignContextIndexCount];

        static JpxTier1LookupTables()
        {
            BuildSignTable();
        }

        /// <summary>
        /// Returns the significance coding context labels of one sub-band type.
        /// Indexed by <see cref="JpxTier1CoefficientState.SignificanceContextIndex"/>.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Table D.1
        /// </remarks>
        public static byte[] GetSignificanceContextTable(JpxSubBandType subBandType)
        {
            return subBandType switch
            {
                JpxSubBandType.HL => SignificanceContextsHL,
                JpxSubBandType.HH => SignificanceContextsHH,
                _ => SignificanceContextsLLAndLH,
            };
        }

        /// <summary>
        /// Builds the significance coding context lookup of one sub-band type.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Table D.1
        /// </remarks>
        private static byte[] BuildSignificanceContextTable(JpxSubBandType subBandType)
        {
            var contexts = new byte[JpxTier1CoefficientState.SignificanceContextIndexCount];

            for (var index = 0; index < contexts.Length; index++)
            {
                var state = new JpxTier1CoefficientState((ushort)index);

                var sumH = state.SignificantHorizontalCount;
                var sumV = state.SignificantVerticalCount;
                var sumD = state.SignificantDiagonalCount;

                if (subBandType == JpxSubBandType.HL)
                {
                    // The HL sub-band uses the LL and LH mapping with the horizontal and vertical sums exchanged
                    var swap = sumH;
                    sumH = sumV;
                    sumV = swap;
                }

                int context;

                if (subBandType == JpxSubBandType.HH)
                {
                    var sumHV = sumH + sumV;

                    if (sumD >= 3)
                    {
                        context = 8;
                    }
                    else if (sumD == 2)
                    {
                        context = sumHV >= 1 ? 7 : 6;
                    }
                    else if (sumD == 1)
                    {
                        context = sumHV >= 2 ? 5 : 3 + sumHV;
                    }
                    else
                    {
                        context = Math.Min(sumHV, 2);
                    }
                }
                else
                {
                    if (sumH == 2)
                    {
                        context = 8;
                    }
                    else if (sumH == 1)
                    {
                        if (sumV >= 1)
                        {
                            context = 7;
                        }
                        else
                        {
                            context = sumD >= 1 ? 6 : 5;
                        }
                    }
                    else if (sumV >= 1)
                    {
                        context = 2 + sumV;
                    }
                    else
                    {
                        context = Math.Min(sumD, 2);
                    }
                }

                contexts[index] = (byte)context;
            }

            return contexts;
        }

        /// <summary>
        /// Builds the sign coding context and XOR bit lookups.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Tables D.2 and D.3
        /// </remarks>
        private static void BuildSignTable()
        {
            static int SignContribution(bool significant, bool negative) =>
                !significant ? 0 :
                negative ? -1 : 1;

            for (var index = 0; index < SignContextTable.Length; index++)
            {
                var state = new JpxTier1CoefficientState((ushort)index);

                var horizontal =
                    SignContribution(state.IsWestNeighborSignificant, state.IsWestNeighborNegative) +
                    SignContribution(state.IsEastNeighborSignificant, state.IsEastNeighborNegative);
                var vertical =
                    SignContribution(state.IsNorthNeighborSignificant, state.IsNorthNeighborNegative) +
                    SignContribution(state.IsSouthNeighborSignificant, state.IsSouthNeighborNegative);

                // Two significant neighbours with the same sign contribute the same as one
                horizontal = MathUtils.Clamp(horizontal, -1, 1);
                vertical = MathUtils.Clamp(vertical, -1, 1);

                // Table D.3 is antisymmetric: negating both contributions gives the same context label with an
                // inverted decoded sign bit
                var xorBit = 0;

                if (horizontal < 0 || (horizontal == 0 && vertical < 0))
                {
                    horizontal = -horizontal;
                    vertical = -vertical;
                    xorBit = 1;
                }

                int context;

                if (horizontal == 1)
                {
                    context = JpxMqContext.FirstSignContext + 3 + vertical;
                }
                else
                {
                    context = JpxMqContext.FirstSignContext + vertical;
                }

                SignContextTable[index] = new JpxTier1SignContext(context, xorBit);
            }
        }
    }
}
