// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;

namespace PdfToSvg.Tests
{
    public class OptionalContentGroupManagerTests
    {
        [Test]
        public void GroupVisible_SharedExpressionsExceedBudget()
        {
            var ocgType = new PdfName("OCG");
            var visible = new PdfDictionary { { Names.Type, ocgType } };
            var hidden = new PdfDictionary { { Names.Type, ocgType } };
            var manager = new OptionalContentGroupManager();
            manager.Initialize(new PdfDictionary
            {
                { Names.OCProperties, new PdfDictionary
                {
                    { Names.OCGs, new object[] { visible, hidden } },
                    { Names.D, new PdfDictionary { { Names.OFF, new object[] { hidden } } } },
                } },
            });

            var expression = new object[] { Names.Or, hidden, hidden };
            for (var i = 0; i < 12; i++)
            {
                expression = new object[] { Names.Or, expression, expression };
            }

            var membership = new PdfDictionary
            {
                { Names.Type, Names.OCMD },
                { Names.VE, new object[] { Names.Or, expression, visible } },
            };

            Assert.IsFalse(manager.GroupVisible(membership));

            membership[Names.VE] = new object[] { Names.Or, visible, expression };
            Assert.IsTrue(manager.GroupVisible(membership));
        }
    }
}
