using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal class OperationAttribute : Attribute
    {
        public OperationAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
