// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;

namespace PdfToSvg.Imaging.Jpx.Coding
{
    internal struct JpxCodeBlockSegmentData
    {
        private const int InitialCapacity = 10;

        // It is most common to have only one data chunk per code bock
        private ArraySegment<byte> segmentHead;
        private ArraySegment<byte>[]? segmentTail;

        private int segmentCount;
        private int byteCount;

        public int ByteCount => byteCount;

        public int SegmentCount => segmentCount;

        public JpxCodeBlockSegmentData(ArraySegment<byte> segmentHead)
        {
            this.segmentHead = segmentHead;
            this.byteCount = segmentHead.Count;
            this.segmentCount = 1;
        }

        public ArraySegment<byte> this[int segmentIndex]
        {
            get
            {
                if (segmentIndex < 0 || segmentIndex >= segmentCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(segmentIndex));
                }

                if (segmentIndex == 0)
                {
                    return segmentHead;
                }
                else
                {
                    return segmentTail![segmentIndex - 1];
                }
            }
        }

        public void Append(ArraySegment<byte> segment)
        {
            if (segmentCount == 0)
            {
                segmentHead = segment;
            }
            else
            {
                var tailCount = segmentCount - 1;

                if (segmentTail == null)
                {
                    segmentTail = new ArraySegment<byte>[InitialCapacity];
                }
                else if (tailCount >= segmentTail.Length)
                {
                    var newTail = new ArraySegment<byte>[segmentTail.Length * 2];
                    Array.Copy(segmentTail, newTail, tailCount);
                    segmentTail = newTail;
                }

                segmentTail[tailCount] = segment;
            }

            segmentCount++;
            byteCount += segment.Count;
        }

        public void CopyTo(byte[] buffer, int offset = 0)
        {
            var segmentCount = this.segmentCount;
            var segmentTail = this.segmentTail;

            if (segmentCount > 0)
            {
                var cursor = offset;

                segmentHead.CopyTo(buffer, cursor);
                cursor += segmentHead.Count;

                if (segmentTail != null)
                {
                    var segmentTailLength = segmentCount - 1;

                    for (var i = 0; i < segmentTailLength; i++)
                    {
                        ref var segment = ref segmentTail[i];
                        segment.CopyTo(buffer, cursor);
                        cursor += segment.Count;
                    }
                }
            }
        }

        public byte[] ToArray()
        {
            var result = new byte[byteCount];
            CopyTo(result);
            return result;
        }
    }
}
