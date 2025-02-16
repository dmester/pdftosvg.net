// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PdfToSvg.Imaging.Jbig2.Coding
{
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
        private int offset;
        private int count;

        private const int MaxFinishedStateCounter = 2;
        private int finishedStateCounter;

        static JbigArithmeticDecoder()
        {
            // Table E.1 – Qe values and probability estimation process
            var qeData = new int[]
            {
                0x5601, 1, 1, 1,
                0x3401, 2, 6, 0,
                0x1801, 3, 9, 0,
                0x0AC1, 4, 12, 0,
                0x0521, 5, 29, 0,
                0x0221, 38, 33, 0,
                0x5601, 7, 6, 1,
                0x5401, 8, 14, 0,
                0x4801, 9, 14, 0,
                0x3801, 10, 14, 0,
                0x3001, 11, 17, 0,
                0x2401, 12, 18, 0,
                0x1C01, 13, 20, 0,
                0x1601, 29, 21, 0,
                0x5601, 15, 14, 1,
                0x5401, 16, 14, 0,
                0x5101, 17, 15, 0,
                0x4801, 18, 16, 0,
                0x3801, 19, 17, 0,
                0x3401, 20, 18, 0,
                0x3001, 21, 19, 0,
                0x2801, 22, 19, 0,
                0x2401, 23, 20, 0,
                0x2201, 24, 21, 0,
                0x1C01, 25, 22, 0,
                0x1801, 26, 23, 0,
                0x1601, 27, 24, 0,
                0x1401, 28, 25, 0,
                0x1201, 29, 26, 0,
                0x1101, 30, 27, 0,
                0x0AC1, 31, 28, 0,
                0x09C1, 32, 29, 0,
                0x08A1, 33, 30, 0,
                0x0521, 34, 31, 0,
                0x0441, 35, 32, 0,
                0x02A1, 36, 33, 0,
                0x0221, 37, 34, 0,
                0x0141, 38, 35, 0,
                0x0111, 39, 36, 0,
                0x0085, 40, 37, 0,
                0x0049, 41, 38, 0,
                0x0025, 42, 39, 0,
                0x0015, 43, 40, 0,
                0x0009, 44, 41, 0,
                0x0005, 45, 42, 0,
                0x0001, 45, 43, 0,
                0x5601, 46, 46, 0,
            };

            qe = new QeEntry[qeData.Length / 4];

            for (var i = 0; i < qe.Length; i++)
            {
                qe[i] = new QeEntry(
                    value: (uint)qeData[i * 4],
                    nmps: (byte)qeData[i * 4 + 1],
                    nlps: (byte)qeData[i * 4 + 2],
                    switchValue: qeData[i * 4 + 3] == 1
                    );
            }
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

            reader.GetBuffer(out data, out offset, out count);

            offset += reader.Cursor.Cursor;
            count -= reader.Cursor.Cursor;

            if (count > maxByteCount)
            {
                count = maxByteCount;
            }

            if (count < 2)
            {
                throw new ArgumentException("The reader must contain at least two remaining bytes.", nameof(reader));
            }

            InitDec();
        }

        public JbigArithmeticDecoder(byte[] data, int offset, int count)
        {
            if (offset < 0 || offset + count > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "The arithmetic encoded data must contain at least two bytes.");
            }

            this.data = data ?? throw new ArgumentNullException(nameof(data));
            this.count = count;
            this.offset = offset;

            InitDec();
        }

        private uint PeekByte(int byteOffset)
        {
            byteOffset += this.byteCursor;

            if (byteOffset - this.offset >= this.count)
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

            byteCursor = offset;
            c = ((uint)data[byteCursor] ^ 0xff) << 16;

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
        private bool MpsExchange(ref JbigArithmeticContextEntry context)
        {
            // Figure E.16

            var qeEntry = qe[context.Index];
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
        private bool LpsExchange(ref JbigArithmeticContextEntry context)
        {
            // Figure E.17

            var qeEntry = qe[context.Index];
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

            var qeEntry = qe[context.Current.Index];
            bool result;

            a = a - qeEntry.Value;

            var Chigh = c >> 16;

            if (Chigh < a)
            {
                if ((a & 0x8000) == 0)
                {
                    result = MpsExchange(ref context.Current);
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
                c = ((Chigh - a) << 16) | (c & 0xffffu);

                result = LpsExchange(ref context.Current);
                Renormd();
            }

            return result ? 1 : 0;
        }
    }
}

