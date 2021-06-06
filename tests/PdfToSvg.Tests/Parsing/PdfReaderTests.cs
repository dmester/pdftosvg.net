// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Parsing
{
    public class PdfReaderTests
    {
        [Test]
        public void FlattenedPages()
        {
            var root = new PdfDictionary
            {
                { Names.Pages, new PdfDictionary {
                    { Names.Type, Names.Pages },
                    { Names.MediaBox, "A" },
                    { Names.Producer, "NotInherited" },
                    { Names.Kids, new object[] {
                        new PdfDictionary {
                            { Names.Type, Names.Pages },
                            { Names.MediaBox, "B" },
                            { Names.CropBox, "D" },
                            { Names.Kids, new object[] {
                                new PdfDictionary {
                                    { Names.Type, Names.Page },
                                    { Names.MediaBox, "C" },
                                },

                                new PdfDictionary {
                                    { Names.Type, Names.Page },
                                    { Names.Rotate, "E" },
                                },
                            } }
                        },

                        new PdfDictionary {
                            { Names.Type, Names.Page },
                        },
                    } }
                } }
            };

            var pages = PdfReader
                .GetFlattenedPages(root)
                .Select(page => new
                {
                    MediaBox = page[Names.MediaBox],
                    CropBox = page[Names.CropBox],
                    Rotate = page[Names.Rotate],
                    Producer = page[Names.Producer],
                })
                .ToList();

            var expected = new[]
            {
                new
                {
                    MediaBox = (object)"C",
                    CropBox = (object)"D",
                    Rotate = (object)null,
                    Producer = (object)null,
                },
                new
                {
                    MediaBox = (object)"B",
                    CropBox = (object)"D",
                    Rotate = (object)"E",
                    Producer = (object)null,
                },
                new
                {
                    MediaBox = (object)"A",
                    CropBox = (object)null,
                    Rotate = (object)null,
                    Producer = (object)null,
                },
            };

            Assert.AreEqual(expected, pages);
        }
    }
}
