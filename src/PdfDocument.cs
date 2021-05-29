using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using PdfToSvg.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PdfToSvg
{
    public class PdfDocument : IDisposable
    {
        private readonly PdfDictionary root;
        private readonly PdfDictionary info;
        private readonly Dictionary<PdfObjectId, object?> objects;
        private InputFile? file;

        internal PdfDocument(InputFile file, PdfDictionary? trailer, Dictionary<PdfObjectId, object?> objects)
        {
            this.info = trailer.GetDictionaryOrEmpty(Names.Info);
            this.root = trailer.GetDictionaryOrEmpty(Names.Root);
            this.objects = objects;
            this.file = file;
            this.Pages = GetPages(root);
        }

        public static PdfDocument Open(Stream stream)
        {
            return PdfReader.Read(new InputFile(stream));
        }

        public static PdfDocument Open(string path)
        {
            var file = new InputFile(path);

            try
            {
                return PdfReader.Read(file);
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }

        public static async Task<PdfDocument> OpenAsync(Stream stream)
        {
            return await PdfReader.ReadAsync(new InputFile(stream));
        }

        public static async Task<PdfDocument> OpenAsync(string path)
        {
            var file = new InputFile(path);

            try
            {
                return await PdfReader.ReadAsync(file);
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }

        public string? Title => info.GetValueOrDefault<PdfString?>(Names.Title)?.ToString();
        public string? Author => info.GetValueOrDefault<PdfString?>(Names.Author)?.ToString();
        public string? Subject => info.GetValueOrDefault<PdfString?>(Names.Subject)?.ToString();
        public string? Keywords => info.GetValueOrDefault<PdfString?>(Names.Keywords)?.ToString();
        public string? Creator => info.GetValueOrDefault<PdfString?>(Names.Creator)?.ToString();
        public string? Producer => info.GetValueOrDefault<PdfString?>(Names.Producer)?.ToString();
        public DateTimeOffset? CreationDate => info.GetValueOrDefault<DateTimeOffset?>(Names.CreationDate);
        public DateTimeOffset? ModDate => info.GetValueOrDefault<DateTimeOffset?>(Names.ModDate);

        public IList<PdfPage> Pages { get; }

        private IList<PdfPage> GetPages(PdfDictionary root)
        {
            var pages = new List<PdfPage>();
            var pagesStack = new Stack<IEnumerator>();

            if (root.TryGetDictionary(Names.Pages, out var rootPagesDict))
            {
                if (rootPagesDict.TryGetArray(Names.Kids, out var kids))
                {
                    pagesStack.Push(kids.GetEnumerator());
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
                                pagesStack.Push(kids.GetEnumerator());
                            }
                        }
                        else if (name == Names.Page)
                        {
                            pages.Add(new PdfPage(this, dict));
                        }
                    }
                }
                else
                {
                    (enumerator as IDisposable)?.Dispose();
                    pagesStack.Pop();
                }
            }

            return pages;
        }

        public void Dispose()
        {
            file?.Dispose();
            file = null;
        }
    }
}
