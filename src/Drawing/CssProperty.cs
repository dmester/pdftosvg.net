using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal struct CssProperty : IEquatable<CssProperty>
    {
        public CssProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }

        public bool Equals(CssProperty other) => other.Name == Name && other.Value == Value;
        public override bool Equals(object obj) => obj is CssProperty prop && Equals(prop);

        public override int GetHashCode()
        {
            return Name == null ? 0 : Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name + ": " + Value;
        }
    }
}
