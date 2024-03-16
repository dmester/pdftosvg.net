// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    /// <summary>
    /// Wraps an object and implements IEquatable to consider only reference equal objects equal.
    /// This removes any specialized behavior specified by Equals on the boxed object.
    /// </summary>
    internal class ReferenceEquatableBox : IEquatable<ReferenceEquatableBox?>
    {
        private readonly object boxedObject;

        public ReferenceEquatableBox(object boxedObject)
        {
            this.boxedObject = boxedObject;
        }

        public bool Equals(ReferenceEquatableBox? other)
        {
            if (other == null)
            {
                return boxedObject == null;
            }

            return ReferenceEquals(boxedObject, other.boxedObject);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ReferenceEquatableBox);
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(boxedObject);
        }

        public override string? ToString()
        {
            return boxedObject?.ToString();
        }
    }
}
