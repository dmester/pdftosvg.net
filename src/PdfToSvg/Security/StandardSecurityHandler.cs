// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PdfToSvg.Security
{
    internal class StandardSecurityHandler : SecurityHandler
    {
        private readonly PdfString ownerPassword;
        private readonly PdfString userPassword;

        private readonly PdfString ownerEncryption;
        private readonly PdfString userEncryption;

        private readonly PdfString perms;

        private readonly PdfString id;

        private readonly bool encryptMetadata;
        private readonly int keyLength;
        private readonly int revision;
        private readonly int permissions;
        private readonly int algorithm;

        private readonly PdfDictionary cryptFilters;
        private readonly PdfName defaultCryptFilterName;
        private readonly PdfName defaultStringCryptFilterName;

        private byte[] encryptionKey;
        private bool ownerAuthenticated;

        public StandardSecurityHandler(PdfDictionary trailerDict, PdfDictionary encryptDict, OpenOptions options)
        {
            if (!trailerDict.TryGetArray<PdfString>(Names.ID, out var id) || id.Length < 1)
            {
                throw SecurityExceptions.MissingFileID();
            }

            this.id = id[0];

            keyLength = encryptDict.GetValueOrDefault(Names.Length, 40);
            revision = encryptDict.GetValueOrDefault(Names.R, 2);
            algorithm = encryptDict.GetValueOrDefault(Names.V, 0);

            ownerPassword = encryptDict.GetValueOrDefault(Names.O, PdfString.Empty);
            userPassword = encryptDict.GetValueOrDefault(Names.U, PdfString.Empty);

            ownerEncryption = encryptDict.GetValueOrDefault(Names.OE, PdfString.Empty);
            userEncryption = encryptDict.GetValueOrDefault(Names.UE, PdfString.Empty);
            perms = encryptDict.GetValueOrDefault(Names.Perms, PdfString.Empty);

            permissions = encryptDict.GetValueOrDefault(Names.P, 0);
            encryptMetadata = encryptDict.GetValueOrDefault(Names.EncryptMetadata, true);

            cryptFilters = encryptDict.GetDictionaryOrEmpty(Names.CF);

            if (algorithm < 4)
            {
                defaultCryptFilterName = Names.V2;
                defaultStringCryptFilterName = Names.V2;
            }
            else
            {
                defaultCryptFilterName = encryptDict.GetValueOrDefault(Names.StmF, Names.Identity);
                defaultStringCryptFilterName = encryptDict.GetValueOrDefault(Names.StrF, Names.Identity);
            }

            if (algorithm == 1)
            {
                keyLength = 40;
            }
            else if (algorithm == 2 || algorithm == 3)
            {
                if (keyLength < 40 || keyLength > 128)
                {
                    throw SecurityExceptions.InvalidKeyLength(keyLength);
                }

                if ((keyLength % 8) != 0)
                {
                    throw SecurityExceptions.KeyLengthNotMultipleOf8(keyLength);
                }
            }

            Authenticate(options, out encryptionKey, out ownerAuthenticated);
        }

        public override bool IsOwnerAuthenticated => ownerAuthenticated;

        private void Authenticate(OpenOptions options, out byte[] encryptionKey, out bool ownerAuthenticated)
        {
            var password = options.Password ?? "";

            byte[]? potentialEncryptionKey;

            ownerAuthenticated = false;

            if (revision <= 4)
            {
                // Check owner password
                var userPasswordFromOwnerPassword = KeyAlgorithms.GetUserPasswordFromOwnerPasswordLegacy(password, ownerPassword, keyLength, revision);
                potentialEncryptionKey = GetEncryptionKeyLegacy(userPasswordFromOwnerPassword);

                // Check user password
                if (potentialEncryptionKey != null)
                {
                    ownerAuthenticated = true;
                }
                else
                {
                    potentialEncryptionKey = GetEncryptionKeyLegacy(KeyAlgorithms.PadPassword(password));
                }
            }
            else if (revision == 5 || revision == 6)
            {
                Func<string, PdfString, PdfString, PdfString?, byte[]?> getEncryptionKey =
                    revision == 5
                    ? KeyAlgorithms.GetEncryptionKeyRevision5
                    : KeyAlgorithms.GetEncryptionKeyRevision6;

                // Check owner password
                potentialEncryptionKey = getEncryptionKey(password, ownerPassword, ownerEncryption, userPassword);

                // Check user password
                if (potentialEncryptionKey != null)
                {
                    ownerAuthenticated = true;
                }
                else
                {
                    potentialEncryptionKey = getEncryptionKey(password, userPassword, userEncryption, null);
                }

                // Verify encrypted permissions
                if (potentialEncryptionKey != null)
                {
                    var expectedPerms = KeyAlgorithms.DecryptPermsRevision5Or6(perms, potentialEncryptionKey);
                    if (expectedPerms != permissions)
                    {
                        potentialEncryptionKey = null;
                    }
                }
            }
            else
            {
                throw SecurityExceptions.UnsupportedRevision(revision);
            }

            if (potentialEncryptionKey == null)
            {
                throw string.IsNullOrEmpty(password)
                    ? SecurityExceptions.PasswordRequired()
                    : SecurityExceptions.WrongPassword();
            }

            encryptionKey = potentialEncryptionKey;
        }

        private byte[]? GetEncryptionKeyLegacy(byte[] paddedPassword)
        {
            var potentialEncryptionKey = KeyAlgorithms.GetEncryptionKeyLegacy(paddedPassword, ownerPassword, permissions, id, encryptMetadata, revision, keyLength);

            var userPasswordString = revision < 3
                ? KeyAlgorithms.GetUserPasswordRevision2(potentialEncryptionKey)
                : KeyAlgorithms.GetUserPasswordRevision3(potentialEncryptionKey, id);

            var checkBytes = revision < 3 ? userPasswordString.Length : 16;

            if (userPassword.Length < checkBytes || !userPassword.StartsWith(userPasswordString, 0, checkBytes))
            {
                // Wrong password
                potentialEncryptionKey = null;
            }

            return potentialEncryptionKey;
        }

        private byte[] CreateObjectKey(PdfObjectId id, bool aes)
        {
            // PDF spec 1.7, Algorithm 1, page 66

            var hashInput = ArrayUtils.Concat(
                encryptionKey,

                new byte[]
                {
                    unchecked((byte)(id.ObjectNumber)),
                    unchecked((byte)(id.ObjectNumber >> 8)),
                    unchecked((byte)(id.ObjectNumber >> 16)),

                    unchecked((byte)(id.Generation)),
                    unchecked((byte)(id.Generation >> 8)),
                },

                aes ? new byte[] { 0x73, 0x41, 0x6c, 0x54 } : null
                );

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(hashInput);

            var objectKey = hash.Slice(0, Math.Min(encryptionKey.Length + 5, 16));
            return objectKey;
        }

        public override PdfDictionary? CreateImplicitCryptDecodeParms()
        {
            return new PdfDictionary
            {
                { Names.Name, InternalNames.Implicit },
            };
        }

        /// <summary>
        /// Creates a decrypted representation of a specified encrypted stream.
        /// The method takes ownership of the <see cref="SymmetricAlgorithm"/> instance.
        /// </summary>
        private static Stream DecryptWithOwnership(Stream stream, SymmetricAlgorithm algorithm)
        {
            var success = false;
            ICryptoTransform? transform = null;

            Action disposer = () =>
            {
                if (transform != null)
                {
                    transform.Dispose();
                    transform = null;
                }

                if (algorithm != null)
                {
                    algorithm.Dispose();
                    algorithm = null!;
                }
            };

            try
            {
                transform = algorithm.CreateDecryptor();

                var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read);
                var disposeStream = new DisposeStream(cryptoStream, disposer);

                success = true;
                return disposeStream;
            }
            finally
            {
                if (!success)
                {
                    disposer();
                }
            }
        }

        public override Stream Decrypt(PdfObjectId id, PdfName filterName, Stream encryptedStream)
        {
            if (filterName == InternalNames.Implicit)
            {
                filterName = defaultCryptFilterName;
            }

            if (filterName == Names.Identity)
            {
                return encryptedStream;
            }

            if (algorithm == 1 || algorithm == 2)
            {
                var key = CreateObjectKey(id, false);
                return DecryptWithOwnership(encryptedStream, new ArcFour(key));
            }

            if (algorithm == 4 || algorithm == 5)
            {
                // PDF spec 1.7, Table 25
                if (!cryptFilters.TryGetDictionary(filterName, out var filter))
                {
                    throw SecurityExceptions.FilterNotFound(filterName);
                }

                var cfm = filter.GetValueOrDefault(Names.CFM, Names.None);

                if (cfm == Names.None)
                {
                    // The behavior of CFM None seems undefined when using the standard security handler.
                    // Let's return the encrypted data and hope for the best.
                    return encryptedStream;
                }

                if (cfm == Names.V2)
                {
                    if (cryptFilters.TryGetInteger(Names.Length, out var localKeyLength) && localKeyLength * 8 != keyLength)
                    {
                        throw SecurityExceptions.CryptFilterKeyLengthNotSupported();
                    }

                    var key = CreateObjectKey(id, false);
                    return DecryptWithOwnership(encryptedStream, new ArcFour(key));
                }

                if (cfm == Names.AESV2 || cfm == Names.AESV3)
                {
                    var objectKey = encryptionKey;

                    if (cfm == Names.AESV2)
                    {
                        objectKey = CreateObjectKey(id, true);
                    }

                    var iv = new byte[16];
                    encryptedStream.ReadAll(iv, 0, iv.Length);

                    var aes = Aes.Create();
                    aes.Key = objectKey;
                    aes.IV = iv;

                    return DecryptWithOwnership(encryptedStream, aes);
                }

                throw SecurityExceptions.UnsupportedCfm(cfm);
            }

            throw SecurityExceptions.UnsupportedAlgorithm(algorithm);
        }

        public override PdfString Decrypt(PdfObjectId id, PdfString s)
        {
            using var encryptedStream = new MemoryStream(s.ToByteArray(), false);
            using var decryptedStream = Decrypt(id, defaultStringCryptFilterName, encryptedStream);
            using var decryptedMemoryStream = new MemoryStream(s.Length);

            decryptedStream.CopyTo(decryptedMemoryStream);

            return new PdfString(decryptedMemoryStream);
        }
    }
}
