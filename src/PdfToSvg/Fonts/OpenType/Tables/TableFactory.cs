// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    internal class TableFactory
    {
        private readonly Func<OpenTypeReader, IBaseTable?>? factoryMethod;
        private readonly Func<OpenTypeReader, OpenTypeReaderContext, IBaseTable?>? extendedFactoryMethod;

        public TableFactory(string? tag, Func<OpenTypeReader, IBaseTable?> factoryMethod)
        {
            Tag = tag;
            this.factoryMethod = factoryMethod;
        }

        public TableFactory(string? tag, Func<OpenTypeReader, OpenTypeReaderContext, IBaseTable?> factoryMethod)
        {
            Tag = tag;
            extendedFactoryMethod = factoryMethod;
        }

        public string? Tag { get; }

        public IBaseTable? Create(OpenTypeReader reader, OpenTypeReaderContext context)
        {
            if (factoryMethod != null)
            {
                return factoryMethod(reader);
            }

            if (extendedFactoryMethod != null)
            {
                return extendedFactoryMethod(reader, context);
            }

            return null;
        }
    }
}
