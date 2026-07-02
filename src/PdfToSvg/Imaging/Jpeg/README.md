# JPEG codec

## JPEG pipeline

This is what a normal JPEG decoding and encoding pipeline looks like:

```
                ENCODER                          DECODER

      Uncompressed RGB image data      Uncompressed RGB image data
                   ↓                                ↑
      ┌─────────────────────────┐      ┌─────────────────────────┐
      │  Color space conversion │      │  Color space conversion │
      └─────────────────────────┘      └─────────────────────────┘
                   ↓                                ↑
      ┌─────────────────────────┐      ┌─────────────────────────┐
      │       Subsampling       │      │   Inverse subsampling   │
      └─────────────────────────┘      └─────────────────────────┘
                   ↓                                ↑
      ┌─────────────────────────┐      ┌─────────────────────────┐
      │     Block splitting     │      │   Block reconstruction  │
      └─────────────────────────┘      └─────────────────────────┘
                   ↓                                ↑
      ┌─────────────────────────┐      ┌─────────────────────────┐
      │           DCT           │      │       Inverse DCT       │
      └─────────────────────────┘      └─────────────────────────┘
                   ↓                                ↑
      ┌─────────────────────────┐      ┌─────────────────────────┐
      │     Zig zag ordering    │      │     Zig zag ordering    │
      └─────────────────────────┘      └─────────────────────────┘
                   ↓                                ↑
      ┌─────────────────────────┐      ┌─────────────────────────┐
      │       Quantization      │      │      Dequantization     │
      └─────────────────────────┘      └─────────────────────────┘
                   ↓                                ↑
      ┌─────────────────────────┐      ┌─────────────────────────┐
      │     Entropy encoding    │      │     Entropy decoding    │
      └─────────────────────────┘      └─────────────────────────┘
                   ↓                                ↑
          Compressed JPEG data             Compressed JPEG data
```

## Optimizations

### DCT lifting scheme

The standard DCT contains many cosine and sqrt operations, making the process CPU intensive. There are several optimization
schemes using simple additions and multiplications to approximate the algorithm. This library use the
[fast DCT algorithm](https://www.semanticscholar.org/paper/Practical-fast-1-D-DCT-algorithms-with-11-Loeffler-Ligtenberg/6134d65dc1d01db1e3c4be6f675763a469a973f6)
described by C. Loeffler, A. Ligtenberg and G. Moschytz.

### SIMD vectorization

The pipelines of the encoder and decoder are entirely vectorized using SSE2 or AVX2 SIMD operations.

SIMD operations can execute operations on several values simultaneously using a single CPU instruction.
For example, in non-vectorized code, the compiler will use an instruction to add two 16 bit numbers into a sum.
In vectorized code, an AVX2 instruction can simultaneously execute the addition operator on 16 numbers with 16 bit precision.

### Transpose and zig zag ordering

This is the textbook approach of 2D DCT:

```
   1D DCT on rows  →  Transpose   →  1D DCT on columns  →  Transpose 
```

The DCT is followed by zig zag ordering the samples before handing them over to the quantizer.

This library eliminates the last transpose and zig zag ordering:

* The quantizer is built to operate on zig-zagged and transposed data
* The zig zag ordering and last transpose are baked into the final entropy coding step (see `JpegDataUnit`)

### Solid colored blocks

If a block contains a single color, the block does not need to go through DCT. Instead, it can be encoded directly.

### Short circuiting non-subsampled JPEGs

If the input JPEG is not subsampled, and the PDF does not specify any special colorspace or decode array, the following steps will be skipped:

* Block splitting
* Subsampling

Instead, raw blocks are taken directly from IDCT to the YCbCr color converter, and fed directly back to DCT of the encoder.

## Limitations

This is a baseline JPEG codec. The following features are not supported:

* Progressive JPEG
* Non-interleaved components
* 12 bit samples
* Arithmetic coding
