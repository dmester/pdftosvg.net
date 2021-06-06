// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.IO
{
    internal class InputFile : IDisposable
    {
        private Stream? baseStream;
        private SemaphoreSlim? readSemaphore = new SemaphoreSlim(1, 1);

        public InputFile(Stream baseStream)
        {
            this.baseStream = baseStream;
        }

        public InputFile(string path)
        {
            this.baseStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public async Task<BufferedReader> CreateExclusiveReaderAsync(int bufferSize = 4096)
        {
            if (baseStream == null || readSemaphore == null) throw new ObjectDisposedException(nameof(InputFile));

            var reader = new BufferedStreamReader(baseStream, () => readSemaphore?.Release(), bufferSize);
            await readSemaphore.WaitAsync();
            return reader;
        }

        public BufferedReader CreateExclusiveReader(int bufferSize = 4096)
        {
            if (baseStream == null || readSemaphore == null) throw new ObjectDisposedException(nameof(InputFile));

            var reader = new BufferedStreamReader(baseStream, () => readSemaphore?.Release(), bufferSize);
            readSemaphore.Wait();
            return reader;
        }

        public async Task<BufferedReader> CreateExclusiveSliceReaderAsync(long offset, long length, int bufferSize = 4096)
        {
            if (baseStream == null || readSemaphore == null) throw new ObjectDisposedException(nameof(InputFile));

            baseStream.Position = offset;
            var reader = new BufferedStreamReader(baseStream, offset, length, () => readSemaphore?.Release(), bufferSize);
            await readSemaphore.WaitAsync();
            return reader;
        }

        public BufferedReader CreateExclusiveSliceReader(long offset, long length, int bufferSize = 4096)
        {
            if (baseStream == null || readSemaphore == null) throw new ObjectDisposedException(nameof(InputFile));

            baseStream.Position = offset;
            var reader = new BufferedStreamReader(baseStream, offset, length, () => readSemaphore?.Release(), bufferSize);
            readSemaphore.Wait();
            return reader;
        }

        public void Dispose()
        {
            readSemaphore?.Dispose();
            baseStream?.Dispose();

            readSemaphore = null;
            baseStream = null;
        }
    }
}
