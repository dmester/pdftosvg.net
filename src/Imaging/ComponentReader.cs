using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal abstract class ComponentReader
    {
        protected readonly Stream stream;
        protected readonly byte[] buffer;
        protected int bufferLength;
        protected int cursor;

        protected ComponentReader(Stream stream, int bufferSize)
        {
            this.stream = stream;
            this.buffer = new byte[bufferSize];
        }

        class ComponentReaderPartialByte : ComponentReader
        {
            private readonly int bitsPerComponent;
            private int bitOffset;
            private const int BitsPerByte = 8;

            public ComponentReaderPartialByte(Stream stream, int bufferSize, int bitsPerComponent) : base(stream, bufferSize)
            {
                this.bitsPerComponent = bitsPerComponent;
                bitOffset = BitsPerByte - bitsPerComponent;
            }

            public override int Read(float[] buffer, int offset, int count)
            {
                var componentsRead = 0;
                var valueMask = (1 << bitsPerComponent) - 1;

                while (componentsRead < count)
                {
                    if (cursor >= bufferLength)
                    {
                        FillBuffer();

                        if (cursor >= bufferLength)
                        {
                            // End of input
                            break;
                        }
                    }

                    var byteValue = this.buffer[cursor];

                    do
                    {
                        buffer[offset + componentsRead] = (byteValue >> bitOffset) & valueMask;
                        bitOffset -= bitsPerComponent;
                        componentsRead++;

                        if (bitOffset < 0)
                        {
                            bitOffset += BitsPerByte;
                            cursor++;
                            break;
                        }
                    }
                    while (componentsRead < count);
                }

                return componentsRead;
            }
        }

        class ComponentReader8bit : ComponentReader
        {
            public ComponentReader8bit(Stream stream, int bufferSize) : base(stream, bufferSize) { }

            public override int Read(float[] buffer, int offset, int count)
            {
                var componentsRead = 0;

                while (componentsRead < count)
                {
                    if (cursor >= bufferLength)
                    {
                        FillBuffer();

                        if (cursor >= bufferLength)
                        {
                            // End of input
                            break;
                        }
                    }

                    buffer[offset + componentsRead++] = this.buffer[cursor++];
                }

                return componentsRead;
            }
        }

        class ComponentReader16bit : ComponentReader
        {
            public ComponentReader16bit(Stream stream, int bufferSize) : base(stream, bufferSize) { }

            public override int Read(float[] buffer, int offset, int count)
            {
                var componentsRead = 0;

                while (componentsRead < count)
                {
                    if (cursor + 1 >= bufferLength)
                    {
                        FillBuffer();

                        if (cursor + 1 >= bufferLength)
                        {
                            // End of input
                            break;
                        }
                    }

                    buffer[offset + componentsRead] = (this.buffer[cursor] << 8) | this.buffer[cursor + 1];
                    componentsRead++;
                    cursor += 2;
                }

                return componentsRead;
            }
        }

        public static ComponentReader Create(Stream stream, int bitsPerComponent, int componentBufferSize)
        {
            var bufferSizeBytes = (componentBufferSize * bitsPerComponent + 7) / 8;

            switch (bitsPerComponent)
            {
                case 1: 
                    return new ComponentReaderPartialByte(stream, bufferSizeBytes, 1);

                case 2:
                    return new ComponentReaderPartialByte(stream, bufferSizeBytes, 2);

                case 4: 
                    return new ComponentReaderPartialByte(stream, bufferSizeBytes, 4);

                case 8: 
                    return new ComponentReader8bit(stream, bufferSizeBytes);

                case 16: 
                    return new ComponentReader16bit(stream, bufferSizeBytes);

                default:
                    throw new ArgumentException(
                       "Invalid bits per component. Only the values 1, 2, 4, 8 and 16 are supported.",
                       nameof(bitsPerComponent));
            }
        }

        protected void FillBuffer()
        {
            bufferLength = 0;
            cursor = 0;

            while (bufferLength < buffer.Length)
            {
                var read = stream.Read(buffer, bufferLength, this.buffer.Length - bufferLength);
                if (read == 0)
                {
                    break;
                }

                bufferLength += read;
            }
        }

        public abstract int Read(float[] buffer, int offset, int count);
    }
}
