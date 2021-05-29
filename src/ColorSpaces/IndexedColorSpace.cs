using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal class IndexedColorSpace : ColorSpace, IEquatable<IndexedColorSpace>
    {
        private readonly ColorSpace baseSpace;
        private readonly float[] baseBuffer;
        private readonly byte[] lookup;

        public IndexedColorSpace(ColorSpace baseSpace, byte[] lookup)
        {
            this.baseSpace = baseSpace;
            this.baseBuffer = new float[baseSpace.ComponentsPerSample];
            this.lookup = lookup;
        }

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            var index = (int)input[inputOffset++];
            var maxIndexWithValues = Math.Min(baseBuffer.Length, lookup.Length - index * baseBuffer.Length);
            var i = 0;

            for (; i < maxIndexWithValues; i++)
            {
                // TODO This is not correct but will probably work most of the time.
                baseBuffer[i] = lookup[index * baseBuffer.Length + i] * (1f / 255);
            }

            for (; i < baseBuffer.Length; i++)
            {
                baseBuffer[i] = 0f;
            }

            var baseIndex = 0;
            baseSpace.ToRgb(baseBuffer, ref baseIndex, out red, out green, out blue);
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, (1 << bitsPerComponent) - 1f });
        }

        public override int ComponentsPerSample => 1;

        public ColorSpace BaseSpace => baseSpace;

        public override float[] DefaultColor => new[] { 0f };

        public int ColorCount => lookup.Length;

        public override int GetHashCode() => baseSpace.GetHashCode() ^ lookup.Length;
        public override bool Equals(object? obj) => Equals(obj as IndexedColorSpace);
        public bool Equals(IndexedColorSpace? other)
        {
            if (other == null)
            {
                return false;
            }

            if (!ReferenceEquals(this, other))
            {
                if (!other.baseSpace.Equals(baseSpace)) return false;
                if (other.lookup.Length != lookup.Length) return false;

                for (var i = 0; i < lookup.Length && i < other.lookup.Length; i++)
                {
                    if (other.lookup[i] != lookup[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override string ToString() => "Indexed";
    }
}
