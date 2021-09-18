// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PdfToSvg.Filters
{
    internal class CryptFilter : Filter
    {
        public override Stream Decode(Stream encodedStream, PdfDictionary? decodeParms)
        {
            if (decodeParms == null)
            {
                throw new FilterException("Missing decode parms to Crypt filter.");
            }

            var securityHandler = decodeParms[InternalNames.SecurityHandler] as SecurityHandler;
            if (securityHandler == null)
            {
                throw new FilterException("The Crypt filter failed to get a reference to the security handler.");
            }

            var objectId = decodeParms[InternalNames.ObjectId] as PdfObjectId?;
            if (objectId == null)
            {
                throw new FilterException("The Crypt filter failed to get the container object id.");
            }

            var name = decodeParms.GetValueOrDefault(Names.Name, Names.Identity);
            return securityHandler.Decrypt(objectId.Value, name, encodedStream);
        }
    }
}
