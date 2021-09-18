// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Security
{
    internal static class SecurityExceptions
    {
        private const string BaseMessage = "Cannot decrypt this PDF document. ";

        public static Exception UnsupportedSecurityHandler(PdfName securityHandler)
        {
            return new EncryptionException(BaseMessage + "Unsupported security handler " + securityHandler + ".");
        }

        public static Exception UnsupportedRevision(int revision)
        {
            return new EncryptionException(BaseMessage + "Unsupported encryption revision " + revision + ".");
        }

        public static Exception UnsupportedAlgorithm(int algorithm)
        {
            return new EncryptionException(BaseMessage + "Unsupported encryption algorithm " + algorithm + ".");
        }

        public static Exception UnsupportedCfm(PdfName cfm)
        {
            return new EncryptionException(BaseMessage + "CFM value " + cfm + " not supported.");
        }

        public static Exception FilterNotFound(PdfName filterName)
        {
            return new EncryptionException(BaseMessage + "Decrypt filter " + filterName + " not found.");
        }

        public static Exception MissingFileID()
        {
            return new EncryptionException(BaseMessage + "ID is missing in the trailer dictionary.");
        }

        public static Exception InvalidKeyLength(int keyLength)
        {
            return new EncryptionException(BaseMessage + "Invalid key length " + keyLength + ". The length must be in the range 40 to 128.");
        }

        public static Exception CryptFilterKeyLengthNotSupported()
        {
            return new EncryptionException(BaseMessage + "PdfToSvg.NET does not support specific key lengths in /Crypt filter dictionaries.");
        }

        public static Exception KeyLengthNotMultipleOf8(int keyLength)
        {
            return new EncryptionException(BaseMessage + "Invalid key length " + keyLength + ". The length must be a multiple of 8.");
        }

        public static Exception WrongPassword()
        {
            return new InvalidCredentialException("Wrong password for opening this PDF document was specified.");
        }

        public static Exception PasswordRequired()
        {
            return new InvalidCredentialException("A password is required for opening this PDF document.");
        }
    }
}
