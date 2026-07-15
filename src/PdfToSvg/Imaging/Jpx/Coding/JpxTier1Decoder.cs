// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0003 // Remove 'this.' qualification

namespace PdfToSvg.Imaging.Jpx.Coding
{
    /// <summary>
    /// Decodes the codeword segments of a code-block into signed fixed-point quantized coefficients, returned in a
    /// coefficient buffer that is reused between code-blocks.
    /// </summary>
    /// <remarks>
    /// See ITU-T T.800 (06/2019) Annex D
    /// </remarks>
    internal sealed class JpxTier1Decoder
    {
        // Coefficients are signed two's-complement fixed-point integers with one fractional bit (value * 2). One
        // additional magnitude bit is reserved for refining coefficients that saturated above the supported range,
        // leaving 29 bits for coded bit-planes => 28 is the maximum bit-plane index.
        private const int MaxPlane = 28;

        private const int StripeHeight = 4;

        private readonly JpxMqContextEntry[] mqContexts = new JpxMqContextEntry[JpxMqContext.ContextCount];

        // The state array has one coefficient of padding in all directions for edge neighbours
        private JpxTier1CoefficientState[] states = ArrayUtils.Empty<JpxTier1CoefficientState>();
        private int stateStride;

        private JpxCodeBlockSegmentDataPool segmentDataPool = new();

        // Decode state of the code-block being decoded
        private int width;
        private int height;
        private int[] coefficients = ArrayUtils.Empty<int>();
        private byte[] significanceContextLut = JpxTier1LookupTables.GetSignificanceContextTable(JpxSubBandType.LL);
        private bool verticallyCausal;
        private bool useMidpointReconstruction;

        // With midpoint reconstruction, a coefficient becoming significant is placed at the centre of its uncertainty
        // interval (1.5 * 2^plane). Each refinement bit halves the interval (plus or minus 0.5 * 2^plane).
        private int significanceValue;
        private int refinementOneDelta;
        private int refinementZeroDelta;

        /// <summary>
        /// Decodes the accumulated codeword segments of <paramref name="codeBlock"/>, returning the decoded
        /// coefficients, or <c>null</c> when the code-block holds no decodable coding passes.
        /// </summary>
        /// <param name="codeBlock">Code-block to decode</param>
        /// <param name="useMidpointReconstruction">
        /// When <c>true</c>, truncated coefficients are reconstructed at the centre of their uncertainty
        /// interval (reconstruction parameter r = 1/2 in ITU-T T.800 (06/2019) Equation E-6)
        /// </param>
        /// <returns>
        /// Signed quantized coefficients with one fractional bit (value * 2; ITU-T T.800 (06/2019) Equation E-1),
        /// row-major over the Width * Height area of the code-block. The buffer is reused by the decoder: it is only
        /// valid until the next <see cref="Decode"/> call, and may be larger than the code-block area.
        /// </returns>
        public int[]? Decode(JpxCodeBlock codeBlock, bool useMidpointReconstruction)
        {
            var subBand = codeBlock.SubBand;
            var style = codeBlock.CodeBlockStyle;
            var bitPlanes = subBand.MagnitudeBitPlanes - codeBlock.ZeroBitPlanes;

            if (!codeBlock.Included || codeBlock.ZeroBitPlanes < 0 || codeBlock.Segments.Count == 0 ||
                codeBlock.Width < 1 || codeBlock.Height < 1 || bitPlanes < 1)
            {
                return null;
            }

            if (bitPlanes - 1 > MaxPlane)
            {
                // Streams with an ROI up-shift (Equation H-3) can code up to 74 bit-planes
                Log.WriteLine(
                    "JPEG 2000 code-block coded with " + bitPlanes + " bit-planes; " +
                    "magnitudes exceeding the coefficient range will saturate.");
            }

            // Set block state
            this.width = codeBlock.Width;
            this.height = codeBlock.Height;
            this.significanceContextLut = JpxTier1LookupTables.GetSignificanceContextTable(subBand.Type);
            this.verticallyCausal = style.VerticallyCausalContext;
            this.useMidpointReconstruction = useMidpointReconstruction;
            this.stateStride = this.width + 2; // +2 for the horizontal neighbours

            // Equations B-17 and B-18 bound the code-block area to 2^12 coefficients, so the reused buffer stays small
            var minCoefficientCapacity = this.width * this.height;
            if (this.coefficients.Length < minCoefficientCapacity)
            {
                this.coefficients = new int[minCoefficientCapacity];
            }
            else
            {
                Array.Clear(this.coefficients, 0, minCoefficientCapacity);
            }

            var minStateCapacity = this.stateStride * (codeBlock.Height + 2); // +2 for the vertical neighbours
            if (this.states.Length < minStateCapacity)
            {
                this.states = new JpxTier1CoefficientState[minStateCapacity];
            }
            else
            {
                Array.Clear(this.states, 0, minStateCapacity);
            }

            JpxMqContext.Clear(mqContexts);

            DecodeSegments(codeBlock, style, bitPlanes);

            // Also returned after the error paths of DecodeSegments, which keep the coefficients decoded so far
            // (corrupt streams are decoded best-effort)
            return this.coefficients;
        }

