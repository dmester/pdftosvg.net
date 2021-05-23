using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal struct PdfObjectId : IEquatable<PdfObjectId>
    {
        private readonly int objectNumber;
        private readonly int generation;

        public PdfObjectId(int objectNumber, int generation)
        {
            this.objectNumber = unchecked(objectNumber + 1);
            this.generation = generation;
        }

        public int ObjectNumber => unchecked(objectNumber - 1);

        public int Generation => generation;

        public bool IsEmpty => objectNumber == 0;

        public static PdfObjectId Empty => default;

        public override int GetHashCode() => ObjectNumber;

        public bool Equals(PdfObjectId other)
        {
            return
                other.ObjectNumber == ObjectNumber &&
                other.Generation == Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is PdfObjectId id &&
                id.ObjectNumber == ObjectNumber &&
                id.Generation == Generation;
        }

        public override string ToString() => $"{ObjectNumber} {Generation} R";
    }
}
