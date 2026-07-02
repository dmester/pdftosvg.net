// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegColorSpaceTransform
    {
        private const int BlockSize = 64;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void YccToRgb(
            float yccY,
            float yccCb,
            float yccCr,
            out float rgbR,
            out float rgbG,
            out float rgbB)
        {
            // The relationship between RGB and YCC is documented in section 13.2:
            // https://www.pdfa.org/norm-refs/5116.DCT_Filter.pdf

            rgbR = yccY + 1.4020f * yccCr - 179.456f;
            rgbG = yccY - 0.3441363f * yccCb - 0.71413636f * yccCr + 135.45890048f;
            rgbB = yccY + 1.772f * yccCb - 226.816f;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void RgbToYcc(
            float rgbR,
            float rgbG,
            float rgbB,
            out float yccY,
            out float yccCb,
            out float yccCr)
        {
            // The relationship between RGB and YCC is documented in section 13.2:
            // https://www.pdfa.org/norm-refs/5116.DCT_Filter.pdf

            yccY = .299f * rgbR + .587f * rgbG + .114f * rgbB;
            yccCb = -.168736f * rgbR - .331264f * rgbG + .500f * rgbB + 128;
            yccCr = .500f * rgbR - .4186876f * rgbG - .08131241f * rgbB + 128;
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void YccToRgb(
            Vector256<float> yccY,
            Vector256<float> yccCb,
            Vector256<float> yccCr,
            out Vector256<float> rgbR,
            out Vector256<float> rgbG,
            out Vector256<float> rgbB)
        {
            rgbR = yccY + 1.4020f * yccCr - Vector256.Create(179.456f);
            rgbG = yccY - 0.3441363f * yccCb - 0.71413636f * yccCr + Vector256.Create(135.45890048f);
            rgbB = yccY + 1.772f * yccCb - Vector256.Create(226.816f);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void YccToRgb(
            Vector128<float> yccY,
            Vector128<float> yccCb,
            Vector128<float> yccCr,
            out Vector128<float> rgbR,
            out Vector128<float> rgbG,
            out Vector128<float> rgbB)
        {
            rgbR = yccY + 1.4020f * yccCr - Vector128.Create(179.456f);
            rgbG = yccY - 0.3441363f * yccCb - 0.71413636f * yccCr + Vector128.Create(135.45890048f);
            rgbB = yccY + 1.772f * yccCb - Vector128.Create(226.816f);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void RgbToYcc(
            Vector256<float> rgbR,
            Vector256<float> rgbG,
            Vector256<float> rgbB,
            out Vector256<float> yccY,
            out Vector256<float> yccCb,
            out Vector256<float> yccCr)
        {
            yccY = .299f * rgbR + .587f * rgbG + .114f * rgbB;
            yccCb = -.168736f * rgbR - .331264f * rgbG + .500f * rgbB + Vector256.Create(128f);
            yccCr = .500f * rgbR - .4186876f * rgbG - .08131241f * rgbB + Vector256.Create(128f);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void RgbToYcc(
            Vector128<float> rgbR,
            Vector128<float> rgbG,
            Vector128<float> rgbB,
            out Vector128<float> yccY,
            out Vector128<float> yccCb,
            out Vector128<float> yccCr)
        {
            yccY = .299f * rgbR + .587f * rgbG + .114f * rgbB;
            yccCb = -.168736f * rgbR - .331264f * rgbG + .500f * rgbB + Vector128.Create(128f);
            yccCr = .500f * rgbR - .4186876f * rgbG - .08131241f * rgbB + Vector128.Create(128f);
        }
#endif

        public static int RgbToYcc(short[] data, int offset, int count)
        {
            for (var inputCursor = 0; inputCursor + 2 < count; inputCursor += 3)
            {
                var rgbR = data[offset + inputCursor + 0];
                var rgbG = data[offset + inputCursor + 1];
                var rgbB = data[offset + inputCursor + 2];

                RgbToYcc(rgbR, rgbG, rgbB, out var yccY, out var yccCb, out var yccCr);

                data[offset + inputCursor + 0] = ClampSample(yccY);
                data[offset + inputCursor + 1] = ClampSample(yccCb);
                data[offset + inputCursor + 2] = ClampSample(yccCr);
            }

            return count;
        }

        /// <summary>
        /// Converts de-interleaved CMYK blocks to de-interleaved YCbCr blocks in place. Each group of 4 consecutive
        /// input blocks (C, M, Y, K) is replaced by 3 output blocks (Y', Cb', Cr'). This is safe to do in place, since
        /// the 3 output blocks per MCU never extend beyond the 4 input blocks. Returns the number of output blocks.
        /// </summary>
        public static int CmykBlocksToYcc(short[] blocks, int blockCount)
        {
            const int CmykComponents = 4;
            const int YccComponents = 3;

            var outputBlockIndex = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
            {
                // AVX2
                const int RowsPerBlock = 4; // 64 shorts / 16 shorts per Vector256<short>

                ref var pData = ref Unsafe.As<short, Vector256<short>>(ref MemoryMarshal.GetArrayDataReference(blocks));

                for (var blockIndex = 0; blockIndex < blockCount; blockIndex += CmykComponents, outputBlockIndex += YccComponents)
                {
                    ref var pCmykC = ref Unsafe.Add(ref pData, RowsPerBlock * blockIndex);
                    ref var pCmykM = ref Unsafe.Add(ref pCmykC, RowsPerBlock);
                    ref var pCmykY = ref Unsafe.Add(ref pCmykC, RowsPerBlock * 2);
                    ref var pCmykK = ref Unsafe.Add(ref pCmykC, RowsPerBlock * 3);

                    ref var pYccY = ref Unsafe.Add(ref pData, RowsPerBlock * outputBlockIndex);
                    ref var pYccCb = ref Unsafe.Add(ref pYccY, RowsPerBlock);
                    ref var pYccCr = ref Unsafe.Add(ref pYccY, RowsPerBlock * 2);

                    if (JpegBlockUtils.IsSolidBlock256Unsafe(ref pCmykC) &&
                        JpegBlockUtils.IsSolidBlock256Unsafe(ref pCmykM) &&
                        JpegBlockUtils.IsSolidBlock256Unsafe(ref pCmykY) &&
                        JpegBlockUtils.IsSolidBlock256Unsafe(ref pCmykK))
                    {
                        DeviceCmykColorSpace.ToRgb(
                            pCmykC[0] * (1f / 255), pCmykM[0] * (1f / 255),
                            pCmykY[0] * (1f / 255), pCmykK[0] * (1f / 255),
                            out var rgbR, out var rgbG, out var rgbB);

                        RgbToYcc(rgbR * 255, rgbG * 255, rgbB * 255, out var yccY, out var yccCb, out var yccCr);

                        JpegBlockUtils.FillSolidBlock256Unsafe(ref pYccY, ClampSample(yccY));
                        JpegBlockUtils.FillSolidBlock256Unsafe(ref pYccCb, ClampSample(yccCb));
                        JpegBlockUtils.FillSolidBlock256Unsafe(ref pYccCr, ClampSample(yccCr));
                    }
                    else
                    {
                        for (var row = 0; row < RowsPerBlock; row++)
                        {
                            var (cmykC_lo, cmykC_hi) = Vector256.Widen(Unsafe.Add(ref pCmykC, row));
                            var (cmykM_lo, cmykM_hi) = Vector256.Widen(Unsafe.Add(ref pCmykM, row));
                            var (cmykY_lo, cmykY_hi) = Vector256.Widen(Unsafe.Add(ref pCmykY, row));
                            var (cmykK_lo, cmykK_hi) = Vector256.Widen(Unsafe.Add(ref pCmykK, row));

                            DeviceCmykColorSpace.ToRgb(
                                Vector256.ConvertToSingle(cmykC_lo) * (1f / 255),
                                Vector256.ConvertToSingle(cmykM_lo) * (1f / 255),
                                Vector256.ConvertToSingle(cmykY_lo) * (1f / 255),
                                Vector256.ConvertToSingle(cmykK_lo) * (1f / 255),
                                out var rgbR_lo, out var rgbG_lo, out var rgbB_lo);

                            RgbToYcc(rgbR_lo * 255f, rgbG_lo * 255f, rgbB_lo * 255f,
                                out var yccY_lo, out var yccCb_lo, out var yccCr_lo);

                            DeviceCmykColorSpace.ToRgb(
                                Vector256.ConvertToSingle(cmykC_hi) * (1f / 255),
                                Vector256.ConvertToSingle(cmykM_hi) * (1f / 255),
                                Vector256.ConvertToSingle(cmykY_hi) * (1f / 255),
                                Vector256.ConvertToSingle(cmykK_hi) * (1f / 255),
                                out var rgbR_hi, out var rgbG_hi, out var rgbB_hi);

                            RgbToYcc(rgbR_hi * 255f, rgbG_hi * 255f, rgbB_hi * 255f,
                                out var yccY_hi, out var yccCb_hi, out var yccCr_hi);

                            ClampSamplesAvx2(
                                ref Unsafe.Add(ref pYccY, row), yccY_lo, yccY_hi,
                                ref Unsafe.Add(ref pYccCb, row), yccCb_lo, yccCb_hi,
                                ref Unsafe.Add(ref pYccCr, row), yccCr_lo, yccCr_hi
                                );
                        }
                    }
                }

                return outputBlockIndex;
            }

            if (Vector128.IsHardwareAccelerated && Sse2.IsSupported)
            {
                // SSE2
                const int RowsPerBlock = 8; // 64 shorts / 8 shorts per Vector128<short>

                ref var pData = ref Unsafe.As<short, Vector128<short>>(ref MemoryMarshal.GetArrayDataReference(blocks));

                for (var blockIndex = 0; blockIndex < blockCount; blockIndex += CmykComponents, outputBlockIndex += YccComponents)
                {
                    ref var pCmykC = ref Unsafe.Add(ref pData, RowsPerBlock * blockIndex);
                    ref var pCmykM = ref Unsafe.Add(ref pCmykC, RowsPerBlock);
                    ref var pCmykY = ref Unsafe.Add(ref pCmykC, RowsPerBlock * 2);
                    ref var pCmykK = ref Unsafe.Add(ref pCmykC, RowsPerBlock * 3);

                    ref var pYccY = ref Unsafe.Add(ref pData, RowsPerBlock * outputBlockIndex);
                    ref var pYccCb = ref Unsafe.Add(ref pYccY, RowsPerBlock);
                    ref var pYccCr = ref Unsafe.Add(ref pYccY, RowsPerBlock * 2);

                    if (JpegBlockUtils.IsSolidBlock128Unsafe(ref pCmykC) &&
                        JpegBlockUtils.IsSolidBlock128Unsafe(ref pCmykM) &&
                        JpegBlockUtils.IsSolidBlock128Unsafe(ref pCmykY) &&
                        JpegBlockUtils.IsSolidBlock128Unsafe(ref pCmykK))
                    {
                        DeviceCmykColorSpace.ToRgb(
                            pCmykC[0] * (1f / 255), pCmykM[0] * (1f / 255),
                            pCmykY[0] * (1f / 255), pCmykK[0] * (1f / 255),
                            out var rgbR, out var rgbG, out var rgbB);

                        RgbToYcc(rgbR * 255, rgbG * 255, rgbB * 255, out var yccY, out var yccCb, out var yccCr);

                        JpegBlockUtils.FillSolidBlock128Unsafe(ref pYccY, ClampSample(yccY));
                        JpegBlockUtils.FillSolidBlock128Unsafe(ref pYccCb, ClampSample(yccCb));
                        JpegBlockUtils.FillSolidBlock128Unsafe(ref pYccCr, ClampSample(yccCr));
                    }
                    else
                    {
                        for (var row = 0; row < RowsPerBlock; row++)
                        {
                            var (cmykC_lo, cmykC_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pCmykC, row));
                            var (cmykM_lo, cmykM_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pCmykM, row));
                            var (cmykY_lo, cmykY_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pCmykY, row));
                            var (cmykK_lo, cmykK_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pCmykK, row));

                            DeviceCmykColorSpace.ToRgb(
                                cmykC_lo * (1f / 255), cmykM_lo * (1f / 255),
                                cmykY_lo * (1f / 255), cmykK_lo * (1f / 255),
                                out var rgbR_lo, out var rgbG_lo, out var rgbB_lo);

                            RgbToYcc(rgbR_lo * 255f, rgbG_lo * 255f, rgbB_lo * 255f,
                                out var yccY_lo, out var yccCb_lo, out var yccCr_lo);

                            DeviceCmykColorSpace.ToRgb(
                                cmykC_hi * (1f / 255), cmykM_hi * (1f / 255),
                                cmykY_hi * (1f / 255), cmykK_hi * (1f / 255),
                                out var rgbR_hi, out var rgbG_hi, out var rgbB_hi);

                            RgbToYcc(rgbR_hi * 255f, rgbG_hi * 255f, rgbB_hi * 255f,
                                out var yccY_hi, out var yccCb_hi, out var yccCr_hi);

                            ClampSamplesSse2(
                                ref Unsafe.Add(ref pYccY, row), yccY_lo, yccY_hi,
                                ref Unsafe.Add(ref pYccCb, row), yccCb_lo, yccCb_hi,
                                ref Unsafe.Add(ref pYccCr, row), yccCr_lo, yccCr_hi
                                );
                        }
                    }
                }

                return outputBlockIndex;
            }

#endif
            // Scalar

            for (var blockIndex = 0; blockIndex < blockCount; blockIndex += CmykComponents, outputBlockIndex += YccComponents)
            {
                var cBase = blockIndex * BlockSize;
                var mBase = (blockIndex + 1) * BlockSize;
                var yBase = (blockIndex + 2) * BlockSize;
                var kBase = (blockIndex + 3) * BlockSize;

                var yccYBase = outputBlockIndex * BlockSize;
                var yccCbBase = (outputBlockIndex + 1) * BlockSize;
                var yccCrBase = (outputBlockIndex + 2) * BlockSize;

                if (JpegBlockUtils.IsSolidBlockScalar(blocks, cBase) &&
                    JpegBlockUtils.IsSolidBlockScalar(blocks, mBase) &&
                    JpegBlockUtils.IsSolidBlockScalar(blocks, yBase) &&
                    JpegBlockUtils.IsSolidBlockScalar(blocks, kBase))
                {
                    DeviceCmykColorSpace.ToRgb(
                        blocks[cBase] * (1f / 255), blocks[mBase] * (1f / 255),
                        blocks[yBase] * (1f / 255), blocks[kBase] * (1f / 255),
                        out var rgbR, out var rgbG, out var rgbB);

                    RgbToYcc(rgbR * 255, rgbG * 255, rgbB * 255, out var yccY, out var yccCb, out var yccCr);

                    JpegBlockUtils.FillSolidBlockScalar(blocks, yccYBase, ClampSample(yccY));
                    JpegBlockUtils.FillSolidBlockScalar(blocks, yccCbBase, ClampSample(yccCb));
                    JpegBlockUtils.FillSolidBlockScalar(blocks, yccCrBase, ClampSample(yccCr));
                }
                else
                {
                    for (var i = 0; i < BlockSize; i++)
                    {
                        DeviceCmykColorSpace.ToRgb(
                            blocks[cBase + i] * (1f / 255), blocks[mBase + i] * (1f / 255),
                            blocks[yBase + i] * (1f / 255), blocks[kBase + i] * (1f / 255),
                            out var rgbR, out var rgbG, out var rgbB);

                        RgbToYcc(rgbR * 255, rgbG * 255, rgbB * 255, out var yccY, out var yccCb, out var yccCr);

                        blocks[yccYBase + i] = ClampSample(yccY);
                        blocks[yccCbBase + i] = ClampSample(yccCb);
                        blocks[yccCrBase + i] = ClampSample(yccCr);
                    }
                }
            }

            return outputBlockIndex;
        }

        /// <summary>
        /// Converts de-interleaved YCCK blocks to de-interleaved YCbCr blocks in place. Each group of 4 consecutive
        /// input blocks (Y, Cb, Cr, K) is replaced by 3 output blocks (Y', Cb', Cr'). Returns the number of output blocks.
        /// </summary>
        public static int YcckBlocksToYcc(short[] blocks, int blockCount)
        {
            const int YcckComponents = 4;
            const int YccComponents = 3;

            var outputBlockIndex = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
            {
                // AVX2
                const int RowsPerBlock = 4;

                ref var pData = ref Unsafe.As<short, Vector256<short>>(ref MemoryMarshal.GetArrayDataReference(blocks));

                for (var blockIndex = 0; blockIndex < blockCount; blockIndex += YcckComponents, outputBlockIndex += YccComponents)
                {
                    ref var pYcckY = ref Unsafe.Add(ref pData, RowsPerBlock * blockIndex);
                    ref var pYcckCb = ref Unsafe.Add(ref pYcckY, RowsPerBlock);
                    ref var pYcckCr = ref Unsafe.Add(ref pYcckY, RowsPerBlock * 2);
                    ref var pYcckK = ref Unsafe.Add(ref pYcckY, RowsPerBlock * 3);

                    ref var pYccY = ref Unsafe.Add(ref pData, RowsPerBlock * outputBlockIndex);
                    ref var pYccCb = ref Unsafe.Add(ref pYccY, RowsPerBlock);
                    ref var pYccCr = ref Unsafe.Add(ref pYccY, RowsPerBlock * 2);

                    if (JpegBlockUtils.IsSolidBlock256Unsafe(ref pYcckY) &&
                        JpegBlockUtils.IsSolidBlock256Unsafe(ref pYcckCb) &&
                        JpegBlockUtils.IsSolidBlock256Unsafe(ref pYcckCr) &&
                        JpegBlockUtils.IsSolidBlock256Unsafe(ref pYcckK))
                    {
                        YcckToYcc(pYcckY[0], pYcckCb[0], pYcckCr[0], pYcckK[0], out var yccY, out var yccCb, out var yccCr);

                        JpegBlockUtils.FillSolidBlock256Unsafe(ref pYccY, ClampSample(yccY));
                        JpegBlockUtils.FillSolidBlock256Unsafe(ref pYccCb, ClampSample(yccCb));
                        JpegBlockUtils.FillSolidBlock256Unsafe(ref pYccCr, ClampSample(yccCr));
                    }
                    else
                    {
                        for (var row = 0; row < RowsPerBlock; row++)
                        {
                            var (ycckY_lo, ycckY_hi) = Vector256.Widen(Unsafe.Add(ref pYcckY, row));
                            var (ycckCb_lo, ycckCb_hi) = Vector256.Widen(Unsafe.Add(ref pYcckCb, row));
                            var (ycckCr_lo, ycckCr_hi) = Vector256.Widen(Unsafe.Add(ref pYcckCr, row));
                            var (ycckK_lo, ycckK_hi) = Vector256.Widen(Unsafe.Add(ref pYcckK, row));

                            YcckToYcc(
                                Vector256.ConvertToSingle(ycckY_lo), Vector256.ConvertToSingle(ycckCb_lo),
                                Vector256.ConvertToSingle(ycckCr_lo), Vector256.ConvertToSingle(ycckK_lo),
                                out var yccY_lo, out var yccCb_lo, out var yccCr_lo);

                            YcckToYcc(
                                Vector256.ConvertToSingle(ycckY_hi), Vector256.ConvertToSingle(ycckCb_hi),
                                Vector256.ConvertToSingle(ycckCr_hi), Vector256.ConvertToSingle(ycckK_hi),
                                out var yccY_hi, out var yccCb_hi, out var yccCr_hi);

                            ClampSamplesAvx2(
                                ref Unsafe.Add(ref pYccY, row), yccY_lo, yccY_hi,
                                ref Unsafe.Add(ref pYccCb, row), yccCb_lo, yccCb_hi,
                                ref Unsafe.Add(ref pYccCr, row), yccCr_lo, yccCr_hi
                                );
                        }
                    }
                }

                return outputBlockIndex;
            }

            if (Vector128.IsHardwareAccelerated && Sse2.IsSupported)
            {
                // SSE2
                const int RowsPerBlock = 8;

                ref var pData = ref Unsafe.As<short, Vector128<short>>(ref MemoryMarshal.GetArrayDataReference(blocks));

                for (var blockIndex = 0; blockIndex < blockCount; blockIndex += YcckComponents, outputBlockIndex += YccComponents)
                {
                    ref var pYcckY = ref Unsafe.Add(ref pData, RowsPerBlock * blockIndex);
                    ref var pYcckCb = ref Unsafe.Add(ref pYcckY, RowsPerBlock);
                    ref var pYcckCr = ref Unsafe.Add(ref pYcckY, RowsPerBlock * 2);
                    ref var pYcckK = ref Unsafe.Add(ref pYcckY, RowsPerBlock * 3);

                    ref var pYccY = ref Unsafe.Add(ref pData, RowsPerBlock * outputBlockIndex);
                    ref var pYccCb = ref Unsafe.Add(ref pYccY, RowsPerBlock);
                    ref var pYccCr = ref Unsafe.Add(ref pYccY, RowsPerBlock * 2);

                    if (JpegBlockUtils.IsSolidBlock128Unsafe(ref pYcckY) &&
                        JpegBlockUtils.IsSolidBlock128Unsafe(ref pYcckCb) &&
                        JpegBlockUtils.IsSolidBlock128Unsafe(ref pYcckCr) &&
                        JpegBlockUtils.IsSolidBlock128Unsafe(ref pYcckK))
                    {
                        YcckToYcc(pYcckY[0], pYcckCb[0], pYcckCr[0], pYcckK[0], out var yccY, out var yccCb, out var yccCr);

                        var vYccY = Vector128.Create(ClampSample(yccY));
                        var vYccCb = Vector128.Create(ClampSample(yccCb));
                        var vYccCr = Vector128.Create(ClampSample(yccCr));

                        JpegBlockUtils.FillSolidBlock128Unsafe(ref pYccY, ClampSample(yccY));
                        JpegBlockUtils.FillSolidBlock128Unsafe(ref pYccCb, ClampSample(yccCb));
                        JpegBlockUtils.FillSolidBlock128Unsafe(ref pYccCr, ClampSample(yccCr));
                    }
                    else
                    {
                        for (var row = 0; row < RowsPerBlock; row++)
                        {
                            var (ycckY_lo, ycckY_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pYcckY, row));
                            var (ycckCb_lo, ycckCb_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pYcckCb, row));
                            var (ycckCr_lo, ycckCr_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pYcckCr, row));
                            var (ycckK_lo, ycckK_hi) = JpegVectorUtils.ConvertToVector128Single(Unsafe.Add(ref pYcckK, row));

                            YcckToYcc(ycckY_lo, ycckCb_lo, ycckCr_lo, ycckK_lo,
                                out var yccY_lo, out var yccCb_lo, out var yccCr_lo);

                            YcckToYcc(ycckY_hi, ycckCb_hi, ycckCr_hi, ycckK_hi,
                                out var yccY_hi, out var yccCb_hi, out var yccCr_hi);

                            ClampSamplesSse2(
                                ref Unsafe.Add(ref pYccY, row), yccY_lo, yccY_hi,
                                ref Unsafe.Add(ref pYccCb, row), yccCb_lo, yccCb_hi,
                                ref Unsafe.Add(ref pYccCr, row), yccCr_lo, yccCr_hi
                                );
                        }
                    }
                }

                return outputBlockIndex;
            }