        private void DecodeSegments(JpxCodeBlock codeBlock, JpxCodeBlockStyle style, int bitPlanes)
        {
            // Section D.3:
            // The first coded bit-plane holds a cleanup pass only
            var maxPasses = 3 * bitPlanes - 2;
            var plane = bitPlanes - 1;
            var pass = 0;
            ref var segments = ref codeBlock.Segments;

            SetPlaneValues(plane);

            for (var segmentIndex = 0; segmentIndex < segments.Count && pass < maxPasses; segmentIndex++)
            {
                var segment = segments[segmentIndex];
                var segmentPasses = Math.Min(segment.CodingPasses, maxPasses - pass);

                // Note: The packet reader terminates codeword segments at every termination signalled by Tables D.8
                // and D.9, so all coding passes of a segment share the same coding
                if (JpxCodingPass.IsRaw(style, pass))
                {
                    // RAW
                    var bitDecoder = new RawBitDecoder(segmentDataPool.ToArraySegment(ref segment.Data));

                    for (var i = 0; i < segmentPasses; i++, pass++)
                    {
                        if (!JpxCodingPass.IsRaw(style, pass))
                        {
                            Log.WriteLine("JPEG 2000: Unexpected MQ coding pass encountered in a RAW coded segment");
                            return;
                        }

                        switch (JpxCodingPass.GetPassType(pass))
                        {
                            case JpxCodingPassType.SignificancePropagation:
                                DecodeSignificancePropagationPass(ref bitDecoder);
                                break;

                            case JpxCodingPassType.MagnitudeRefinement:
                                DecodeMagnitudeRefinementPass(ref bitDecoder);
                                break;

                            default:
                                Log.WriteLine("JPEG 2000: Unexpected cleanup raw coding pass encountered");
                                return;
                        }
                    }
                }
                else
                {
                    // MQ
                    var bitDecoder = new MqBitDecoder(segmentDataPool.ToArraySegment(ref segment.Data), mqContexts);

                    for (var i = 0; i < segmentPasses; i++, pass++)
                    {
                        if (JpxCodingPass.IsRaw(style, pass))
                        {
                            Log.WriteLine("JPEG 2000: Unexpected raw coding pass encountered in an MQ coded segment");
                            return;
                        }

                        switch (JpxCodingPass.GetPassType(pass))
                        {
                            case JpxCodingPassType.SignificancePropagation:
                                DecodeSignificancePropagationPass(ref bitDecoder);
                                break;

                            case JpxCodingPassType.MagnitudeRefinement:
                                DecodeMagnitudeRefinementPass(ref bitDecoder);
                                break;

                            default:
                                DecodeCleanupPass(ref bitDecoder);

                                // ITU-T T.800 (06/2019) Section D.5: an optional segmentation symbol ends every
                                // bit-plane and confirms its correct decoding
                                if (style.SegmentationSymbolsAreUsed && !DecodeSegmentationSymbol(ref bitDecoder))
                                {
                                    Log.WriteLine(
                                        "Invalid JPEG 2000 segmentation symbol; discarding the " +
                                        "remaining coding passes of the code-block.");
                                    return;
                                }

                                SetPlaneValues(--plane);
                                break;
                        }

                        if (style.ResetContextProbabilitiesOnCodingPassBoundaries)
                        {
                            JpxMqContext.Clear(mqContexts);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Decodes one significance propagation pass: the significance bit, followed upon significance by the sign
        /// bit, of every insignificant coefficient with at least one significant neighbour.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Section D.3.1
        /// </remarks>
        private void DecodeSignificancePropagationPass<TBitDecoder>(ref TBitDecoder bitDecoder)
            where TBitDecoder : struct, IBitDecoder
        {
            var states = this.states;
            var stride = this.stateStride;
            var contextLut = this.significanceContextLut;

            for (var stripeY = 0; stripeY < height; stripeY += StripeHeight)
            {
                var rowsInStripe = Math.Min(StripeHeight, height - stripeY);

                for (var x = 0; x < width; x++)
                {
                    var stateIndex = (stripeY + 1) * stride + x + 1;
                    var coefficientIndex = stripeY * width + x;

                    for (var stripeRow = 0; stripeRow < rowsInStripe; stripeRow++)
                    {
                        ref var coefficientState = ref states[stateIndex];

                        if (coefficientState.IsUndecoded && coefficientState.HasSignificantNeighbors)
                        {
                            if (bitDecoder.DecodeBit(contextLut, coefficientState.SignificanceContextIndex) == 1)
                            {
                                var signBit = bitDecoder.DecodeSignBit(coefficientState.SignContextIndex);
                                MakeSignificant(stateIndex, coefficientIndex, stripeY + stripeRow, signBit);
                            }

                            coefficientState.MarkVisited();
                        }

                        stateIndex += stride;
                        coefficientIndex += width;
                    }
                }
            }
        }

        /// <summary>
        /// Decodes one magnitude refinement pass: one more magnitude bit of every coefficient that was significant
        /// before this bit-plane.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Section D.3.3
        /// </remarks>
        private void DecodeMagnitudeRefinementPass<TBitDecoder>(ref TBitDecoder bitDecoder)
            where TBitDecoder : struct, IBitDecoder
        {
            var states = this.states;
            var stride = stateStride;

            for (var stripeY = 0; stripeY < height; stripeY += StripeHeight)
            {
                var rowsInStripe = Math.Min(StripeHeight, height - stripeY);

                for (var x = 0; x < width; x++)
                {
                    var stateIndex = (stripeY + 1) * stride + x + 1;
                    var coefficientIndex = stripeY * width + x;

                    for (var stripeRow = 0; stripeRow < rowsInStripe; stripeRow++)
                    {
                        ref var coefficientState = ref states[stateIndex];

                        if (coefficientState.NeedsRefinement)
                        {
                            // The refinement bit halves the uncertainty interval of the coefficient, moving its
                            // magnitude to the centre of either half
                            var magnitudeDelta = bitDecoder.DecodeRefinementBit(coefficientState) == 1
                                ? refinementOneDelta
                                : refinementZeroDelta;

                            ref var coefficient = ref coefficients[coefficientIndex];
                            coefficient += coefficient < 0
                                ? -magnitudeDelta
                                : magnitudeDelta;

                            coefficientState.MarkRefined();
                        }

                        stateIndex += stride;
                        coefficientIndex += width;
                    }
                }
            }
        }

        /// <summary>
        /// Decodes one cleanup pass: the significance bits of all coefficients not covered by the two other passes of
        /// the bit-plane, with run-length coding of stripe columns of insignificant coefficients without significant
        /// neighbours. Ends the bit-plane by clearing the per-bit-plane visited state.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Section D.3.4
        /// </remarks>
        private void DecodeCleanupPass<TBitDecoder>(ref TBitDecoder bitDecoder)
            where TBitDecoder : struct, IBitDecoder
        {
            var states = this.states;
            var stride = this.stateStride;
            var contexts = this.significanceContextLut;

            for (var stripeY = 0; stripeY < height; stripeY += StripeHeight)
            {
                var rowsInStripe = Math.Min(StripeHeight, height - stripeY);

                for (var x = 0; x < width; x++)
                {
                    var stateIndex = (stripeY + 1) * stride + x + 1;
                    var coefficientIndex = stripeY * width + x;
                    var stripeRow = 0;

                    if (rowsInStripe == StripeHeight)
                    {
                        // Run-length mode requires all coefficients of the stripe column to be decoded in this
                        // pass and to have all-zero contexts, evaluated on the union of their states
                        var column =
                            states[stateIndex + 0 * stride] |
                            states[stateIndex + 1 * stride] |
                            states[stateIndex + 2 * stride] |
                            states[stateIndex + 3 * stride];

                        if (column.IsUndecoded && !column.HasSignificantNeighbors)
                        {
                            if (bitDecoder.DecodeBit(JpxMqContext.RunLengthContext) == 0)
                            {
                                // The whole column stays insignificant
                                continue;
                            }

                            // Two UNIFORM bits, MSB first, locate the first significant coefficient. Its
                            // significance bit is implied by the run-length symbol.
                            stripeRow = (
                                bitDecoder.DecodeBit(JpxMqContext.UniformContext) << 1) |
                                bitDecoder.DecodeBit(JpxMqContext.UniformContext);
                            stateIndex += stripeRow * stride;
                            coefficientIndex += stripeRow * width;

                            var signBit = bitDecoder.DecodeSignBit(states[stateIndex].SignContextIndex);
                            MakeSignificant(stateIndex, coefficientIndex, stripeY + stripeRow, signBit);

                            stripeRow++;
                            stateIndex += stride;
                            coefficientIndex += width;
                        }
                    }

                    for (; stripeRow < rowsInStripe; stripeRow++)
                    {
                        ref var coefficientState = ref states[stateIndex];

                        if (coefficientState.IsUndecoded)
                        {
                            if (bitDecoder.DecodeBit(contexts, coefficientState.SignificanceContextIndex) == 1)
                            {
                                var signBit = bitDecoder.DecodeSignBit(coefficientState.SignContextIndex);
                                MakeSignificant(stateIndex, coefficientIndex, stripeY + stripeRow, signBit);
                            }
                        }
                        else
                        {
                            // The visited state only spans one bit-plane, which ends with this pass
                            coefficientState.ClearVisited();
                        }

                        stateIndex += stride;
                        coefficientIndex += width;
                    }
                }
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private void MakeSignificant(int stateIndex, int coefficientIndex, int y, int signBit)
        {
            var states = this.states;
            var stride = this.stateStride;

            states[stateIndex].MarkSignificant();

            // ITU-T T.800 (06/2019) Figure D.2: Update neighbours state
            // This coefficient is the east neighbour of the coefficient west of it, and so on
            states[stateIndex - 1].MarkEastNeighborSignificant(signBit);
            states[stateIndex + 1].MarkWestNeighborSignificant(signBit);

            states[stateIndex + stride - 1].MarkNorthEastNeighborSignificant();
            states[stateIndex + stride + 0].MarkNorthNeighborSignificant(signBit);
            states[stateIndex + stride + 1].MarkNorthWestNeighborSignificant();

            // Section D.7: with vertically causal contexts, significance never propagates to the stripe above
            if (!this.verticallyCausal || y % StripeHeight != 0)
            {
                states[stateIndex - stride - 1].MarkSouthEastNeighborSignificant();
                states[stateIndex - stride + 0].MarkSouthNeighborSignificant(signBit);
                states[stateIndex - stride + 1].MarkSouthWestNeighborSignificant();
            }

            coefficients[coefficientIndex] = signBit == 0 ? significanceValue : -significanceValue;
        }

        private bool DecodeSegmentationSymbol<TBitDecoder>(ref TBitDecoder bitDecoder)
            where TBitDecoder : struct, IBitDecoder
        {
            // See ITU-T T.800 (06/2019) Section D.5
            var symbol = 0;

            for (var i = 0; i < 4; i++)
            {
                symbol = (symbol << 1) | bitDecoder.DecodeBit(JpxMqContext.UniformContext);
            }

            return symbol == 0b1010;
        }

        private void SetPlaneValues(int plane)
        {
            if (plane > MaxPlane)
            {
                // Magnitudes saturate above MaxPlane: coefficients becoming significant get the largest
                // representable plane value, and refinement bits of saturated planes are decoded but no longer
                // move the magnitude
                significanceValue = (useMidpointReconstruction ? 3 : 2) << MaxPlane;
                refinementOneDelta = 0;
                refinementZeroDelta = 0;
            }
            else if (plane < 0)
            {
                // Only reached after the final cleanup pass, when no pass will use the values
                significanceValue = 0;
                refinementOneDelta = 0;
                refinementZeroDelta = 0;
            }
            else if (useMidpointReconstruction)
            {
                // Interval midpoint reconstruction: 1.5 * 2^plane upon significance, then plus or minus
                // 0.5 * 2^plane per refinement bit
                significanceValue = 3 << plane;
                refinementOneDelta = 1 << plane;
                refinementZeroDelta = -(1 << plane);
            }
            else
            {
                // Plain decoded bits (one fraction bit is still reserved in the coefficient)
                significanceValue = 2 << plane;
                refinementOneDelta = 2 << plane;
                refinementZeroDelta = 0;
            }
        }

        private interface IBitDecoder
        {
            // Separate methods to prevent unnecessary table lookups in raw mode
            int DecodeBit(int context);
            int DecodeBit(byte[] contextLut, int lutIndex);

            int DecodeRefinementBit(JpxTier1CoefficientState state);

            int DecodeSignBit(int signContextIndex);
        }

        private struct MqBitDecoder(ArraySegment<byte> data, JpxMqContextEntry[] mqContext)
            : IBitDecoder
        {
            private JpxMqDecoder decoder = new(data);

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public int DecodeBit(int context)
            {
                return decoder.DecodeBit(ref mqContext[context]);
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public int DecodeBit(byte[] contextLut, int lutIndex)
            {
                return DecodeBit(contextLut[lutIndex]);
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public int DecodeRefinementBit(JpxTier1CoefficientState coefficientState)
            {
                var context = JpxMqContext.GetRefinementContext(coefficientState);
                return DecodeBit(context);
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public int DecodeSignBit(int signContextIndex)
            {
                var context = JpxTier1LookupTables.SignContextTable[signContextIndex];

                // ITU-T T.800 (06/2019) Equation D-1
                return decoder.DecodeBit(ref mqContext[context.Label]) ^ context.XorBit;
            }
        }

        private struct RawBitDecoder(ArraySegment<byte> data) : IBitDecoder
        {
            private JpxRawBitReader reader = new(data);

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public int DecodeBit(int context)
            {
                return reader.ReadBit();
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public int DecodeBit(byte[] contextLut, int lutIndex)
            {
                return reader.ReadBit();
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public int DecodeRefinementBit(JpxTier1CoefficientState state)
            {
                return reader.ReadBit();
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public int DecodeSignBit(int signContextIndex)
            {
                // ITU-T T.800 (06/2019) Equation D-2: raw sign bits are stored directly
                return reader.ReadBit();
            }
        }
    }
}

#pragma warning restore IDE0003 // Remove 'this.' qualification
