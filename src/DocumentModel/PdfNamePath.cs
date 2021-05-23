using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal class PdfNamePath : IEnumerable<PdfName>
    {
        private PdfName[] path;

        public PdfNamePath(params PdfName[] names) : this(names, true) { }

        private PdfNamePath(PdfName[] names, bool copy)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));

            if (copy)
            {
                path = new PdfName[names.Length];
                names.CopyTo(this.path, 0);
            }
            else
            {
                path = names;
            }
        }

        public IEnumerator<PdfName> GetEnumerator()
        {
            return path.Cast<PdfName>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return path.GetEnumerator();
        }

        public static implicit operator PdfNamePath(PdfName name)
        {
            return new PdfNamePath(new [] { name }, false);
        }

        public static PdfNamePath operator /(PdfNamePath path, PdfName name)
        {
            var newPath = new PdfName[path.path.Length + 1];
            path.path.CopyTo(newPath, 0);
            newPath[path.path.Length - 1] = name;
            return new PdfNamePath(newPath, false);
        }

        public static PdfNamePath operator /(PdfNamePath path1, PdfNamePath path2)
        {
            var newPath = new PdfName[path1.path.Length + path2.path.Length];
            path1.path.CopyTo(newPath, path1.path.Length);
            path2.path.CopyTo(newPath, 0);
            return new PdfNamePath(newPath, false);
        }
    }
}
