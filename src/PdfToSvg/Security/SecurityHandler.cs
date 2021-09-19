// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Security
{
    internal abstract class SecurityHandler
    {
        /// <summary>
        /// Creates the security handler for decrypting a document.
        /// </summary>
        /// <param name="trailerDict">Trailer dictionary before object inlining.</param>
        /// <param name="encryptDict">Encryption dictionary.</param>
        /// <param name="options">Options passed to <see cref="PdfDocument.Open(Stream, bool, OpenOptions?, System.Threading.CancellationToken)"/>.</param>
        public static SecurityHandler Create(PdfDictionary trailerDict, PdfDictionary encryptDict, OpenOptions options)
        {
            var securityHandler = encryptDict.GetValueOrDefault(Names.Filter, Names.Standard);
            if (securityHandler == Names.Standard)
            {
                return new StandardSecurityHandler(trailerDict, encryptDict, options);
            }

            throw SecurityExceptions.UnsupportedSecurityHandler(securityHandler);
        }

        /// <summary>
        /// Creates a decode params dictionary used for implicit /Crypt filters inserted where a /Crypt filter is not explicitly specified.
        /// </summary>
        public virtual PdfDictionary? CreateImplicitCryptDecodeParms() { return null; }

        /// <summary>
        /// Decrypts an object stream.
        /// </summary>
        /// <param name="id">The id of the object whose stream is decrypted.</param>
        /// <param name="filterName">Decryption filter name specified in the /Decrypt filter decode params.</param>
        /// <param name="encryptedStream">Encrypted stream content.</param>
        /// <returns></returns>
        public abstract Stream Decrypt(PdfObjectId id, PdfName filterName, Stream encryptedStream);

        /// <summary>
        /// Decrypts a string located inside an indirect object with a specified id.
        /// </summary>
        /// <param name="id">The id of the parent indirect object.</param>
        /// <param name="s">String to decrypt.</param>
        public abstract PdfString Decrypt(PdfObjectId id, PdfString s);

        public abstract bool IsOwnerAuthenticated { get; }
    }
}
