// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
#endif

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegArrayUtils
    {
        public static void Cast(float[] destinationArray, short[] sourceArray, int length)
        {
            if (destinationArray == null)
            {
                throw new ArgumentNullException(nameof(destinationArray));
            }
            if (sourceArray == null)
            {
                throw new ArgumentNullException(nameof(sourceArray));
            }
            if (length < 0 ||
                length > sourceArray.Length ||
                length > destinationArray.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                // Vectorized 256 bit batches
                const int VectorSize = 256;
                const int ElementSize = 16;
                const int ElementBatchSize = VectorSize / ElementSize;

                var cursor = 0;

                ref var pSource = ref MemoryMarshal.GetArrayDataReference(sourceArray);
                ref var pDestination = ref MemoryMarshal.GetArrayDataReference(destinationArray);

                for (; cursor + ElementBatchSize <= length; cursor += ElementBatchSize)
                {
                    var sourceBatch = Unsafe.As<short, Vector256<short>>(ref Unsafe.Add(ref pSource, cursor));

                    var (lower, upper) = JpegVectorUtils.ConvertToVector256Single(sourceBatch);

                    ref var pDestinationBatch = ref Unsafe.As<float, Vector256<float>>(
                        ref Unsafe.Add(ref pDestination, cursor));

                    Unsafe.Add(ref pDestinationBatch, 0) = lower;
                    Unsafe.Add(ref pDestinationBatch, 1) = upper;
                }

                // Scalar remainder
                for (; cursor < length; cursor++)
                {
                    Unsafe.Add(ref pDestination, cursor) = Unsafe.Add(ref pSource, cursor);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                // Vectorized 128 bit batches
                const int VectorSize = 128;
                const int ElementSize = 16;
                const int ElementBatchSize = VectorSize / ElementSize;

                var cursor = 0;

                ref var pSource = ref MemoryMarshal.GetArrayDataReference(sourceArray);
                ref var pDestination = ref MemoryMarshal.GetArrayDataReference(destinationArray);

                for (; cursor + ElementBatchSize <= length; cursor += ElementBatchSize)
                {
                    var sourceBatch = Unsafe.As<short, Vector128<short>>(ref Unsafe.Add(ref pSource, cursor));

                    var (lower, upper) = JpegVectorUtils.ConvertToVector128Single(sourceBatch);

                    ref var pDestinationBatch = ref Unsafe.As<float, Vector128<float>>(
                        ref Unsafe.Add(ref pDestination, cursor));

                    Unsafe.Add(ref pDestinationBatch, 0) = lower;
                    Unsafe.Add(ref pDestinationBatch, 1) = upper;
                }

                // Scalar remainder
                for (; cursor < length; cursor++)
                {
                    Unsafe.Add(ref pDestination, cursor) = Unsafe.Add(ref pSource, cursor);
                }
            }
            else
#endif
            {
                if (length == sourceArray.Length)
                {
                    // Optimized for removed range checks
                    for (var cursor = 0; cursor < sourceArray.Length; cursor++)
                    {
                        destinationArray[cursor] = sourceArray[cursor];
                    }
                }
                else
                {
                    for (var cursor = 0; cursor < length; cursor++)
                    {
                        destinationArray[cursor] = sourceArray[cursor];
                    }
                }
            }
        }
    }
}
