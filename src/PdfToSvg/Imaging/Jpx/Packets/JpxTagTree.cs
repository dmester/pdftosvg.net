// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpx.Packets
{
    /// <summary>
    /// Tag tree implemenation according to ITU-T T.800 (06/2019) section B.10.2
    /// </summary>
    /// <threadsafety instance="false" />
    internal class JpxTagTree
    {
        private const int NoParent = -1;
        private const int NotYetDecoded = int.MaxValue;

        // Nodes are ordered starting with the deepest highest resolution level (as leaves), up to the root
        private readonly Node[] nodes;
        private readonly int leafCount;

        // Scratch buffer reused by Decode to walk from a leaf to the root. Sized to the tree
        // depth in the constructor so a leaf-to-root walk can never overrun it (not thread safe).
        private readonly int[] reusablePath;

        private struct Node
        {
            public int Value;
            public int Low;
            public int ParentIndex;
        }

        public JpxTagTree(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be greater than zero.");
            }
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Width and height must be greater than zero.");
            }

            var nodeCount = 0;
            var depth = 0;

            var levelWidth = width;
            var levelHeight = height;

            while (levelWidth > 1 || levelHeight > 1)
            {
                nodeCount += levelWidth * levelHeight;

                levelWidth = (levelWidth + 1) / 2;
                levelHeight = (levelHeight + 1) / 2;
                depth++;
            }

            // Root node
            depth++;
            nodeCount++;

            nodes = new Node[nodeCount];
            leafCount = width * height;

            // A Decode walk visits one node per level from the leaf up to (and including) the root.
            reusablePath = new int[depth];

            levelWidth = width;
            levelHeight = height;

            // Initialize tree structure
            var nodeIndex = 0;

            for (var level = 0; level < depth; level++)
            {
                var levelNodeCount = levelWidth * levelHeight;

                var parentLevelStartIndex = nodeIndex + levelNodeCount;
                var parentLevelWidth = (levelWidth + 1) / 2;
                var parentLevelHeight = (levelHeight + 1) / 2;

                for (var y = 0; y < levelHeight; y++)
                {
                    for (var x = 0; x < levelWidth; x++)
                    {
                        var parentLevelX = x >> 1;
                        var parentLevelY = y >> 1;

                        nodes[nodeIndex].ParentIndex =
                            level == depth - 1
                                ? NoParent
                                : parentLevelStartIndex +
                                  parentLevelX +
                                  parentLevelY * parentLevelWidth;

                        nodes[nodeIndex].Value = NotYetDecoded;

                        nodeIndex++;
                    }
                }

                levelWidth = parentLevelWidth;
                levelHeight = parentLevelHeight;
            }
        }

        public bool Decode(ref JpxPacketBitReader reader, int leafNumber, int threshold)
        {
            if (leafNumber < 0 || leafNumber >= leafCount)
            {
                throw new ArgumentOutOfRangeException(nameof(leafNumber),
                    "Leaf number must be less than the total number of leaves and not negative");
            }

            var nodes = this.nodes;
            var path = this.reusablePath;
            var pathLength = 0;

            var nodeIndex = leafNumber;
            while (nodeIndex != NoParent)
            {
                path[pathLength++] = nodeIndex;
                nodeIndex = nodes[nodeIndex].ParentIndex;
            }

            var low = 0;

            // Walk path from leaf to root
            for (var i = pathLength - 1; i >= 0; i--)
            {
                nodeIndex = path[i];
                ref var node = ref nodes[nodeIndex];

                if (node.Low < low)
                {
                    node.Low = low;
                }

                while (node.Low < threshold && node.Value == NotYetDecoded)
                {
                    var bit = reader.ReadBit();
                    if (bit == 1)
                    {
                        node.Value = node.Low;
                    }
                    else
                    {
                        node.Low++;
                    }
                }

                low = node.Value == NotYetDecoded ? node.Low : node.Value;
            }

            return nodes[leafNumber].Value < threshold;
        }
    }
}
