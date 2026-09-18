// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Svg
{
    internal class SvgText(string value, bool isRaw = false) : SvgNode
    {
        public readonly string Value = value;

        public override SvgNode CloneNode() => new SvgText(Value, isRaw);

        public override void WriteTo(SvgTextWriter writer)
        {
            if (isRaw)
            {
                writer.WriteRawText(Value);
            }
            else
            {
                writer.WriteEncodedText(Value, escapeQuote: false);
            }
        }

#if HAVE_ASYNC
        public override async Task WriteToAsync(SvgTextWriter writer, CancellationToken cancellationToken)
        {
            if (Value.Length > writer.AsyncLengthThreshold)
            {
                if (isRaw)
                {
                    await writer.WriteRawTextAsync(Value, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteEncodedTextAsync(Value, escapeQuote: false, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                WriteTo(writer);
            }

            if (writer.ShouldFlush)
            {
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
#endif
    }
}
