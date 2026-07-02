// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    public class JpegBlockEnumeratorTests
    {
        [Test]
        public void MoveNext()
        {
            var components = new JpegComponent[]
            {
                new JpegComponent { ComponentId = 0, HorizontalSamplingFactor = 2, VerticalSamplingFactor = 2 },
                new JpegComponent { ComponentId = 1, HorizontalSamplingFactor = 1, VerticalSamplingFactor = 2 },
                new JpegComponent { ComponentId = 2, HorizontalSamplingFactor = 2, VerticalSamplingFactor = 1 },
            };
            var enumerator = new JpegBlockEnumerator(components, restartInterval: 2);

            void MoveNextAndAssert(int blockIndex, int componentId, int subSampleY, int subSampleX, bool shouldRestart)
            {
                enumerator.MoveNext();

                Assert.AreEqual(blockIndex, enumerator.BlockIndex, "BlockIndex");
                Assert.AreEqual(subSampleX, enumerator.SubSampleX, "SubSampleX");
                Assert.AreEqual(subSampleY, enumerator.SubSampleY, "SubSampleY");
                Assert.AreEqual(componentId, enumerator.ComponentId, "ComponentId");
                Assert.AreEqual(shouldRestart, enumerator.ShouldRestart, "ShouldRestart");
            }

            var block = 0;

            //               Block     C  Y  X  Restart
            // ----------------------------------

            // MCU 0
            MoveNextAndAssert(block++, 0, 0, 0, true);
            MoveNextAndAssert(block++, 0, 0, 1, false);
            MoveNextAndAssert(block++, 0, 1, 0, false);
            MoveNextAndAssert(block++, 0, 1, 1, false);

            MoveNextAndAssert(block++, 1, 0, 0, false);
            MoveNextAndAssert(block++, 1, 1, 0, false);

            MoveNextAndAssert(block++, 2, 0, 0, false);
            MoveNextAndAssert(block++, 2, 0, 1, false);

            // MCU 1
            MoveNextAndAssert(block++, 0, 0, 0, false);
            MoveNextAndAssert(block++, 0, 0, 1, false);
            MoveNextAndAssert(block++, 0, 1, 0, false);
            MoveNextAndAssert(block++, 0, 1, 1, false);

            MoveNextAndAssert(block++, 1, 0, 0, false);
            MoveNextAndAssert(block++, 1, 1, 0, false);

            MoveNextAndAssert(block++, 2, 0, 0, false);
            MoveNextAndAssert(block++, 2, 0, 1, false);

            // MCU 2
            MoveNextAndAssert(block++, 0, 0, 0, true);
            MoveNextAndAssert(block++, 0, 0, 1, false);
            MoveNextAndAssert(block++, 0, 1, 0, false);
            MoveNextAndAssert(block++, 0, 1, 1, false);

            MoveNextAndAssert(block++, 1, 0, 0, false);
            MoveNextAndAssert(block++, 1, 1, 0, false);

            MoveNextAndAssert(block++, 2, 0, 0, false);
            MoveNextAndAssert(block++, 2, 0, 1, false);
        }
    }
}
