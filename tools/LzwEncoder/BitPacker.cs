using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LzwEncoder
{
    internal class BitPacker
    {
        private readonly MemoryStream output = new MemoryStream();
        private int pendingByte;
        private int pendingBytePopulatedBits;

        public void Write(int value, int bits)
        {
            for (var cursor = 0; cursor < bits;)
            {
                var takeBits = Math.Min(8 - pendingBytePopulatedBits, bits - cursor);
                var bitMask = (1 << takeBits) - 1;

                pendingByte |= ((value >> (bits - cursor - takeBits)) & bitMask) << (8 - pendingBytePopulatedBits - takeBits);

                pendingBytePopulatedBits += takeBits;

                if (pendingBytePopulatedBits == 8)
                {
                    output.WriteByte((byte)pendingByte);
                    pendingByte = 0;
                    pendingBytePopulatedBits = 0;
                }

                cursor += takeBits;
            }
        }

        public byte[] ToArray()
        {
            if (pendingBytePopulatedBits > 0)
            {
                output.WriteByte((byte)pendingByte);
            }

            return output.ToArray();
        }
    }
}
