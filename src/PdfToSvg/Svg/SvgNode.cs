// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Svg
{
    internal abstract class SvgNode
    {
        public SvgNode? NextNode;
        public bool HasParent;

        public abstract void WriteTo(SvgTextWriter writer);

#if HAVE_ASYNC
        public virtual Task WriteToAsync(SvgTextWriter writer, CancellationToken cancellationToken)
        {
            WriteTo(writer);

#if NET45
            return Task.FromResult(true);
#else
            return Task.CompletedTask;
#endif
        }
#endif

        /// <summary>
        /// Makes a deep clone of the node.
        /// </summary>
        public abstract SvgNode CloneNode();

        public override string ToString()
        {
            var writer = new SvgStringWriter();
            WriteTo(writer);
            return writer.ToString();
        }
    }
}
