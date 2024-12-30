// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jbig2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Tests.Images.Jbig2
{
    public class JbigDecoderTests
    {
        [Test]
        public void DetectGenericRegionLength_Mmr_Middle()
        {
            var data = new byte[] {
                99, 99,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                0xff, // RegionFlagsIndex
                1, 1, 1, 1, 1, 1,
                0, 0, // End marker
                1, 2, 3, 4, // Trailing row count
                1, 1, 1, 1, 1, 1,
                99, 99
            };

            Assert.AreEqual(30, JbigDecoder.DetectGenericRegionLength(data, 2, data.Length - 4));
        }

        [Test]
        public void DetectGenericRegionLength_Mmr_Start()
        {
            var data = new byte[] {
                99, 99,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                0xff, // RegionFlags
                0, 0, // End marker
                1, 2, 3, 4, // Trailing row count
                1, 1, 1, 1, 1, 1,
                99, 99
            };

            Assert.AreEqual(24, JbigDecoder.DetectGenericRegionLength(data, 2, data.Length - 4));
        }


        [Test]
        public void DetectGenericRegionLength_Mmr_End()
        {
            var data = new byte[] {
                99, 99,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                0xff, // RegionFlags
                1, 1, 1, 1, 1, 1,
                0, 0, // End marker
                1, 2, 3, 4, // Trailing row count
                99, 99
            };

            Assert.AreEqual(30, JbigDecoder.DetectGenericRegionLength(data, 2, data.Length - 4));
        }

        [Test]
        public void DetectGenericRegionLength_Mmr_PastEnd()
        {
            var data = new byte[] {
                99, 99,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                0xff, // RegionFlags
                1, 1, 1, 1, 1, 1,
                0, 0, // End marker
                1, 2, 3, 4, // Trailing row count
                99
            };

            Assert.AreEqual(-1, JbigDecoder.DetectGenericRegionLength(data, 2, data.Length - 4));
        }

        [Test]
        public void DetectGenericRegionLength_NotFound()
        {
            var data = new byte[] {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                0xff, // RegionFlags
                1, 1, 1, 1, 1, 1, 1,
            };

            Assert.AreEqual(-1, JbigDecoder.DetectGenericRegionLength(data, 0, data.Length));
        }

        [Test]
        public void DetectGenericRegionLength_Arithmetic()
        {
            var data = new byte[] {
                99, 99,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                0x00, // RegionFlags
                1, 1, 1, 1, 1, 1,
                0xff, 0xac, // End marker
                1, 2, 3, 4, // Trailing row count
                1, 1, 1, 1, 1, 1,
                99, 99
            };

            Assert.AreEqual(30, JbigDecoder.DetectGenericRegionLength(data, 2, data.Length - 4));
        }
    }
}
