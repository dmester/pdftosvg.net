// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Svg;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Svg
{
    public class SvgTextTests
    {
        [TestCase(false, 0)]
        [TestCase(true, 0)]
        [TestCase(false, 10000)]
        [TestCase(true, 10000)]
        public void WriteTo_RawAndEncoded(bool isRaw, int padding)
        {
            var prefix = new string('A', padding);
            var text = new SvgText(prefix + "<&>é", isRaw);
            var expected = prefix + (isRaw ? "<&>é" : "&lt;&amp;&gt;é");

            Assert.AreEqual(expected, text.ToString());
            
            using var stream = new MemoryStream();
            using var streamWriter = new SvgStreamWriter(stream, preferAsync: false);
            
            text.WriteTo(streamWriter);
            streamWriter.Flush();

            Assert.AreEqual(expected, Encoding.UTF8.GetString(stream.ToArray()));
        }

#if !NET40
        [TestCase(false, 0)]
        [TestCase(true, 0)]
        [TestCase(false, 10000)]
        [TestCase(true, 10000)]
        public async Task WriteToAsync_RawAndEncoded(bool isRaw, int padding)
        {
            var prefix = new string('A', padding);
            var text = new SvgText(prefix + "<&>é", isRaw);
            var expected = prefix + (isRaw ? "<&>é" : "&lt;&amp;&gt;é");

            using var writer = new SvgStringWriter();
            await text.WriteToAsync(writer, CancellationToken.None);
            
            Assert.AreEqual(expected, writer.ToString());

            using var asyncStream = new MemoryStream();
            using var asyncWriter = new SvgStreamWriter(asyncStream, preferAsync: true);

            await text.WriteToAsync(asyncWriter, CancellationToken.None);
            await asyncWriter.FlushAsync(CancellationToken.None);

            Assert.AreEqual(expected, Encoding.UTF8.GetString(asyncStream.ToArray()));
        }
#endif
    }
}
