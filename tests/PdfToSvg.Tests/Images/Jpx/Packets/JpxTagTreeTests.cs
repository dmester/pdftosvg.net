// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.IO;
using PdfToSvg.Imaging.Jpx.Packets;
using System.Collections.Generic;

namespace PdfToSvg.Tests.Images.Jpx.Packets
{
    internal class JpxTagTreeTests
    {
        private static JpxPacketBitReader Reader(params int[] bits)
        {
            var bytes = new List<byte>();
            var current = 0;
            var count = 0;

            foreach (var bit in bits)
            {
                current = (current << 1) | bit;

                if (++count == 8)
                {
                    bytes.Add((byte)current);
                    current = 0;
                    count = 0;
                }
            }

            if (count > 0)
            {
                bytes.Add((byte)(current << (8 - count)));
            }

            var data = bytes.ToArray();
            var dataReader = new JpxDataReader(data);
            return new JpxPacketBitReader(dataReader);
        }

        // Decodes the value stored at a leaf by decoding the same bit stream repeatedly on a fresh tree, raising the
        // threshold until the value is first reported as below it. Decode returns "value < threshold", so the smallest
        // threshold that returns true equals value + 1.
        private static int DecodeLeafValue(int width, int height, int[] leafOrder, int targetLeaf, int[] bits)
        {
            for (var threshold = 0; threshold <= 64; threshold++)
            {
                var tree = new JpxTagTree(width, height);
                var reader = Reader(bits);

                var below = false;
                foreach (var leaf in leafOrder)
                {
                    below = tree.Decode(ref reader, leaf, leaf == targetLeaf ? threshold : 1000);
                    if (leaf == targetLeaf)
                    {
                        break;
                    }
                }

                if (below)
                {
                    return threshold - 1;
                }
            }

            return -1;
        }

        [Test]
        public void Decode_SpecFigureB12FirstThreeLeaves()
        {
            // ITU-T T.800 (06/2019) Figure B.12 and its NOTE. The example array is 6 wide, 3 high. Decoding the first
            // three leaves in raster order consumes "01111 001 101" and yields the leaf values 1, 3 and 2.
            var bits = new[] { 0, 1, 1, 1, 1, /**/ 0, 0, 1, /**/ 1, 0, 1 };
            var order = new[] { 0, 1, 2 };

            Assert.AreEqual(1, DecodeLeafValue(6, 3, order, 0, bits));
            Assert.AreEqual(3, DecodeLeafValue(6, 3, order, 1, bits));
            Assert.AreEqual(2, DecodeLeafValue(6, 3, order, 2, bits));
        }

        [Test]
        public void Decode_SpecFigureB12ConsumesExactlyElevenBits()
        {
            // Decoding all three leaves consumes exactly 11 bits, which occupy the first two bytes of the stream.
            // Position points at the byte that still holds unread bits.
            var tree = new JpxTagTree(6, 3);
            var reader = Reader(0, 1, 1, 1, 1, 0, 0, 1, 1, 0, 1);

            tree.Decode(ref reader, 0, 1000);
            tree.Decode(ref reader, 1, 1000);
            tree.Decode(ref reader, 2, 1000);

            // 11 bits span into the second byte; Position points at the byte that still holds the three remaining
            // unread bits (offset 2, minus 1 because a partial byte is buffered).
            Assert.AreEqual(1, reader.Cursor);
        }

        [Test]
        public void Decode_RootValueRaisedByLeadingZeros()
        {
            // 2x1 tree (root + two leaves). "0 1 1" fixes the root minimum at 1 and confirms the leaf equals it
            // => value 1.
            var value = DecodeLeafValue(2, 1, new[] { 0 }, 0, new[] { 0, 1, 1 });
            Assert.AreEqual(1, value);
        }

        [Test]
        public void Decode_LeafIncrementAboveRoot()
        {
            // "1" fixes the root minimum at 0; "0 1" then raises the leaf one step above the root => value 1.
            var value = DecodeLeafValue(2, 1, new[] { 0 }, 0, new[] { 1, 0, 1 });
            Assert.AreEqual(1, value);
        }

        [Test]
        public void Decode_ReturnsFalseWhenValueNotBelowThreshold()
        {
            // Leaf value 6 (root fixed at 0 with "1", then six increments and a terminating 1).
            // A threshold of 2 reports the value is not below it.
            var tree = new JpxTagTree(2, 1);
            var reader = Reader(1, 0, 0, 0, 0, 0, 0, 1);

            Assert.IsFalse(tree.Decode(ref reader, 0, 2));
        }

        [Test]
        public void Decode_ThresholdQueryCanBeRefinedLater()
        {
            // A partial query (small threshold) can be refined by a later query with a larger threshold on the same
            // tree: no bits are re-read for the part already resolved.
            var tree = new JpxTagTree(2, 1);
            var reader = Reader(1, 0, 0, 0, 0, 0, 0, 1);

            Assert.IsFalse(tree.Decode(ref reader, 0, 2));
            Assert.IsFalse(tree.Decode(ref reader, 0, 6));
            Assert.IsTrue(tree.Decode(ref reader, 0, 7));
        }

        [Test]
        public void Decode_CausalityAcrossSiblingLeaves()
        {
            // ITU-T T.800 (06/2019) B.10.2: a shared ancestor fixed by an earlier leaf is not coded again. In a 2x1
            // tree the root is shared by both leaves. After leaf 0 fixes the root at 1 (bits "0 1 1"), leaf 1 only
            // needs "1" to confirm it equals the root.
            var tree = new JpxTagTree(2, 1);
            var reader = Reader(0, 1, 1, /**/ 1);

            Assert.IsTrue(tree.Decode(ref reader, 0, 1000));
            Assert.IsTrue(tree.Decode(ref reader, 1, 1000));

            // Exactly 4 bits consumed => still within the first (only) byte; Position stays at 0.
            Assert.AreEqual(0, reader.Cursor);
        }
    }
}
