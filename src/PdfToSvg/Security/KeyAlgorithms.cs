// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.IO;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PdfToSvg.Security
{
    internal static class KeyAlgorithms
    {
        private const int PaddedPasswordLength = 32;

        private static readonly byte[] padding = new byte[]
        {
            0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
            0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A
        };

        public static byte[] PadPassword(string password)
        {
            var paddedPassword = new byte[PaddedPasswordLength];

            PadPassword(paddedPassword, password);

            return paddedPassword;
        }

        private static void PadPassword(byte[] output, string password)
        {
            // ISO 32000-2, Algorithm 2, step a)

            // Convert Unicode to codepage encoding
#if !NETSTANDARD1_6
            var codepageBytes = Encoding.Default.GetBytes(password);
            password = Encoding.Default.GetString(codepageBytes);
#endif

            // Convert codepage encoding to PDFDocEncoding
            var encoding = SingleByteEncoding.PdfDoc;
            var decodeCharCount = Math.Min(PaddedPasswordLength, password.Length);
            var passwordLength = encoding.GetBytes(password, 0, decodeCharCount, output, 0);

            // Add padding
            var padIndex = 0;

            while (passwordLength < PaddedPasswordLength)
            {
                output[passwordLength] = padding[padIndex];

                passwordLength++;
                padIndex++;
            }
        }

        public static byte[] GetUserPasswordFromOwnerPasswordLegacy(string password, PdfString ownerPassword, int keyLength, int revision)
        {
            // PDF spec 1.7, Algorithm 7
            // Step a)
            var fileEncryptionKey = new byte[keyLength / 8];

            {
                // PDF spec 1.7, Algorithm 3
                // Step a)
                using var md5 = MD5.Create();
                var hashInput = PadPassword(password);

                // Step b)
                var hash = md5.ComputeHash(hashInput);

                // Step c)
                if (revision >= 3)
                {
                    for (var i = 0; i < 50; i++)
                    {
                        hash = md5.ComputeHash(hash);
                    }
                }

                // Step d)
                Buffer.BlockCopy(hash, 0, fileEncryptionKey, 0, fileEncryptionKey.Length);
            }

            // Step b)
            var userPassword = ownerPassword.ToByteArray();

            for (var iteration = revision < 3 ? 0 : 19; iteration >= 0; iteration--)
            {
                var iterationKey = new byte[fileEncryptionKey.Length];

                for (var i = 0; i < iterationKey.Length; i++)
                {
                    iterationKey[i] = unchecked((byte)(fileEncryptionKey[i] ^ iteration));
                }

                userPassword = ArcFour.Transform(iterationKey, userPassword);
            }

            return userPassword;
        }

        public static byte[] GetUserPasswordRevision2(byte[] encryptionKey)
        {
            return ArcFour.Transform(encryptionKey, padding);
        }

        public static byte[] GetUserPasswordRevision3(byte[] encryptionKey, PdfString id)
        {
            // PDF spec 1.7, page 71, section 7.6.3.4, Algorithm 5
            using var md5 = MD5.Create();

            // Step b)
            var hashInput = new byte[padding.Length + id.Length];
            Buffer.BlockCopy(padding, 0, hashInput, 0, padding.Length);

            // Step c)
            for (var i = 0; i < id.Length; i++)
            {
                hashInput[padding.Length + i] = id[i];
            }

            var hash = md5.ComputeHash(hashInput);

            // Step d) and e)
            for (var iteration = 0; iteration < 20; iteration++)
            {
                var iterationKey = new byte[encryptionKey.Length];
                for (var i = 0; i < encryptionKey.Length; i++)
                {
                    iterationKey[i] = unchecked((byte)(encryptionKey[i] ^ iteration));
                }

                hash = ArcFour.Transform(iterationKey, hash);
            }

            var result = new byte[32];
            Buffer.BlockCopy(hash, 0, result, 0, 16);
            return result;
        }

        public static byte[] GetEncryptionKeyLegacy(byte[] paddedPassword,
            PdfString ownerPasswordString, int permittedAccess,
            PdfString id, bool encryptedMetadata, int revision,
            int keyLength)
        {
            // PDF spec 1.7, page 69, section 7.6.3.3

            using var md5 = MD5.Create();

            int hashInputCursor = PaddedPasswordLength;

            var hashInput = new byte[
                PaddedPasswordLength +
                ownerPasswordString.Length +
                4 +
                id.Length +
                (!encryptedMetadata && revision >= 4 ? 4 : 0)];

            // Step a) and b)
            Buffer.BlockCopy(paddedPassword, 0, hashInput, 0, PaddedPasswordLength);

            // Step c)
            for (var i = 0; i < ownerPasswordString.Length; i++)
            {
                hashInput[hashInputCursor++] = ownerPasswordString[i];
            }

            // Step d)
            hashInput[hashInputCursor++] = unchecked((byte)permittedAccess);
            hashInput[hashInputCursor++] = unchecked((byte)(permittedAccess >> 8));
            hashInput[hashInputCursor++] = unchecked((byte)(permittedAccess >> 16));
            hashInput[hashInputCursor++] = unchecked((byte)(permittedAccess >> 24));

            // Step e)
            for (var i = 0; i < id.Length; i++)
            {
                hashInput[hashInputCursor++] = id[i];
            }

            // Step f)
            if (!encryptedMetadata && revision >= 4)
            {
                hashInput[hashInputCursor++] = 0xff;
                hashInput[hashInputCursor++] = 0xff;
                hashInput[hashInputCursor++] = 0xff;
                hashInput[hashInputCursor++] = 0xff;
            }

            // Step g)
            var hash = md5.ComputeHash(hashInput);

            // Step h)
            if (revision >= 3)
            {
                hashInput = new byte[keyLength / 8];

                for (var i = 0; i < 50; i++)
                {
                    Buffer.BlockCopy(hash, 0, hashInput, 0, hashInput.Length);
                    hash = md5.ComputeHash(hashInput);
                }
            }

            // Step i)
            var result = new byte[keyLength / 8];
            Buffer.BlockCopy(hash, 0, result, 0, result.Length);
            return result;
        }

        public static byte[]? GetEncryptionKeyRevision5(string password, PdfString userPasswordString, PdfString encryptedKey, PdfString? extraSalt)
        {
            // Adobe Supplement to the ISO 32000, Algorithm 3.2a

            using var sha256 = SHA256.Create();

            // Step 1.
            // Only the normalization part from the SASLprep preparation is performed.
#if NETSTANDARD1_6
            var normalizedPassword = password;
#else
            var normalizedPassword = password.Normalize(NormalizationForm.FormKC);
#endif
            var passwordBytes = Encoding.UTF8.GetBytes(normalizedPassword);

            // Step 2.
            if (passwordBytes.Length > 127)
            {
                passwordBytes = passwordBytes.Slice(0, 127);
            }

            // Step 3.
            var extraSaltBytes = extraSalt?.ToByteArray() ?? ArrayUtils.Empty<byte>();

            // Verify password
            var validationSalt = userPasswordString.ToByteArray(32, 8);
            var validationHashInput = ArrayUtils.Concat(passwordBytes, validationSalt, extraSaltBytes);
            var validationHash = sha256.ComputeHash(validationHashInput);

            if (!userPasswordString.StartsWith(validationHash, 0, 32))
            {
                return null;
            }

            // Create intermediate key
            var keySalt = userPasswordString.ToByteArray(40, 8);
            var intermediateKeyInput = ArrayUtils.Concat(passwordBytes, keySalt, extraSaltBytes);
            var intermediateKey = sha256.ComputeHash(intermediateKeyInput);

            // Decrypt file encryption key
            using var encryptionKeyStream = new MemoryStream();

            {
                using var aes = Aes.Create();

                aes.Key = intermediateKey;
                aes.IV = new byte[16]; // Zero IV
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                using var decryptor = aes.CreateDecryptor();
                using var cryptoStream = new CryptoStream(encryptionKeyStream, decryptor, CryptoStreamMode.Write);

                var encryptedKeyBytes = encryptedKey.ToByteArray();
                cryptoStream.Write(encryptedKeyBytes);
            }

            return encryptionKeyStream.ToArray();
        }

        public static int DecryptPermsRevision5Or6(PdfString encryptedPerms, byte[] encryptionKey)
        {
            // Revision 5:
            // Adobe Supplement to the ISO 32000, Algorithm 3.2a, step 5.
            //
            // Revision 6:
            // ISO 32000-2, Algorithm 2.A, step f)

            using var decryptedPermsStream = new MemoryStream();

            {
                using var aes = Aes.Create();

                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.Key = encryptionKey;
                aes.IV = new byte[16]; // Zero IV

                using var decryptor = aes.CreateDecryptor();
                using var cryptoStream = new CryptoStream(decryptedPermsStream, decryptor, CryptoStreamMode.Write);

                cryptoStream.Write(encryptedPerms.ToByteArray());
            }

            var decryptedPerms = decryptedPermsStream.ToArray();

            if (decryptedPerms.Length < 12 ||
                decryptedPerms[9] != 'a' ||
                decryptedPerms[10] != 'd' ||
                decryptedPerms[11] != 'b')
            {
                // Invalid ecrypted permissions
                return 0;
            }

            return
                (decryptedPerms[3] << 24) |
                (decryptedPerms[2] << 16) |
                (decryptedPerms[1] << 8) |
                (decryptedPerms[0]);
        }


        public static byte[]? GetEncryptionKeyRevision6(string password, PdfString passwordString, PdfString encryptedKey, PdfString? userKey)
        {
            // ISO 32000-2, Algorithm 2.A

            // Only the normalization part from the SASLprep preparation is performed.
#if NETSTANDARD1_6
            var normalizedPassword = password;
#else
            var normalizedPassword = password.Normalize(NormalizationForm.FormKC);
#endif

            var passwordBytes = Encoding.UTF8.GetBytes(normalizedPassword);
            if (passwordBytes.Length > 127)
            {
                passwordBytes = passwordBytes.Slice(0, 127);
            }

            // Verify password
            var validationSalt = passwordString.ToByteArray(32, 8);
            var validationHash = HashRevision6(passwordBytes, validationSalt, userKey);

            if (!passwordString.StartsWith(validationHash, 0, 32))
            {
                return null;
            }

            // Create intermediate key
            var keySalt = passwordString.ToByteArray(40, 8);
            var keyHash = HashRevision6(passwordBytes, keySalt, userKey);

            // Decrypt file encryption key
            using var encryptionKeyStream = new MemoryStream();

            {
                using var aes = Aes.Create();

                aes.Key = keyHash;
                aes.IV = new byte[16]; // Zero IV
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                using var decryptor = aes.CreateDecryptor();
                using var cryptoStream = new CryptoStream(encryptionKeyStream, decryptor, CryptoStreamMode.Write);

                var encryptedKeyBytes = encryptedKey.ToByteArray();
                cryptoStream.Write(encryptedKeyBytes);
            }

            return encryptionKeyStream.ToArray();
        }

        public static byte[] HashRevision6(byte[] passwordBytes, byte[] salt, PdfString? userKey)
        {
            // ISO 32000-2, Algorithm 2.B

            using var sha256 = SHA256.Create();
            using var sha384 = SHA384.Create();
            using var sha512 = SHA512.Create();

            var hashAlgorithms = new HashAlgorithm[] { sha256, sha384, sha512 };

            var userKeyBytes = userKey?.ToByteArray() ?? ArrayUtils.Empty<byte>();

            var kInput = ArrayUtils.Concat(passwordBytes, salt, userKeyBytes);
            var K = sha256.ComputeHash(kInput);

            var E = ArrayUtils.Empty<byte>();

            for (var round = 0; round < 64 || E[E.Length - 1] > round - 32; round++)
            {
                var dataToEncrypt = ArrayUtils.Concat(passwordBytes, K, userKeyBytes);
                using var encryptedStream = new MemoryStream(dataToEncrypt.Length * 64);

                {
                    using var aes = Aes.Create();

                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.None;
                    aes.Key = K.Slice(0, 16);
                    aes.IV = K.Slice(16, 16);

                    using var encryptor = aes.CreateEncryptor();
                    using var cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write);

                    for (var j = 0; j < 64; j++)
                    {
                        cryptoStream.Write(dataToEncrypt);
                    }
                }

                E = encryptedStream.ToArray();

                var remainderModulo3 = MathUtils.ModBE(E.Slice(0, 16), 3);
                var hashAlgorithm = hashAlgorithms[remainderModulo3];

                K = hashAlgorithm.ComputeHash(E);
            }

            return K.Slice(0, 32);
        }
    }
}
