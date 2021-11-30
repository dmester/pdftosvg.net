// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Drawing
{
    internal class CssPropertyCollectionTests
    {
        [Test]
        public void Add()
        {
            var collection = new CssPropertyCollection();

            collection.Add("text-decoration", "underline");
            collection.Add("border-width", "2px");
            collection.Add("margin", "2px");
            collection.Add(new CssProperty("background", "red"));

            var other = new CssPropertyCollection();

            other.Add("text-decoration", "none");
            other.Add("font", "Arial");

            collection.Add(other);

            // Updated property
            collection.Add("margin", "3px");

            // Removed property
            collection.Add("border-width", null);

            Assert.AreEqual("text-decoration:none;margin:3px;background:red;font:Arial;", collection.ToString());
            Assert.AreEqual(4, collection.Count);
        }

        [Test]
        public void Remove()
        {
            var collection = new CssPropertyCollection();

            collection.Add("color", "red");
            collection.Add("text-decoration", "underline");
            collection.Add("border-width", "2px");
            collection.Add("margin", "2px");

            Assert.IsTrue(collection.Remove("color"));
            Assert.IsFalse(collection.Remove("margin", "3px"));
            Assert.IsFalse(collection.Remove(new CssProperty("margin", "3px")));
            Assert.IsTrue(collection.Remove(new CssProperty("border-width", "2px")));

            Assert.AreEqual("text-decoration:underline;margin:2px;", collection.ToString());
            Assert.AreEqual(2, collection.Count);
        }

        [Test]
        public void Indexer()
        {
            var collection = new CssPropertyCollection();

            collection["color"] = "red";
            collection["text-decoration"] = "underline";
            collection["border-width"] = "2px";
            collection["margin"] = "2px";

            collection["color"] = null;
            collection["margin"] = "3px";

            Assert.AreEqual("text-decoration:underline;border-width:2px;margin:3px;", collection.ToString());
            Assert.AreEqual(3, collection.Count);
        }

        [Test]
        public void CopyTo()
        {
            var collection = new CssPropertyCollection();

            collection["text-decoration"] = "underline";
            collection["color"] = "red";

            var array = new KeyValuePair<string, string>[4];
            ((ICollection<KeyValuePair<string, string>>)collection).CopyTo(array, 2);

            Assert.AreEqual("text-decoration", array[2].Key);
            Assert.AreEqual("color", array[3].Key);
        }
    }
}
