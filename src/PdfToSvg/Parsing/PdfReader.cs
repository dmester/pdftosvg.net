// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using PdfToSvg.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal static class PdfReader
    {
        // PDF spec 1.7, Table 30, page 85
        private static readonly HashSet<PdfName> InheritablePageAttributes = new HashSet<PdfName>
        {
            Names.Resources,
            Names.MediaBox,
            Names.CropBox,
            Names.Rotate,
        };

        public static PdfDocument Read(InputFile file, OpenOptions options, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var reader = file.CreateReader(cancellationToken);
            var parser = new DocumentParser(file, reader);

            parser.ReadFileHeader();

            var startxref = parser.ReadStartXRef();
            var xrefTable = parser.ReadXRefTables(startxref, cancellationToken);
            var objects = parser.ReadAllObjects(xrefTable, cancellationToken);

            InlineReferences(objects, xrefTable.Trailer, recurse: false);

            var decrypted = Decrypt(xrefTable.Trailer, objects, options, out var permissions);

            InlineReferences(objects);

            return new PdfDocument(file, xrefTable.Trailer, permissions, decrypted);
        }

#if HAVE_ASYNC
        public static async Task<PdfDocument> ReadAsync(InputFile file, OpenOptions options, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var reader = await file.CreateReaderAsync(cancellationToken).ConfigureAwait(false);

            var parser = new DocumentParser(file, reader);

            await parser.ReadFileHeaderAsync().ConfigureAwait(false);

            var startxref = await parser.ReadStartXRefAsync().ConfigureAwait(false);
            var xrefTable = parser.ReadXRefTables(startxref, cancellationToken); // TODO make async
            var objects = parser.ReadAllObjects(xrefTable, cancellationToken); // TODO make async

            InlineReferences(objects, xrefTable.Trailer, recurse: false);

            var decrypted = Decrypt(xrefTable.Trailer, objects, options, out var permissions);

            InlineReferences(objects);

            return new PdfDocument(file, xrefTable.Trailer, permissions, decrypted);
        }
#endif

        private static bool Decrypt(PdfDictionary trailerDict, Dictionary<PdfObjectId, object?> objects, OpenOptions options, out DocumentPermissions permissions)
        {
            if (!trailerDict.TryGetDictionary(Names.Encrypt, out var encryptDict))
            {
                // Not encrypted
                permissions = new DocumentPermissions();
                return false;
            }

            var securityHandler = SecurityHandler.Create(trailerDict, encryptDict, options);
            var modifications = new List<KeyValuePair<PdfObjectId, object?>>();

            foreach (var property in objects)
            {
                // Encrypt dictionary is not encrypted
                if (property.Value == encryptDict)
                {
                    continue;
                }

                var decrypted = Decrypt(securityHandler, property.Key, property.Value);
                if (decrypted != property.Value)
                {
                    modifications.Add(new KeyValuePair<PdfObjectId, object?>(property.Key, decrypted));
                }
            }

            foreach (var modifiation in modifications)
            {
                objects[modifiation.Key] = modifiation.Value;
            }

            permissions = new DocumentPermissions(
                encryptDict.GetValueOrDefault(Names.R, 2),
                encryptDict.GetValueOrDefault(Names.P, -1),
                securityHandler.IsOwnerAuthenticated);
            return true;
        }

        private static object? Decrypt(SecurityHandler securityHandler, PdfObjectId objectId, object? input)
        {
            if (input is PdfString s)
            {
                return securityHandler.Decrypt(objectId, s);
            }

            if (input is PdfDictionary dict)
            {
                var modifications = new List<KeyValuePair<PdfName, object?>>();

                foreach (var property in dict)
                {
                    var decrypted = Decrypt(securityHandler, objectId, property.Value);
                    if (decrypted != property.Value)
                    {
                        modifications.Add(new KeyValuePair<PdfName, object?>(property.Key, decrypted));
                    }
                }

                foreach (var modifiation in modifications)
                {
                    dict[modifiation.Key] = modifiation.Value;
                }

                if (dict.Stream != null)
                {
                    var cryptDecodeParms = securityHandler.CreateImplicitCryptDecodeParms();
                    if (cryptDecodeParms != null)
                    {
                        cryptDecodeParms[InternalNames.ObjectId] = objectId;
                        cryptDecodeParms[InternalNames.SecurityHandler] = securityHandler;

                        dict.Stream.CryptDecodeParms = cryptDecodeParms;
                    }
                }

                return dict;
            }

            if (input is object?[] arr)
            {
                for (var i = 0; i < arr.Length; i++)
                {
                    arr[i] = Decrypt(securityHandler, objectId, arr[i]);
                }
            }

            return input;
        }

        public static IList<PdfDictionary> GetFlattenedPages(PdfDictionary root)
        {
            var pages = new List<PdfDictionary>();
            var pagesStack = new Stack<IEnumerator>();
            var parentStack = new Stack<PdfDictionary>();

            if (root.TryGetDictionary(Names.Pages, out var rootPagesDict))
            {
                if (rootPagesDict.TryGetArray(Names.Kids, out var kids))
                {
                    pagesStack.Push(kids.GetEnumerator());
                    parentStack.Push(rootPagesDict);
                }
            }

            while (pagesStack.Count > 0)
            {
                var enumerator = pagesStack.Peek();
                if (enumerator.MoveNext())
                {
                    var value = enumerator.Current;

                    if (value is PdfDictionary dict && dict.TryGetName(Names.Type, out var name))
                    {
                        if (name == Names.Pages)
                        {
                            if (dict.TryGetArray(Names.Kids, out var kids))
                            {
                                InheritAttributes(parentStack.Peek(), dict);
                                pagesStack.Push(kids.GetEnumerator());
                                parentStack.Push(dict);
                            }
                        }
                        else if (name == Names.Page)
                        {
                            InheritAttributes(parentStack.Peek(), dict);
                            pages.Add(dict);
                        }
                    }
                }
                else
                {
                    (enumerator as IDisposable)?.Dispose();
                    pagesStack.Pop();
                    parentStack.Pop();
                }
            }

            return pages;
        }

        private static void InheritAttributes(PdfDictionary parent, PdfDictionary child)
        {
            foreach (var property in parent)
            {
                if (InheritablePageAttributes.Contains(property.Key) && !child.ContainsKey(property.Key))
                {
                    child.Add(property);
                }
            }
        }

        private static void InlineReferences(Dictionary<PdfObjectId, object?> objects)
        {
            foreach (var pair in objects)
            {
                InlineReferences(objects, pair.Value, true);
            }
        }

        private static void InlineReferences(Dictionary<PdfObjectId, object?> objects, object? value, bool recurse)
        {
            if (value is PdfDictionary dict)
            {
                var refs = new List<KeyValuePair<PdfName, PdfObjectId>>();

                foreach (var pair in dict)
                {
                    if (pair.Value is PdfRef reference)
                    {
                        refs.Add(new KeyValuePair<PdfName, PdfObjectId>(pair.Key, reference.Id));
                    }
                    else if (recurse)
                    {
                        InlineReferences(objects, pair.Value, recurse);
                    }
                }

                foreach (var reference in refs)
                {
                    if (objects.TryGetValue(reference.Value, out var referencedValue))
                    {
                        dict[reference.Key] = referencedValue;
                    }
                    else
                    {
                        Log.WriteLine($"Reference to missing object {reference.Value}.");
                    }
                }
            }
            else if (value is object?[] arr)
            {
                for (var i = 0; i < arr.Length; i++)
                {
                    if (arr[i] is PdfRef reference)
                    {
                        if (objects.TryGetValue(reference.Id, out var referencedValue))
                        {
                            arr[i] = referencedValue;
                        }
                        else
                        {
                            Log.WriteLine($"Reference to missing object ({reference.Id}).");
                        }
                    }
                    else if (recurse)
                    {
                        InlineReferences(objects, arr[i], recurse);
                    }
                }
            }
        }
    }
}
