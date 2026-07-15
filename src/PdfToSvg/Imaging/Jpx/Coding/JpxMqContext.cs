// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace PdfToSvg.Imaging.Jpx.Coding
{
    internal static class JpxMqContext
    {
        // Context labels of the probability model. The concrete label values are implementation-defined:
        //
        //       Reference    Bits  Content
        //  ---------------------------------------------------------
        //       Table D.1    0-8   significance coding
        //       Table D.3    9-13  sign coding
        //       Table D.4   14-16  magnitude refinement coding
        //   Section D.3.4   17-18  run-length and UNIFORM
        //  ---------------------------------------------------------

        public const int FirstSignContext = 9;
        public const int RefinementNoNeighborsContext = 14;
        public const int RefinementNeighborsContext = 15;
        public const int RefinementLaterContext = 16;

        public const int RunLengthContext = 17;
        public const int UniformContext = 18;
        public const int ContextCount = 19;

        public static void Clear(JpxMqContextEntry[] entries)
        {
            Array.Clear(entries, 0, entries.Length);

            // Default values from ITU-T T.800 (06/2019) Table D.7
            entries[0].Index = 4;
            entries[RunLengthContext].Index = 3;
            entries[UniformContext].Index = 46;
        }

        /// <summary>
        /// Returns the magnitude refinement coding context label of a coefficient.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Table D.4
        /// </remarks>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int GetRefinementContext(JpxTier1CoefficientState state)
        {
            if (state.IsRefined)
            {
                return RefinementLaterContext;
            }

            if (state.HasSignificantNeighbors)
            {
                return RefinementNeighborsContext;
            }

            return RefinementNoNeighborsContext;
        }

    }
}