#endif

            // Scalar
            for (var blockIndex = 0; blockIndex < blockCount; blockIndex += YcckComponents, outputBlockIndex += YccComponents)
            {
                var ycckYBase = blockIndex * BlockSize;
                var ycckCbBase = (blockIndex + 1) * BlockSize;
                var ycckCrBase = (blockIndex + 2) * BlockSize;
                var ycckKBase = (blockIndex + 3) * BlockSize;

                var yccYBase = outputBlockIndex * BlockSize;
                var yccCbBase = (outputBlockIndex + 1) * BlockSize;
                var yccCrBase = (outputBlockIndex + 2) * BlockSize;

                if (JpegBlockUtils.IsSolidBlockScalar(blocks, ycckYBase) &&
                    JpegBlockUtils.IsSolidBlockScalar(blocks, ycckCbBase) &&
                    JpegBlockUtils.IsSolidBlockScalar(blocks, ycckCrBase) &&
                    JpegBlockUtils.IsSolidBlockScalar(blocks, ycckKBase))
                {
                    YcckToYcc(blocks[ycckYBase], blocks[ycckCbBase], blocks[ycckCrBase], blocks[ycckKBase],
                        out var yccY, out var yccCb, out var yccCr);

                    JpegBlockUtils.FillSolidBlockScalar(blocks, yccYBase, ClampSample(yccY));
                    JpegBlockUtils.FillSolidBlockScalar(blocks, yccCbBase, ClampSample(yccCb));
                    JpegBlockUtils.FillSolidBlockScalar(blocks, yccCrBase, ClampSample(yccCr));
                }
                else
                {
                    for (var i = 0; i < BlockSize; i++)
                    {
                        YcckToYcc(blocks[ycckYBase + i], blocks[ycckCbBase + i], blocks[ycckCrBase + i], blocks[ycckKBase + i],
                            out var yccY, out var yccCb, out var yccCr);

                        blocks[yccYBase + i] = ClampSample(yccY);
                        blocks[yccCbBase + i] = ClampSample(yccCb);
                        blocks[yccCrBase + i] = ClampSample(yccCr);
                    }
                }
            }

            return outputBlockIndex;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void YcckToYcc(
            float ycckY,
            float ycckCb,
            float ycckCr,
            float ycckK,
            out float yccY,
            out float yccCb,
            out float yccCr)
        {
            YccToRgb(ycckY, ycckCb, ycckCr, out var ycckR, out var ycckG, out var ycckB);

            // The relationship between CMYK and YCCK is documented in section 13.1:
            // https://www.pdfa.org/norm-refs/5116.DCT_Filter.pdf
            DeviceCmykColorSpace.ToRgb(
                (255 - ycckR) * (1f / 255), (255 - ycckG) * (1f / 255),
                (255 - ycckB) * (1f / 255), ycckK * (1f / 255),
                out var rgbR, out var rgbG, out var rgbB);

            RgbToYcc(rgbR * 255, rgbG * 255, rgbB * 255, out yccY, out yccCb, out yccCr);
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void YcckToYcc(
            Vector256<float> ycckY,
            Vector256<float> ycckCb,
            Vector256<float> ycckCr,
            Vector256<float> ycckK,
            out Vector256<float> yccY,
            out Vector256<float> yccCb,
            out Vector256<float> yccCr)
        {
            YccToRgb(ycckY, ycckCb, ycckCr, out var ycckR, out var ycckG, out var ycckB);

            var v255 = Vector256.Create(255f);

            DeviceCmykColorSpace.ToRgb(
                (v255 - ycckR) * (1f / 255), (v255 - ycckG) * (1f / 255),
                (v255 - ycckB) * (1f / 255), ycckK * (1f / 255),
                out var rgbR, out var rgbG, out var rgbB);

            RgbToYcc(rgbR * 255f, rgbG * 255f, rgbB * 255f, out yccY, out yccCb, out yccCr);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void YcckToYcc(
            Vector128<float> ycckY,
            Vector128<float> ycckCb,
            Vector128<float> ycckCr,
            Vector128<float> ycckK,
            out Vector128<float> yccY,
            out Vector128<float> yccCb,
            out Vector128<float> yccCr)
        {
            YccToRgb(ycckY, ycckCb, ycckCr, out var ycckR, out var ycckG, out var ycckB);

            var v255 = Vector128.Create(255f);

            DeviceCmykColorSpace.ToRgb(
                (v255 - ycckR) * (1f / 255), (v255 - ycckG) * (1f / 255),
                (v255 - ycckB) * (1f / 255), ycckK * (1f / 255),
                out var rgbR, out var rgbG, out var rgbB);

            RgbToYcc(rgbR * 255f, rgbG * 255f, rgbB * 255f, out yccY, out yccCb, out yccCr);
        }
#endif

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void YccToRgb(float[] data, int offset, int count)
        {
            for (var i = 0; i + 2 < count; i += 3)
            {
                var yccY = data[offset + i + 0];
                var yccCb = data[offset + i + 1];
                var yccCr = data[offset + i + 2];

                YccToRgb(yccY, yccCb, yccCr, out var rgbR, out var rgbG, out var rgbB);

                data[offset + i + 0] = MathUtils.Clamp(rgbR, 0f, 255f);
                data[offset + i + 1] = MathUtils.Clamp(rgbG, 0f, 255f);
                data[offset + i + 2] = MathUtils.Clamp(rgbB, 0f, 255f);
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void YcckToCmyk(float[] data, int offset, int count)
        {
            for (var i = 0; i + 3 < count; i += 4)
            {
                var ycckY = data[offset + i + 0];
                var ycckCb = data[offset + i + 1];
                var ycckCr = data[offset + i + 2];

                YccToRgb(ycckY, ycckCb, ycckCr, out var ycckR, out var ycckG, out var ycckB);

                // The relationship between CMYK and YCCK is documented in section 13.1:
                // https://www.pdfa.org/norm-refs/5116.DCT_Filter.pdf
                data[offset + i + 0] = MathUtils.Clamp(255f - ycckR, 0f, 255f);
                data[offset + i + 1] = MathUtils.Clamp(255f - ycckG, 0f, 255f);
                data[offset + i + 2] = MathUtils.Clamp(255f - ycckB, 0f, 255f);
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static short ClampSample(float value)
        {
            return MathUtils.RoundToShort(MathUtils.Clamp(value, 0f, 255f));
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void ClampSamplesAvx2(
            ref Vector256<short> pYccY, Vector256<float> yccY_lo, Vector256<float> yccY_hi,
            ref Vector256<short> pYccCb, Vector256<float> yccCb_lo, Vector256<float> yccCb_hi,
            ref Vector256<short> pYccCr, Vector256<float> yccCr_lo, Vector256<float> yccCr_hi
            )
        {
            var min = Vector256<float>.Zero;
            var max = Vector256.Create(255f);

            Vector256<short> Narrow(Vector256<float> lo, Vector256<float> hi)
            {
                return JpegVectorUtils.ConvertToVector256Int16_Avx2(
                    JpegVectorUtils.ClampNative(lo, min, max),
                    JpegVectorUtils.ClampNative(hi, min, max)
                    );
            }

            pYccY = Narrow(yccY_lo, yccY_hi);
            pYccCb = Narrow(yccCb_lo, yccCb_hi);
            pYccCr = Narrow(yccCr_lo, yccCr_hi);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void ClampSamplesSse2(
            ref Vector128<short> pYccY, Vector128<float> yccY_lo, Vector128<float> yccY_hi,
            ref Vector128<short> pYccCb, Vector128<float> yccCb_lo, Vector128<float> yccCb_hi,
            ref Vector128<short> pYccCr, Vector128<float> yccCr_lo, Vector128<float> yccCr_hi
            )
        {
            var min = Vector128<float>.Zero;
            var max = Vector128.Create(255f);

            Vector128<short> Narrow(Vector128<float> lo, Vector128<float> hi)
            {
                return JpegVectorUtils.ConvertToVector128Int16_Sse2(
                    JpegVectorUtils.ClampNative(lo, min, max),
                    JpegVectorUtils.ClampNative(hi, min, max)
                    );
            }

            pYccY = Narrow(yccY_lo, yccY_hi);
            pYccCb = Narrow(yccCb_lo, yccCb_hi);
            pYccCr = Narrow(yccCr_lo, yccCr_hi);
        }
#endif
    }
}
