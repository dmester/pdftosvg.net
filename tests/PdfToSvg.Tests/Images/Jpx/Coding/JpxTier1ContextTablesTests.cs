// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.Coding;
using PdfToSvg.Imaging.Jpx.ImageModel;

namespace PdfToSvg.Tests.Images.Jpx.Coding
{
    public class JpxTier1ContextTablesTests
    {
        // ITU-T T.800 (06/2019) Table D.1, LL and LH sub-band column (ΣH, ΣV, ΣD)
        [TestCase(0, 0, 0, ExpectedResult = 0)]
        [TestCase(0, 0, 1, ExpectedResult = 1)]
        [TestCase(0, 0, 2, ExpectedResult = 2)]
        [TestCase(0, 0, 3, ExpectedResult = 2)]
        [TestCase(0, 1, 0, ExpectedResult = 3)]
        [TestCase(0, 1, 3, ExpectedResult = 3)]
        [TestCase(0, 2, 0, ExpectedResult = 4)]
        [TestCase(1, 0, 0, ExpectedResult = 5)]
        [TestCase(1, 0, 1, ExpectedResult = 6)]
        [TestCase(1, 1, 0, ExpectedResult = 7)]
        [TestCase(1, 2, 3, ExpectedResult = 7)]
        [TestCase(2, 0, 0, ExpectedResult = 8)]
        [TestCase(2, 2, 3, ExpectedResult = 8)]
        public int GetSignificanceContextTable_TableD1LLAndLH(int sumH, int sumV, int sumD)
        {
            var llContext = SignificanceContext(JpxSubBandType.LL, sumH, sumV, sumD);
            var lhContext = SignificanceContext(JpxSubBandType.LH, sumH, sumV, sumD);

            Assert.AreEqual(llContext, lhContext, "The LL and LH sub-bands share the same mapping");
            return llContext;
        }

        // ITU-T T.800 (06/2019) Table D.1, HL sub-band column: the LL mapping with the roles of
        // the horizontal and vertical sums exchanged (ΣH, ΣV, ΣD)
        [TestCase(0, 0, 0, ExpectedResult = 0)]
        [TestCase(1, 0, 0, ExpectedResult = 3)]
        [TestCase(2, 0, 0, ExpectedResult = 4)]
        [TestCase(0, 1, 0, ExpectedResult = 5)]
        [TestCase(0, 1, 1, ExpectedResult = 6)]
        [TestCase(1, 1, 0, ExpectedResult = 7)]
        [TestCase(0, 2, 0, ExpectedResult = 8)]
        public int GetSignificanceContextTable_TableD1HL(int sumH, int sumV, int sumD)
        {
            return SignificanceContext(JpxSubBandType.HL, sumH, sumV, sumD);
        }

        // ITU-T T.800 (06/2019) Table D.1, HH sub-band column (ΣH, ΣV, ΣD)
        [TestCase(0, 0, 0, ExpectedResult = 0)]
        [TestCase(1, 0, 0, ExpectedResult = 1)]
        [TestCase(1, 1, 0, ExpectedResult = 2)]
        [TestCase(2, 1, 0, ExpectedResult = 2)]
        [TestCase(0, 0, 1, ExpectedResult = 3)]
        [TestCase(1, 0, 1, ExpectedResult = 4)]
        [TestCase(1, 1, 1, ExpectedResult = 5)]
        [TestCase(0, 0, 2, ExpectedResult = 6)]
        [TestCase(1, 0, 2, ExpectedResult = 7)]
        [TestCase(0, 0, 3, ExpectedResult = 8)]
        [TestCase(2, 2, 3, ExpectedResult = 8)]
        public int GetSignificanceContextTable_TableD1HH(int sumH, int sumV, int sumD)
        {
            return SignificanceContext(JpxSubBandType.HH, sumH, sumV, sumD);
        }

        // ITU-T T.800 (06/2019) Tables D.2 and D.3. The horizontal and vertical contributions
        // are given as -1/0/1, where 0 is realized as two insignificant neighbours.
        [TestCase(1, 1, 13, 0)]
        [TestCase(1, 0, 12, 0)]
        [TestCase(1, -1, 11, 0)]
        [TestCase(0, 1, 10, 0)]
        [TestCase(0, 0, 9, 0)]
        [TestCase(0, -1, 10, 1)]
        [TestCase(-1, 1, 11, 1)]
        [TestCase(-1, 0, 12, 1)]
        [TestCase(-1, -1, 13, 1)]
        public void SignContextTable_TableD3(
            int horizontal, int vertical, int expectedContext, int expectedXorBit)
        {
            var state = new JpxTier1CoefficientState();

            if (horizontal != 0)
            {
                state.MarkWestNeighborSignificant(horizontal < 0 ? 1 : 0);
            }
            if (vertical != 0)
            {
                state.MarkNorthNeighborSignificant(vertical < 0 ? 1 : 0);
            }

            Assert.AreEqual(expectedContext, JpxTier1LookupTables.SignContextTable[state.SignContextIndex].Label);
            Assert.AreEqual(expectedXorBit, JpxTier1LookupTables.SignContextTable[state.SignContextIndex].XorBit);
        }

