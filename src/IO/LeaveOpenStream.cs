using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.IO
{
    /// <summary>
    /// Proxies all calls to the base stream but leaves the base stream
    /// open when the <see cref="LeaveOpenStream"/> is disposed.
    /// </summary>
    internal class LeaveOpenStream : Stream
    {
        private readonly Stream baseStream;

        public LeaveOpenStream(Stream baseStream)
        {
            this.baseStream = baseStream;
        }

        public override bool CanRead => baseStream.CanRead;

        public override bool CanSeek => baseStream.CanSeek;

        public override bool CanWrite => baseStream.CanWrite;

        public override long Length => baseStream.Length;

        public override long Position
        {
            get => baseStream.Position;
            set => baseStream.Position = value;
        }

        public override void Flush()
            => baseStream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken)
            => baseStream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count)
            => baseStream.Read(buffer, offset, count);

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => baseStream.BeginRead(buffer, offset, count, callback, state);

        public override int EndRead(IAsyncResult asyncResult)
            => baseStream.EndRead(asyncResult);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => baseStream.ReadAsync(buffer, offset, count, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin)
            => baseStream.Seek(offset, origin);

        public override void SetLength(long value)
            => baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
            => baseStream.Write(buffer, offset, count);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => baseStream.BeginWrite(buffer, offset, count, callback, state);

        public override void EndWrite(IAsyncResult asyncResult)
            => baseStream.EndWrite(asyncResult);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => baseStream.WriteAsync(buffer, offset, count, cancellationToken);
    }
}
