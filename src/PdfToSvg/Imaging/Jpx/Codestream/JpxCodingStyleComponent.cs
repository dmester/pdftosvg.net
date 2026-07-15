// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    /// <summary>
    /// COC marker segment
    /// </summary>
    internal sealed class JpxCodingStyleComponent
    {
        public bool UserDefinedPrecincts;
        public int NumberOfDecompositionLevels;
        public int CodeBlockWidth;
        public int CodeBlockHeight;
        public JpxCodeBlockStyle CodeBlockStyle;
        public bool ReversibleFilter;
        public JpxPrecinctSize[] PrecinctSize = ArrayUtils.Empty<JpxPrecinctSize>();

        /// <summary>
        /// Copies the component-scoped parameters onto <paramref name="target"/>, leaving the
        /// tile-scoped SGcod parameters of <paramref name="target"/> untouched.
        /// </summary>
        public void CopyTo(JpxCodingStyleDefaults target)
        {
            target.CodingStyle.UserDefinedPrecincts = UserDefinedPrecincts;
            target.NumberOfDecompositionLevels = NumberOfDecompositionLevels;
            target.CodeBlockWidth = CodeBlockWidth;
            target.CodeBlockHeight = CodeBlockHeight;
            target.CodeBlockStyle = CodeBlockStyle;
            target.ReversibleFilter = ReversibleFilter;
            target.PrecinctSize = (JpxPrecinctSize[])PrecinctSize.Clone();
        }
    }
}
