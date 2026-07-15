# JPEG 2000 codec

## Decoding pipeline

This is what a decoding pipeline for JPEG 2000 looks like. 
Data flows top to bottom. Each stage is owned by a single namespace.

```
           Compressed JPEG 2000 data       Namespace         JpxDecoder phase
                      ↓
     ┌────────────────────────────────┐
     │     JP2 container parsing      │    Container       ┐
     └────────────────────────────────┘                    │
                      ↓                                    │ ReadMetadata
     ┌────────────────────────────────┐                    │
     │        Parse codestream        │    Codestream      ┘
     └────────────────────────────────┘
                      ↓ 
     ┌────────────────────────────────┐
     │    Canvas geometry / layout    │    ImageModel      ┐
     └────────────────────────────────┘                    │
                      ↓                                    │
     ┌────────────────────────────────┐                    │
     │     Tier-2 packet decoding     │    Packets         │
     └────────────────────────────────┘                    │
                      ↓                                    │
     ┌────────────────────────────────┐                    │
     │  Tier-1 entropy decoding (MQ)  │    Coding          │
     └────────────────────────────────┘                    │
                      ↓                                    │
     ┌────────────────────────────────┐                    │
     │        Dequantization          │    Transforms      │ ReadImageData
     └────────────────────────────────┘                    │
                      ↓                                    │
     ┌────────────────────────────────┐                    │
     │   Inverse wavelet transform    │    Transforms      │
     └────────────────────────────────┘                    │
                      ↓                                    │
     ┌────────────────────────────────┐                    │
     │ Inverse component transform    │    Transforms      │
     └────────────────────────────────┘                    │
                      ↓                                    │
     ┌────────────────────────────────┐                    │
     │          Tile assembly         │    Decoding        ┘
     └────────────────────────────────┘
                      ↓
           Uncompressed image data
```

## Structure

The JPEG 2000 format has a very complex structure, making it hard to understand. This is a short summary of the structure.

File structure from top to bottom:

* JP2 file
* Boxes
* Codestream
* Tile part
* Packet - All code blocks for a specific precinct and layer.

PDF files can contain either JP2 files or raw codestreams.

Logical structure from top to bottom:

* Image
* Tile - For segmented image encoding
* Component - One for each color channel
* Resolution level - Each level increases the resolution of the decoded image. Only the difference between each resolution level is encoded.
* Sub-band - Wavelet transform
* Precinct - Group of code blocks
* Code block - Smallest indepedently entropy coded unit
* Coding passes - Blocks are coded into a sequence of coding passes, that can be segregated into multiple *quality layers* that can be decoded serially for a progressively decoded image

## Compliance

ITU-T T.803 defines compliance classes (0, 1 and 2) that describe the minimum guarantees of a decoder. This decoder targets **Cclass 1**. 
Here are the parameters for compliance class 1 and 2, and what this decoder supports.

| Parameter (T.803 Annex A)   | Cclass 1              | Cclass 2                | This decoder |
|-----------------------------|-----------------------|-------------------------|--------------|
| *W* × *H* (image size)      | ✅ 2048 × 2048           | ✅ 16384 × 16384           | 16384 × 16384 |
| *C* (decoded components)    | ✅ 4                     | ❌ 256                     | 5 output planes (for CMYK + alpha) |
| *N*comp (component parsing) | ✅ 256                   | ❌ 16 384                  | 384 |
| *N*cb, *L*body (parser and coded data bounds) | ✅ 66 048 / 2^23 bytes | ✅ ~2.7 · 10^8 / 2^30 bytes | Unlimited within the decode memory budgets |
| *M* (decoded bit-planes)    | ✅ 15                    | ❌ 30                      | 29 decoded exactly |
| *P* (9-7I precision)        | ✅ 16-bit fixed point    | ✅ 32-bit floats           | 32-bit floats |
| *B* (5-3R exact bit-depth)  | ✅ 12                    | ✅ 16                      | ~20 (note 2) |
| *TL* (transform levels)     | ✅ 7                     | ✅ 12                      | 32 |
| *L* (layers)                | ✅ 255                   | ✅ 65 535                  | 65 535 |
| Progressions                | ✅ All                   | ✅ All                     | All |
| Tile-parts                  | ✅ All up to *N*cb/*L*body | ✅ All up to *N*cb/*L*body | All |
| Precincts                   | ✅ All up to *N*cb/*L*body | ✅ All up to *N*cb/*L*body | All |

`JpxConformanceTests` implements tests of the T.803 test suite codestreams (`p0_01`–`p0_16` and `p1_01`–`p1_07`) and compares each component against the
suite's PGX reference images within the Class 1 error tolerances of Tables C.6 and C.7.

Note that the T.803 test files are not part of the repository for legal reasons, but you can download them free of cost from ITU:

https://www.itu.int/rec/T-REC-T.803/en

Unzip the folder in `tests\TestFiles\T.803-JPEG2000-conformance`. The target folder structure should look like this:

```
TestFiles
└── T.803-JPEG2000-conformance
    ├── codestreams_profile0
    ├── codestreams_profile1
    ├── reference_class1_profile0
    └── reference_class1_profile1
```

## Limitations

* Decoding only
* Part 1 (T.800) only
* HTJ2K (Part 15, T.814) codestreams are rejected
* ICC colorspaces not supported
