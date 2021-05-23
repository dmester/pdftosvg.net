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
        private readonly Dictionary<PdfObjectId, object> objects;
        private InputFile? file;

        internal PdfDocument(InputFile file, PdfDictionary trailer, Dictionary<PdfObjectId, object> objects)
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

        public string Title => info.GetValueOrDefault(Names.Title, PdfString.Empty).ToString();
        public string Author => info.GetValueOrDefault(Names.Author, PdfString.Empty).ToString();
        public string Subject => info.GetValueOrDefault(Names.Subject, PdfString.Empty).ToString();
        public string Keywords => info.GetValueOrDefault(Names.Keywords, PdfString.Empty).ToString();
        public string Creator => info.GetValueOrDefault(Names.Creator, PdfString.Empty).ToString();
        public string Producer => info.GetValueOrDefault(Names.Producer, PdfString.Empty).ToString();
        public DateTimeOffset? CreationDate => info.GetValueOrDefault(Names.CreationDate, (DateTimeOffset?)null);
        public DateTimeOffset? ModDate => info.GetValueOrDefault(Names.ModDate, (DateTimeOffset?)null);

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
