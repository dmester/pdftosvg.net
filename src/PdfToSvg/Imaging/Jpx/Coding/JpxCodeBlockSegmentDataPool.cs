// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;

namespace PdfToSvg.Imaging.Jpx.Coding
{
    internal struct JpxCodeBlockSegmentDataPool
    {
        private byte[]? pool;

        public ArraySegment<byte> ToArraySegment(ref readonly JpxCodeBlockSegmentData data)
        {
            if (data.SegmentCount == 0)
            {
                return ArrayUtils.EmptySegment<byte>();
            }
            else if (data.SegmentCount == 1)
            {
                return data[0];
            }
            else
            {
                if (pool is null || pool.Length < data.ByteCount)
                {
                    pool = new byte[data.ByteCount];
                }

                data.CopyTo(pool);

                return new ArraySegment<byte>(pool, 0, data.ByteCount);
            }
        }
    }
}
