// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.Coding;

namespace PdfToSvg.Tests.Images.Jpx.Coding
{
    internal class JpxTier1CoefficientStateTests
    {
        [Test]
        public void IsUndecoded_CoefficientStates()
        {
            var state = new JpxTier1CoefficientState();
            Assert.AreEqual(true, state.IsUndecoded);
            Assert.AreEqual(false, state.NeedsRefinement);

            // Decoded by the significance propagation pass without becoming significant
            var visited = state;
            visited.MarkVisited();
            Assert.AreEqual(false, visited.IsUndecoded);
            Assert.AreEqual(false, visited.NeedsRefinement);

            visited.ClearVisited();
            Assert.AreEqual(true, visited.IsUndecoded);

            // Became significant in the current bit-plane => not refined until the next bit-plane
            var newlySignificant = state;
            newlySignificant.MarkSignificant();
            newlySignificant.MarkVisited();
            Assert.AreEqual(false, newlySignificant.IsUndecoded);
            Assert.AreEqual(false, newlySignificant.NeedsRefinement);

            newlySignificant.ClearVisited();
            Assert.AreEqual(true, newlySignificant.IsSignificant);
            Assert.AreEqual(true, newlySignificant.NeedsRefinement);
        }

        [Test]
        public void MarkNeighborSignificant_SetsSignificanceAndSign()
        {
            var state = new JpxTier1CoefficientState();
            Assert.AreEqual(false, state.HasSignificantNeighbors);

            state.MarkEastNeighborSignificant(negative: 1);
            state.MarkNorthNeighborSignificant(negative: 0);

            Assert.AreEqual(true, state.HasSignificantNeighbors);
            Assert.AreEqual(true, state.IsEastNeighborSignificant);
            Assert.AreEqual(true, state.IsEastNeighborNegative);
            Assert.AreEqual(true, state.IsNorthNeighborSignificant);
            Assert.AreEqual(false, state.IsNorthNeighborNegative);
            Assert.AreEqual(false, state.IsWestNeighborSignificant);
            Assert.AreEqual(false, state.IsSouthNeighborSignificant);

            Assert.AreEqual(1, state.SignificantHorizontalCount);
            Assert.AreEqual(1, state.SignificantVerticalCount);
            Assert.AreEqual(0, state.SignificantDiagonalCount);

            state.MarkNorthWestNeighborSignificant();
            state.MarkSouthEastNeighborSignificant();
            Assert.AreEqual(2, state.SignificantDiagonalCount);
        }

        [Test]
        public void OperatorOr_CombinesStates()
        {
            var first = new JpxTier1CoefficientState();
            first.MarkVisited();

            var second = new JpxTier1CoefficientState();
            second.MarkWestNeighborSignificant(0);

            var union = first | second;

            Assert.AreEqual(false, union.IsUndecoded);
            Assert.AreEqual(true, union.HasSignificantNeighbors);

            var emptyUnion = new JpxTier1CoefficientState() | new JpxTier1CoefficientState();
            Assert.AreEqual(true, emptyUnion.IsUndecoded);
            Assert.AreEqual(false, emptyUnion.HasSignificantNeighbors);
        }

        [Test]
        public void SignificanceContextIndex_CoversNeighborSignificance()
        {
            // The significance context index reflects the significance of all eight neighbours,
            // but not their signs or the coefficient's own state
            var state = new JpxTier1CoefficientState();
            state.MarkWestNeighborSignificant(1);
            state.MarkSouthWestNeighborSignificant();
            state.MarkSignificant();
            state.MarkVisited();
            state.MarkRefined();

            var reference = new JpxTier1CoefficientState();
            reference.MarkWestNeighborSignificant(0);
            reference.MarkSouthWestNeighborSignificant();

            Assert.AreEqual(reference.SignificanceContextIndex, state.SignificanceContextIndex);
            Assert.AreNotEqual(reference.SignContextIndex, state.SignContextIndex);
            Assert.Less(state.SignificanceContextIndex, JpxTier1CoefficientState.SignificanceContextIndexCount);
            Assert.Less(state.SignContextIndex, JpxTier1CoefficientState.SignContextIndexCount);
        }
    }
}
