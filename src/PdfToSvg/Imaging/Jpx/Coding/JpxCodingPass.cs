// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.Codestream;

namespace PdfToSvg.Imaging.Jpx.Coding
{
    internal enum JpxCodingPassType
    {
        Cleanup,
        SignificancePropagation,
        MagnitudeRefinement,
    }

    internal static class JpxCodingPass
    {
        public static JpxCodingPassType GetPassType(int pass)
        {
            // ITU-T T.800 (06/2019) section D.3
            return (pass % 3) switch
            {
                0 => JpxCodingPassType.Cleanup,
                1 => JpxCodingPassType.SignificancePropagation,
                _ => JpxCodingPassType.MagnitudeRefinement,
            };
        }

        /// <summary>
        /// ITU-T T.800 (06/2019) Table D.9:
        /// With selective arithmetic coding bypass, significance propagation and magnitude refinement passes are coded
        /// raw from the fifth bit-plane (pass 10) onwards; cleanup passes stay arithmetic.
        /// </summary>
        public static bool IsRaw(JpxCodeBlockStyle style, int pass)
        {
            if (!style.SelectiveArithmeticCodingBypass || pass < 10)
            {
                return false;
            }

            var type = GetPassType(pass);
            return type == JpxCodingPassType.SignificancePropagation ||
                   type == JpxCodingPassType.MagnitudeRefinement;
        }

        /// <summary>
        /// ITU-T T.800 (06/2019) Table D.9:
        /// A terminated pass ends the current codeword segment. With bypass, the fourth cleanup pass (pass 9) and, from
        /// the fifth bit-plane (pass 11) onwards, every non-significance-propagation pass is terminated.
        /// </summary>
        public static bool IsTerminated(JpxCodeBlockStyle style, int pass)
        {
            if (style.TerminationOnEachCodingPass)
            {
                return true;
            }

            if (!style.SelectiveArithmeticCodingBypass)
            {
                return false;
            }

            return pass == 9 ||
                (pass >= 11 && GetPassType(pass) != JpxCodingPassType.SignificancePropagation);
        }
    }
}
