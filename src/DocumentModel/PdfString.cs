// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal class PdfString : IEquatable<PdfString?>
    {
        private readonly byte[] data;
        private int hash;

        private PdfString(byte[] data, bool ownData)
        {
            if (ownData)
            {
                this.data = data;
            }
            else
            {
                this.data = new byte[data.Length];
                Buffer.BlockCopy(data, 0, this.data, 0, data.Length);
            }
        }

        public PdfString(byte[] data)
        {
            this.data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, this.data, 0, data.Length);
        }

        public PdfString(MemoryStream stream)
        {
            this.data = stream.ToArray();
        }

        public PdfString(string data)
        {
            this.data = Encoding.ASCII.GetBytes(data);
        }

        public byte this[int index]
        {
            get => data[index];
        }

        public static PdfString Empty { get; } = new PdfString(ArrayUtils.Empty<byte>());

        public int Length => data.Length;

        public static bool operator ==(PdfString? a, PdfString? b)
        {
            if ((object?)a == null) return (object?)b == null;
            if ((object?)b == null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(PdfString? a, PdfString? b) => !(a == b);

        public static PdfString operator +(PdfString? a, PdfString? b)
        {
            if ((object?)a == null) return b ?? Empty;
            if ((object?)b == null) return a;

            var concat = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a.data, 0, concat, 0, a.data.Length);
            Buffer.BlockCopy(b.data, 0, concat, a.data.Length, b.data.Length);
            return new PdfString(concat, true);
        }

        public static PdfString FromUnicode(string str)
        {
            return new PdfString(Encoding.BigEndianUnicode.GetBytes(str), true);
        }

        public bool Equals(PdfString? other)
        {
            if ((object?)other == null)
            {
                return false;
            }
            if (other.data == data)
            {
                return true;
            }
            if (other.data.Length != data.Length ||
                other.hash != 0 && hash != 0 && other.hash != hash)
            {
                return false;
            }

            for (var i = 0; i < data.Length; i++)
            {
                if (other.data[i] != data[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PdfString);
        }

        public override int GetHashCode()
        {
            if (hash == 0 && data != null)
            {
                var result = 0;
                for (var i = 0; i < data.Length && i < 200; i++)
                {
                    result = ((result << 3) | (result >> 29)) ^ data[i];
                }
                hash = result == 0 ? 1 : result;
            }

            return hash;
        }

        public byte[] ToByteArray() => (byte[])data.Clone();

        public override string ToString()
        {
            // PDF spec 1.7, Table 35, page 93
            if (data.Length >= 2 && data[0] == 0xfe && data[1] == 0xff)
            {
                return Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
            }

            return new PdfDocEncoding().GetString(data);
        }

        public string ToString(Encoding encoding) => encoding.GetString(data);
    }
}
