// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PdfToSvg.Security
{
    internal class ArcFour : SymmetricAlgorithm
    {
        public ArcFour()
        {
        }

        public ArcFour(byte[] key)
        {
            Key = key;
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return new ArcFourTransform(rgbKey);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return new ArcFourTransform(rgbKey);
        }

        public override KeySizes[] LegalKeySizes => new[] { new KeySizes(1 * 8, 256 * 8, 8) };

        public override KeySizes[] LegalBlockSizes => new[] { new KeySizes(8, 8, 1) };

        public override void GenerateIV()
        {
            IVValue = new byte[0];
        }

        public override void GenerateKey()
        {
            KeyValue = new byte[16];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(KeyValue);
            }
        }

        public static byte[] Transform(byte[] key, byte[] data)
        {
            using var arcFour = new ArcFourTransform(key);
            return arcFour.TransformFinalBlock(data, 0, data.Length);
        }
    }
}