        [Test]
        public void SignContextTable_TableD2OppositeSignsCancel()
        {
            // Two significant neighbours with different signs contribute 0 (Table D.2)
            var state = new JpxTier1CoefficientState();
            state.MarkWestNeighborSignificant(0);
            state.MarkEastNeighborSignificant(1);
            state.MarkNorthNeighborSignificant(1);
            state.MarkSouthNeighborSignificant(0);

            Assert.AreEqual(9, JpxTier1LookupTables.SignContextTable[state.SignContextIndex].Label);
            Assert.AreEqual(0, JpxTier1LookupTables.SignContextTable[state.SignContextIndex].XorBit);
        }

        [Test]
        public void SignContextTable_TableD2SameSignsContributeAsOne()
        {
            // Two significant negative vertical neighbours contribute -1 (Table D.2)
            var state = new JpxTier1CoefficientState();
            state.MarkNorthNeighborSignificant(1);
            state.MarkSouthNeighborSignificant(1);

            Assert.AreEqual(10, JpxTier1LookupTables.SignContextTable[state.SignContextIndex].Label);
            Assert.AreEqual(1, JpxTier1LookupTables.SignContextTable[state.SignContextIndex].XorBit);
        }

        [Test]
        public void GetRefinementContext_TableD4()
        {
            var noNeighbors = new JpxTier1CoefficientState();
            Assert.AreEqual(14, JpxMqContext.GetRefinementContext(noNeighbors));

            var withNeighbor = new JpxTier1CoefficientState();
            withNeighbor.MarkSouthWestNeighborSignificant();
            Assert.AreEqual(15, JpxMqContext.GetRefinementContext(withNeighbor));

            var refined = new JpxTier1CoefficientState();
            refined.MarkRefined();
            Assert.AreEqual(16, JpxMqContext.GetRefinementContext(refined));

            var refinedWithNeighbor = withNeighbor;
            refinedWithNeighbor.MarkRefined();
            Assert.AreEqual(16, JpxMqContext.GetRefinementContext(refinedWithNeighbor));
        }

        [Test]
        public void InitializeContextStates_TableD7()
        {
            var entries = new JpxMqContextEntry[JpxMqContext.ContextCount];

            entries[5].Index = 42;
            entries[5].Mps = true;

            JpxMqContext.Clear(entries);

            for (var context = 0; context < JpxMqContext.ContextCount; context++)
            {
                var expectedState = 0;

                if (context == 0)
                {
                    expectedState = 4;
                }
                else if (context == JpxMqContext.RunLengthContext)
                {
                    expectedState = 3;
                }
                else if (context == JpxMqContext.UniformContext)
                {
                    expectedState = 46;
                }

                Assert.AreEqual(expectedState, entries[context].Index, "State of context " + context);
                Assert.AreEqual(false, entries[context].Mps, "MPS of context " + context);
            }
        }

        private static JpxTier1CoefficientState State(
            int significantHorizontal = 0, int significantVertical = 0, int significantDiagonal = 0)
        {
            var state = new JpxTier1CoefficientState();

            if (significantHorizontal > 0)
            {
                state.MarkWestNeighborSignificant(0);
            }
            if (significantHorizontal > 1)
            {
                state.MarkEastNeighborSignificant(0);
            }

            if (significantVertical > 0)
            {
                state.MarkNorthNeighborSignificant(0);
            }
            if (significantVertical > 1)
            {
                state.MarkSouthNeighborSignificant(0);
            }

            if (significantDiagonal > 0)
            {
                state.MarkNorthWestNeighborSignificant();
            }
            if (significantDiagonal > 1)
            {
                state.MarkSouthEastNeighborSignificant();
            }
            if (significantDiagonal > 2)
            {
                state.MarkNorthEastNeighborSignificant();
            }

            return state;
        }

        private static int SignificanceContext(
            JpxSubBandType subBandType, int sumH, int sumV, int sumD)
        {
            var contexts = JpxTier1LookupTables.GetSignificanceContextTable(subBandType);
            return contexts[State(sumH, sumV, sumD).SignificanceContextIndex];
        }
    }
}
