// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Tests.Images.Jpx
{
    /// <summary>
    /// The complete codestream of the decoding example in ITU-T T.800 (06/2019) Section J.10, which shows the decoding
    /// of a 100 byte codestream with intermediate steps: a 1x9 image with one 8-bit unsigned component in a single
    /// tile, one decomposition level, one layer, LRCP progression, 64x64 code-blocks, and the 5-3 reversible filter
    /// without quantization.
    /// </summary>
    internal static class JpxAnnexJ10
    {
        private static readonly byte[] codestream =
        {
            // SOC (Section J.10.1)
            0xFF, 0x4F,

            // SIZ, Lsiz = 41: no profile restrictions, 1x9 image at offset (0, 0), 1x9 tiles at offset (0, 0),
            // one 8-bit unsigned component without sub-sampling
            0xFF, 0x51, 0x00, 0x29,
            0x00, 0x00,             // Rsiz
            0x00, 0x00, 0x00, 0x01, // Xsiz
            0x00, 0x00, 0x00, 0x09, // Ysiz
            0x00, 0x00, 0x00, 0x00, // XOsiz
            0x00, 0x00, 0x00, 0x00, // YOsiz
            0x00, 0x00, 0x00, 0x01, // XTsiz
            0x00, 0x00, 0x00, 0x09, // YTsiz
            0x00, 0x00, 0x00, 0x00, // XTOsiz
            0x00, 0x00, 0x00, 0x00, // YTOsiz
            0x00, 0x01,             // Csiz
            0x07, 0x01, 0x01,       // Ssiz, XRsiz, YRsiz

            // QCD, Lqcd = 7: no quantization, 2 guard bits, exponents {8, 9, 9, 10}
            0xFF, 0x5C, 0x00, 0x07,
            0x40,                   // Sqcd
            0x40, 0x48, 0x48, 0x50, // SPqcd

            // COD, Lcod = 12: LRCP progression, 1 layer, no multiple component transformation, 1 decomposition
            // level, 64x64 code-blocks, default code-block style, 5-3 reversible filter, no precincts
            0xFF, 0x52, 0x00, 0x0C,
            0x00,                   // Scod
            0x00, 0x00, 0x01, 0x00, // SGcod
            0x01, 0x04, 0x04, 0x00, // SPcod
            0x01,

            // SOT, Lsot = 10 (Section J.10.2): tile 0, Psot = 30, tile-part 0 of 1
            0xFF, 0x90, 0x00, 0x0A,
            0x00, 0x00,             // Isot
            0x00, 0x00, 0x00, 0x1E, // Psot
            0x00,                   // TPsot
            0x01,                   // TNsot

            // SOD
            0xFF, 0x93,

            // First packet: 3 header bytes (Table J.20) and 6 body bytes (Table J.22) holding the 1x5 LL band
            // code-block
            0xC7, 0xD4, 0x0C,
            0x01, 0x8F, 0x0D, 0xC8, 0x75, 0x5D,

            // Second packet: 4 header bytes (Table J.21) and 3 body bytes (Table J.23) holding the 1x4 1LH band
            // code-block
            0xC0, 0x7C, 0x21, 0x80,
            0x0F, 0xB1, 0x76,

            // EOC
            0xFF, 0xD9,
        };

        /// <summary>The codestream bytes. A new array is returned on each call.</summary>
        public static byte[] Codestream => (byte[])codestream.Clone();

        /// <summary>The 16 bytes of packet data between the SOD and EOC markers.</summary>
        public static byte[] TilePartData => new byte[]
        {
            0xC7, 0xD4, 0x0C, 0x01, 0x8F, 0x0D, 0xC8, 0x75, 0x5D, 0xC0, 0x7C, 0x21, 0x80, 0x0F, 0xB1, 0x76,
        };

        /// <summary>The decoded component samples given in Section J.10.5.</summary>
        public static byte[] ExpectedSamples => new byte[] { 101, 103, 104, 105, 96, 97, 96, 102, 109 };
    }
}
