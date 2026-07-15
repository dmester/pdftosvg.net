// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PdfToSvg.Imaging.Jbig2.Coding
{
    /// <summary>
    /// MQ decoder implemented according to "software convention" decoder in ITU-T T.88 Annex G.
    /// Note that this is essentially a duplicate of <see cref="Jpx.Coding.JpxMqDecoder"/>, to ensure both decoders are
    /// as self-contained as possible within their namespaces.
    /// </summary>
    internal class JbigArithmeticDecoder
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

        private const int MaxFinishedStateCounter = 2;
        private int finishedStateCounter;

        static JbigArithmeticDecoder()
        {
            // Table E.1 – Qe values and probability estimation process
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

        public JbigArithmeticDecoder(VariableBitReader reader) : this(reader, int.MaxValue)
        {
        }

        public JbigArithmeticDecoder(VariableBitReader reader, int maxByteCount)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.Cursor.BitCursor != 0)
            {
                throw new ArgumentException("The reader must be on a byte boundary", nameof(reader));
            }

            reader.GetBuffer(out data, out var readerOffset, out var readerCount);

            var readerCursor = Math.Min(reader.Cursor.Cursor, readerCount);
            var remainingCount = readerCount - readerCursor;
            var decoderCount = MathUtils.Clamp(remainingCount, min: 0, max: maxByteCount);

            checked
            {
                byteCursor = readerOffset + readerCursor;
                dataEndIndex = byteCursor + decoderCount;
            }

            InitDec();
        }

        public JbigArithmeticDecoder(byte[] data, int offset, int count)
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

            if (byteOffset >= this.dataEndIndex)
            {
                return 0xff;
            }
            else
            {
                return this.data[byteOffset];
            }
        }

        private void InitDec()
        {
            // Figure G.1

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
            // Figure G.3

            var b = PeekByte(byteOffset: 0);
            var b1 = PeekByte(byteOffset: 1);

            if (b == 0xff)
            {
                if (b1 > 0x8f)
                {
                    ct = 8;

                    if (++finishedStateCounter > MaxFinishedStateCounter)
                    {
                        throw new EndOfStreamException("Possibly malformed input");
                    }
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
        private bool MpsExchange(ref QeEntry qeEntry, ref JbigArithmeticContextEntry context)
        {
            // Figure E.16

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
        private bool LpsExchange(ref QeEntry qeEntry, ref JbigArithmeticContextEntry context)
        {
            // Figure E.17

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
            // Figure E.18

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

        public int DecodeInteger(JbigArithmeticContext context)
        {
            var value = DecodeIntegerOrOob(context);
            if (value.IsOob)
            {
                throw new JbigException("Unexpected OOB");
            }

            return value.Value;
        }

        public JbigDecodedValue DecodeIntegerOrOob(JbigArithmeticContext context)
        {
            // Figure A.1

            var prev = 1;

            int Bit()
            {
                context.EntryIndex = prev;

                var result = DecodeBit(context);

                prev = (prev << 1) | result;

                if (prev > 511)
                {
                    prev = (prev & 511) | 256;
                }

                return result;
            }

            var s = Bit();

            int vOffset;
            int vBitCount;

            if (Bit() == 0)
            {
                vOffset = 0;
                vBitCount = 2;
            }
            else if (Bit() == 0)
            {
                vOffset = 4;
                vBitCount = 4;
            }
            else if (Bit() == 0)
            {
                vOffset = 20;
                vBitCount = 6;
            }
            else if (Bit() == 0)
            {
                vOffset = 84;
                vBitCount = 8;
            }
            else if (Bit() == 0)
            {
                vOffset = 340;
                vBitCount = 12;
            }
            else
            {
                vOffset = 4436;
                vBitCount = 32;
            }

            var v = 0;
            while (vBitCount-- > 0) v = (v << 1) | Bit();
            v += vOffset;

            if (s == 0)
            {
                return new JbigDecodedValue(v);
            }
            else if (v != 0)
            {
                return new JbigDecodedValue(-v);
            }
            else
            {
                return JbigDecodedValue.Oob;
            }
        }

        public int DecodeSymbol(JbigArithmeticContext context, int codeLength)
        {
            // Section A.3

            var prev = 1;

            for (var i = 0; i < codeLength; i++)
            {
                context.EntryIndex = prev;
                var D = DecodeBit(context);
                prev = (prev << 1) | D;
            }

            prev -= 1 << codeLength;

            return prev;
        }

        public int DecodeBit(JbigArithmeticContext context)
        {
            // Figure G.2

            ref var qeEntry = ref qe[context.Current.Index];
            bool result;

            a = a - qeEntry.Value;

            var Chigh = c >> 16;

            if (Chigh < a)
            {
                if ((a & 0x8000) == 0)
                {
                    result = MpsExchange(ref qeEntry, ref context.Current);
                    Renormd();
                }
                else
                {
                    result = context.Current.Mps;
                }
            }
            else
            {
                // Chigh = Chigh - A
                c -= a << 16;

                result = LpsExchange(ref qeEntry, ref context.Current);
                Renormd();
            }

            return result ? 1 : 0;
        }
    }
}

