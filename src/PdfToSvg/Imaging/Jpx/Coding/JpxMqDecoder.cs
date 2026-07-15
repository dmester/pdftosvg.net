// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace PdfToSvg.Imaging.Jpx.Coding
{
    /// <summary>
    /// MQ decoder implemented according to "software convention" decoder in ITU-T T.800 (06/2019) section J.1.
    /// Note that this is essentially a duplicate of <see cref="Jbig2.Coding.JbigArithmeticDecoder"/>, to ensure both
    /// decoders are as self-contained as possible within their namespaces.
    /// </summary>
    /// <remarks>
    /// The decoder is a struct to prevent allocating thousands of heap objects in <see cref="JpxTier1Decoder"/>.
    /// </remarks>
    internal struct JpxMqDecoder
    {
        private struct QeEntry
        {
            public readonly uint Value;
            public readonly byte Nmps;
            public readonly byte Nlps;
            public readonly bool Switch;

            public QeEntry(uint value, byte nmps, byte nlps, bool switchValue)
            {
                this.Value = value;
                this.Nmps = nmps;
                this.Nlps = nlps;
                this.Switch = switchValue;
            }
        }

        private static readonly QeEntry[] qe;

        private int byteCursor; // From array start, not from "offset"
        private uint c;
        private int ct;
        private uint a;

        private byte[] data;
        private int dataEndIndex;

        static JpxMqDecoder()
        {
            // Table C.2 – Qe values and probability estimation
            qe = [
                new QeEntry(0x5601, 1, 1, true),
                new QeEntry(0x3401, 2, 6, false),
                new QeEntry(0x1801, 3, 9, false),
                new QeEntry(0x0AC1, 4, 12, false),
                new QeEntry(0x0521, 5, 29, false),
                new QeEntry(0x0221, 38, 33, false),
                new QeEntry(0x5601, 7, 6, true),
                new QeEntry(0x5401, 8, 14, false),
                new QeEntry(0x4801, 9, 14, false),
                new QeEntry(0x3801, 10, 14, false),
                new QeEntry(0x3001, 11, 17, false),
                new QeEntry(0x2401, 12, 18, false),
                new QeEntry(0x1C01, 13, 20, false),
                new QeEntry(0x1601, 29, 21, false),
                new QeEntry(0x5601, 15, 14, true),
                new QeEntry(0x5401, 16, 14, false),
                new QeEntry(0x5101, 17, 15, false),
                new QeEntry(0x4801, 18, 16, false),
                new QeEntry(0x3801, 19, 17, false),
                new QeEntry(0x3401, 20, 18, false),
                new QeEntry(0x3001, 21, 19, false),
                new QeEntry(0x2801, 22, 19, false),
                new QeEntry(0x2401, 23, 20, false),
                new QeEntry(0x2201, 24, 21, false),
                new QeEntry(0x1C01, 25, 22, false),
                new QeEntry(0x1801, 26, 23, false),
                new QeEntry(0x1601, 27, 24, false),
                new QeEntry(0x1401, 28, 25, false),
                new QeEntry(0x1201, 29, 26, false),
                new QeEntry(0x1101, 30, 27, false),
                new QeEntry(0x0AC1, 31, 28, false),
                new QeEntry(0x09C1, 32, 29, false),
                new QeEntry(0x08A1, 33, 30, false),
                new QeEntry(0x0521, 34, 31, false),
                new QeEntry(0x0441, 35, 32, false),
                new QeEntry(0x02A1, 36, 33, false),
                new QeEntry(0x0221, 37, 34, false),
                new QeEntry(0x0141, 38, 35, false),
                new QeEntry(0x0111, 39, 36, false),
                new QeEntry(0x0085, 40, 37, false),
                new QeEntry(0x0049, 41, 38, false),
                new QeEntry(0x0025, 42, 39, false),
                new QeEntry(0x0015, 43, 40, false),
                new QeEntry(0x0009, 44, 41, false),
                new QeEntry(0x0005, 45, 42, false),
                new QeEntry(0x0001, 45, 43, false),
                new QeEntry(0x5601, 46, 46, false),
            ];
        }

        public JpxMqDecoder(ArraySegment<byte> data) : this(data.Array!, data.Offset, data.Count)
        {
        }

        public JpxMqDecoder(byte[] data, int offset, int count)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (offset < 0 || offset > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > data.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            this.data = data;
            this.dataEndIndex = offset + count;
            this.byteCursor = offset;

            InitDec();
        }

        private uint PeekByte(int byteOffset)
        {
            byteOffset += this.byteCursor;

            if (byteOffset >= dataEndIndex)
            {
                // Section D.4.1 says the decoder shall extend the input with 0xFF when reaching the end of the input
                return 0xff;
            }
            else
            {
                return this.data[byteOffset];
            }
        }

        private void InitDec()
        {
            // Figure J.1

            if (byteCursor < dataEndIndex)
            {
                c = ((uint)data[byteCursor] ^ 0xff) << 16;
            }
            else
            {
                c = 0;
            }

            ByteIn();

            c = c << 7;
            ct = ct - 7;
            a = 0x8000;
        }

        private void ByteIn()
        {
            // Figure J.3

            var b = PeekByte(byteOffset: 0);
            var b1 = PeekByte(byteOffset: 1);

            if (b == 0xff)
            {
                if (b1 > 0x8f)
                {
                    ct = 8;

                    // JbigArithmeticDecoder has a protection against terminated streams here, but JPEG 2000
                    // explicitly allows overreading the input (see section D.4.1), so we can't have it here.
                }
                else
                {
                    byteCursor++;
                    c = c + 0xfe00 - (b1 << 9);
                    ct = 7;
                }
            }
            else
            {
                byteCursor++;
                c = c + 0xff00 - (b1 << 8);
                ct = 8;
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private bool MpsExchange(ref QeEntry qeEntry, ref JpxMqContextEntry context)
        {
            // Figure C.16

            bool d;

            if (a < qeEntry.Value)
            {
                d = !context.Mps;

                if (qeEntry.Switch)
                {
                    context.Mps = !context.Mps;
                }

                context.Index = qeEntry.Nlps;
            }
            else
            {
                d = context.Mps;
                context.Index = qeEntry.Nmps;
            }

            return d;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private bool LpsExchange(ref QeEntry qeEntry, ref JpxMqContextEntry context)
        {
            // Figure C.17

            bool d;

            if (a < qeEntry.Value)
            {
                a = qeEntry.Value;
                d = context.Mps;
                context.Index = qeEntry.Nmps;
            }
            else
            {
                a = qeEntry.Value;
                d = !context.Mps;

                if (qeEntry.Switch)
                {
                    context.Mps = !context.Mps;
                }

                context.Index = qeEntry.Nlps;
            }

            return d;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private void Renormd()
        {
            // Figure C.18

            do
            {
                if (ct == 0)
                {
                    ByteIn();
                }

                a = a << 1;
                c = c << 1;
                ct = ct - 1;
            }
            while ((a & 0x8000) == 0);
        }

        public int DecodeBit(ref JpxMqContextEntry context)
        {
#if DEBUG
            if (data == null)
            {
                throw new InvalidOperationException(
                    "Calling DecodeBit on a default JpxMqDecoder instance is not allowed");
            }
#endif

            // Figure J.2

            ref var qeEntry = ref qe[context.Index];
            bool result;

            a = a - qeEntry.Value;

            var Chigh = c >> 16;

            if (Chigh < a)
            {
                if ((a & 0x8000) == 0)
                {
                    result = MpsExchange(ref qeEntry, ref context);
                    Renormd();
                }
                else
                {
                    result = context.Mps;
                }
            }
            else
            {
                // Chigh = Chigh - A
                c -= a << 16;

                result = LpsExchange(ref qeEntry, ref context);
                Renormd();
            }

            return result ? 1 : 0;
        }
    }
}
