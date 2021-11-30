// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.OpenType.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.OpenType.Utils
{
    public class OptimalTableOrderTests
    {
        [TestCase(false,
            "prep,LTSH,OS/2,hmtx,maxp,VDMX,fpgm,NULL,gasp,hdmx,hhea,loca,PCLT,cvt ,kern,cmap,glyf,DSIG,name,post,head",
            "head,hhea,maxp,OS/2,hmtx,LTSH,VDMX,hdmx,cmap,fpgm,prep,cvt ,loca,glyf,kern,name,post,gasp,PCLT,DSIG,NULL")]
        [TestCase(true, "OS/2,xxxx,hhea,NULL,name,cmap,yyyy,head,post,CFF ,maxp", "head,hhea,maxp,OS/2,name,cmap,post,CFF ,xxxx,yyyy,NULL")]
        public void StorageSort(bool isCff, string input, string sorted)
        {
            var array = input.Split(',');

            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] == "NULL")
                {
                    array[i] = null;
                }
            }

            var list = new List<string>(array);

            OptimalTableOrder.StorageSort(array, x => x, isCff);
            OptimalTableOrder.StorageSort(list, x => x, isCff);

            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] == null)
                {
                    array[i] = "NULL";
                }
            }

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    list[i] = "NULL";
                }
            }

            Assert.AreEqual(sorted, string.Join(",", array), "Array");
            Assert.AreEqual(sorted, string.Join(",", list), "List");
        }

        [TestCase("head,hhea,maxp,OS/2,name,cmap,post,CFF ", "CFF ,OS/2,cmap,head,hhea,maxp,name,post")]
        public void DirectorySort(string input, string sorted)
        {
            var array = input.Split(',');
            var list = new List<string>(array);

            OptimalTableOrder.DirectorySort(array, x => x);
            OptimalTableOrder.DirectorySort(list, x => x);

            Assert.AreEqual(sorted, string.Join(",", array), "Array");
            Assert.AreEqual(sorted, string.Join(",", list), "List");
        }
    }
}
