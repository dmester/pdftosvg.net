// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PdfToSvg.Tests.DocumentModel
{
    public class PdfNameTests
    {
        [Test]
        public void Create_KnownNameIsInterned()
        {
            var name = PdfName.Create("Root");
            Assert.AreEqual("Root", name.Value);
            Assert.AreSame(Names.Root, name);
        }

        [Test]
        public void Create_UnknownName()
        {
            var name = PdfName.Create("__MyCustomName__");
            Assert.AreEqual("__MyCustomName__", name.Value);
        }

        [Test]
        public void TryLookup_MultiByteUnicodeChars()
        {
            // If only the least significant 8 bits of the first chars are checked,
            // `Kũds` will match `Kids` and `UŒI` will match `URI`

            Assert.IsFalse(Names.TryLookup("UŒI", out _));
            Assert.IsFalse(Names.TryLookup("Kũds", out _));
        }

        [Test]
        public void TryLookup_ByteArrayKnownNameIsInterned()
        {
            var buffer = Encoding.ASCII.GetBytes("Root with trailing buffer contents");

            var found = Names.TryLookup(buffer, 4, out var name);

            Assert.IsTrue(found);
            Assert.AreSame(Names.Root, name);
        }

        [Test]
        public void TryLookup_ByteArrayUnknownName()
        {
            var buffer = Encoding.ASCII.GetBytes("Nope");

            var found = Names.TryLookup(buffer, buffer.Length, out var name);

            Assert.IsFalse(found);
            Assert.IsNull(name);
        }

        [Test]
        public void TryLookup_AllKnownNamesAreInterned()
        {
            var properties = typeof(Names)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(property => property.PropertyType == typeof(PdfName));

            foreach (var property in properties)
            {
                var expected = (PdfName)property.GetValue(null)!;
                var buffer = Encoding.ASCII.GetBytes(expected.Value + " trailing buffer contents");

                Assert.AreSame(expected, PdfName.Create(expected.Value), property.Name);
                Assert.IsTrue(Names.TryLookup(expected.Value, out var stringResult), property.Name);
                Assert.AreSame(expected, stringResult, property.Name);
                Assert.IsTrue(Names.TryLookup(buffer, expected.Value.Length, out var bufferResult), property.Name);
                Assert.AreSame(expected, bufferResult, property.Name);
            }
        }

        [Test]
        public void TryLookup_SameHashUnknownName()
        {
            const string Value = "Rooz";
            var buffer = Encoding.ASCII.GetBytes(Value);

            Assert.IsFalse(Names.TryLookup(Value, out var stringResult));
            Assert.IsNull(stringResult);
            Assert.IsFalse(Names.TryLookup(buffer, buffer.Length, out var bufferResult));
            Assert.IsNull(bufferResult);
        }
    }
}
