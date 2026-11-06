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
        private static readonly PdfName testValue = new PdfName("TestValue");
        private static readonly PdfName testSelf = new PdfName("TestSelf");

        [Test]
        public void InlineReferences_SharedCyclicArrays()
        {
            var first = new object[] { new PdfRef(9, 0) };
            var shared = new object[] { new PdfRef(8, 0), new PdfRef(9, 0), new PdfRef(10, 0) };
            var objects = new Dictionary<PdfObjectId, object>
            {
                { new PdfObjectId(8, 0), first },
                { new PdfObjectId(9, 0), shared },
                { new PdfObjectId(10, 0), shared },
            };

            PdfReader.InlineReferences(objects);

            Assert.AreSame(shared, first[0]);
            Assert.AreSame(first, shared[0]);
            Assert.AreSame(shared, shared[1]);
            Assert.AreSame(shared, shared[2]);
            Assert.AreSame(objects[new PdfObjectId(9, 0)], objects[new PdfObjectId(10, 0)]);
        }

        [Test]
        public void InlineReferences_MixedCycle()
        {
            var dict = new PdfDictionary { { testValue, new PdfRef(3, 0) } };
            var array = new object[] { new PdfRef(1, 0), null };
            array[1] = array;
            dict[Names.Kids] = array;
            dict[testSelf] = dict;
            dict.MakeIndirectObject(new PdfObjectId(1, 0), null);
            var objects = new Dictionary<PdfObjectId, object>
            {
                { new PdfObjectId(1, 0), dict },
                { new PdfObjectId(2, 0), array },
                { new PdfObjectId(3, 0), 42 },
            };

            PdfReader.InlineReferences(objects);

            Assert.AreEqual(42, dict[testValue]);
            Assert.AreSame(array, dict[Names.Kids]);
            Assert.AreSame(dict, dict[testSelf]);
            Assert.AreSame(dict, array[0]);
            Assert.AreSame(array, array[1]);
        }

        [Test]
        public void InlineReferences_DeepContainers()
        {
            var leaf = new PdfDictionary { { testValue, new PdfRef(2, 0) } };
            object root = leaf;

            for (var i = 0; i < 20000; i++)
            {
                root = new PdfDictionary { { Names.Kids, new object[] { root } } };
            }

            var objects = new Dictionary<PdfObjectId, object>
            {
                { new PdfObjectId(1, 0), root },
                { new PdfObjectId(2, 0), 42 },
            };

            PdfReader.InlineReferences(objects);

            Assert.AreEqual(42, leaf[testValue]);
        }

        [Test]
        public void InlineReferences_ShallowRepeatedPasses()
        {
            var reference = new PdfRef(2, 0);
            var dict = new PdfDictionary { { testValue, reference } };
            var nested = new object[] { reference };
            var trailer = new PdfDictionary
            {
                { Names.Root, new PdfRef(1, 0) },
                { Names.Kids, nested },
            };
            var objects = new Dictionary<PdfObjectId, object>
            {
                { new PdfObjectId(2, 0), 42 },
            };

            PdfReader.InlineReferences(objects, trailer, recurse: false);

            Assert.IsInstanceOf<PdfRef>(trailer[Names.Root]);

            objects.Add(new PdfObjectId(1, 0), dict);
            PdfReader.InlineReferences(objects, trailer, recurse: false);

            Assert.AreSame(dict, trailer[Names.Root]);
            Assert.AreSame(reference, dict[testValue]);
            Assert.AreSame(reference, nested[0]);

            PdfReader.InlineReferences(objects);

            Assert.AreEqual(42, dict[testValue]);
        }

        [Test]
        public void InlineReferences_RepeatedPasses()
        {
            var reference = new PdfRef(2, 0);
            var array = new object[] { reference };
            var dict = new PdfDictionary { { testValue, reference }, { Names.Kids, array } };
            var objects = new Dictionary<PdfObjectId, object>
            {
                { new PdfObjectId(1, 0), dict },
            };

            PdfReader.InlineReferences(objects);

            Assert.AreSame(reference, dict[testValue]);
            Assert.AreSame(reference, array[0]);

            objects.Add(new PdfObjectId(2, 0), 42);
            PdfReader.InlineReferences(objects);

            Assert.AreEqual(42, dict[testValue]);
            Assert.AreEqual(42, array[0]);
        }

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
