// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.Container;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PdfToSvg.Tests.Images.Jpx.Container
{
    internal class JpxBoxParserTests
    {
        [Test]
        public void Parse_RawCodestream()
        {
            var data = new byte[] { 0xFF, 0x4F, 0x01, 0x02, 0x03 };

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.IsFalse(container.IsJp2);
            Assert.AreEqual(data, container.Codestream.ToArray());
        }

        [Test]
        public void Parse_RawCodestreamWithOffset()
        {
            var prefix = new byte[] { 0xAA, 0xBB };
            var codestream = new byte[] { 0xFF, 0x4F, 0x01, 0x02, 0x03 };
            var data = Concat(prefix, codestream);

            var container = JpxBoxParser.Parse(data, prefix.Length, codestream.Length);

            Assert.IsFalse(container.IsJp2);
            Assert.AreEqual(codestream, container.Codestream.ToArray());
        }

        [Test]
        public void Parse_Jp2Container()
        {
            var codestream = MinimalCodestream();
            var data = MinimalJp2File(codestream);

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.IsTrue(container.IsJp2);
            Assert.AreEqual(codestream, container.Codestream.ToArray());
            Assert.IsNotNull(container.ImageHeader);
            Assert.AreEqual(10, container.ImageHeader.Width);
            Assert.AreEqual(20, container.ImageHeader.Height);
            Assert.AreEqual(3, container.ImageHeader.NumberOfComponents);
            Assert.AreEqual(JpxEnumeratedColorSpace.SRgb, container.EnumeratedColorSpace);
        }

        [Test]
        public void Parse_BitsPerComponentMatchesComponentCount()
        {
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Box("jp2h", Concat(
                    ImageHeaderBox(numComponents: 3, bitsPerComponent: 255),
                    Box("bpcc", new byte[] { 7, 11, 15 }),
                    ColourSpecificationBox())),
                CodestreamBox(MinimalCodestream()));

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.AreEqual(new[] { 7, 11, 15 }, container.BitsPerComponent);
        }

        [Test]
        public void Parse_BitsPerComponentWithExtraEntryThrows()
        {
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Box("jp2h", Concat(
                    ImageHeaderBox(numComponents: 3, bitsPerComponent: 255),
                    Box("bpcc", new byte[] { 7, 11, 15, 7 }),
                    ColourSpecificationBox())),
                CodestreamBox(MinimalCodestream()));

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_Jp2ContainerWithLength0Codestream()
        {
            // Length-0 box means "extends to the end of the file" (I.4).
            var codestream = new byte[] { 0xFF, 0x4F, 0x11, 0x22, 0x33, 0x44 };
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Jp2HeaderBox(ColourSpecificationBox()),
                BoxLength0("jp2c", codestream)
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.IsTrue(container.IsJp2);
            Assert.AreEqual(codestream, container.Codestream.ToArray());
        }

        [Test]
        public void Parse_Jp2ContainerWithXLBox()
        {
            var codestream = MinimalCodestream();
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Jp2HeaderBox(ColourSpecificationBox()),
                BoxXL("jp2c", codestream)
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.IsTrue(container.IsJp2);
            Assert.AreEqual(codestream, container.Codestream.ToArray());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Parse_Jp2ContainerWithPalette(bool componentMappingFirst)
        {
            // pclr box: NE=2 entries, NPC=1 column, Bi=7 (8-bit unsigned), values 10 and 200.
            var paletteContent = new byte[] { 0, 2, 1, 0x07, 10, 200 };
            // cmap box: component 0 indexes palette column 0.
            var componentMappingContent = new byte[] { 0, 0, 1, 0 };
            var paletteBox = Box("pclr", paletteContent);
            var componentMappingBox = Box("cmap", componentMappingContent);
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                componentMappingFirst
                    ? Jp2HeaderBox(ColourSpecificationBox(), componentMappingBox, paletteBox)
                    : Jp2HeaderBox(ColourSpecificationBox(), paletteBox, componentMappingBox),
                CodestreamBox(MinimalCodestream())
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.IsNotNull(container.Palette);
            Assert.AreEqual(2, container.Palette.EntryCount);
            Assert.AreEqual(1, container.Palette.ColumnCount);
            Assert.AreEqual(10, container.Palette.Values[0, 0]);
            Assert.AreEqual(200, container.Palette.Values[1, 0]);
            Assert.AreEqual(JpxComponentMappingType.Palette, container.ComponentMappings[0].Type);
            Assert.AreEqual(0, container.ComponentMappings[0].PaletteColumnIndex);
        }

        [Test]
        public void Parse_ComponentMappingAtFileLevel()
        {
            // Some producers write too short a length on jp2h, causing trailing child boxes to end up at file level
            var paletteContent = new byte[] { 0, 2, 1, 0x07, 10, 200 };
            var componentMappingContent = new byte[] { 0, 0, 1, 0 };
            var cdefContent = new byte[] { 0, 1, 0, 0, 0, 0, 0, 1 };
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Jp2HeaderBox(ColourSpecificationBox(), Box("pclr", paletteContent)),
                Box("cmap", componentMappingContent),
                Box("cdef", cdefContent),
                CodestreamBox(MinimalCodestream())
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.IsNotNull(container.Palette);
            Assert.AreEqual(2, container.Palette.EntryCount);
            Assert.AreEqual(1, container.ComponentMappings.Length);
            Assert.AreEqual(JpxComponentMappingType.Palette, container.ComponentMappings[0].Type);
            Assert.AreEqual(1, container.ChannelDefinitions.Length);
        }

        [Test]
        public void Parse_DuplicateComponentMappingIgnored()
        {
            var paletteContent = new byte[] { 0, 2, 1, 0x07, 10, 200 };
            var firstComponentMapping = new byte[] { 0, 0, 1, 0 }; // Palette mapping
            var secondComponentMapping = new byte[] { 0, 0, 0, 0 }; // Direct mapping
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Jp2HeaderBox(
                    ColourSpecificationBox(),
                    Box("pclr", paletteContent),
                    Box("cmap", firstComponentMapping)),
                Box("cmap", secondComponentMapping),
                CodestreamBox(MinimalCodestream())
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.AreEqual(1, container.ComponentMappings.Length);
            Assert.AreEqual(JpxComponentMappingType.Palette, container.ComponentMappings[0].Type);
        }

        [Test]
        public void Parse_PaletteWithoutComponentMappingThrows()
        {
            var paletteContent = new byte[] { 0, 2, 1, 0x07, 10, 200 };
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Jp2HeaderBox(ColourSpecificationBox(), Box("pclr", paletteContent)),
                CodestreamBox(MinimalCodestream())
            );

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_Jp2ContainerWithComponentMappingAndChannelDefinition()
        {
            // pclr/cmap: one palette column generated from component 0.
            var paletteContent = new byte[] { 0, 1, 1, 0x07, 10 };
            var cmapContent = new byte[] { 0, 0, 1, 0 };

            // cdef: one channel, index 0, type 0 (colour), association 1.
            var cdefContent = new byte[] { 0, 1, 0, 0, 0, 0, 0, 1 };

            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Jp2HeaderBox(
                    ColourSpecificationBox(),
                    Box("pclr", paletteContent),
                    Box("cmap", cmapContent),
                    Box("cdef", cdefContent)),
                CodestreamBox(MinimalCodestream())
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.AreEqual(1, container.ComponentMappings.Length);
            Assert.AreEqual(0, container.ComponentMappings[0].ComponentIndex);
            Assert.AreEqual(JpxComponentMappingType.Palette, container.ComponentMappings[0].Type);

            Assert.AreEqual(1, container.ChannelDefinitions.Length);
            Assert.AreEqual(0, container.ChannelDefinitions[0].ChannelIndex);
            Assert.AreEqual(JpxChannelType.Color, container.ChannelDefinitions[0].Type);
            Assert.AreEqual(1, container.ChannelDefinitions[0].Association);
        }

        [TestCase("jpx ")]
        [TestCase("jpxb")]
        [TestCase("abcd")]
        public void Parse_NonJp2BrandAccepted(string brand)
        {
            // PDFs embed JPX baseline streams, and some files don't declare JP2 compatibility at all
            var data = Concat(
                SignatureBox(),
                FileTypeBox(brand, brand),
                Jp2HeaderBox(ColourSpecificationBox()),
                CodestreamBox(MinimalCodestream())
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.IsTrue(container.IsJp2);
            Assert.AreEqual(MinimalCodestream(), container.Codestream.ToArray());
        }

        [Test]
        public void Parse_MissingSignatureBoxThrows()
        {
            var data = Concat(FileTypeBox(), Jp2HeaderBox(ColourSpecificationBox()), CodestreamBox(MinimalCodestream()));

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_MissingFileTypeBoxThrows()
        {
            var data = Concat(SignatureBox(), Jp2HeaderBox(ColourSpecificationBox()), CodestreamBox(MinimalCodestream()));

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_MissingImageHeaderBoxThrows()
        {
            var data = Concat(SignatureBox(), FileTypeBox(), Box("jp2h", ColourSpecificationBox()), CodestreamBox(MinimalCodestream()));

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_MissingColourSpecificationBoxThrows()
        {
            var data = Concat(SignatureBox(), FileTypeBox(), Box("jp2h", ImageHeaderBox()), CodestreamBox(MinimalCodestream()));

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_MissingCodestreamBoxThrows()
        {
            var data = Concat(SignatureBox(), FileTypeBox(), Jp2HeaderBox(ColourSpecificationBox()));

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_TruncatedBoxHeaderThrows()
        {
            // Only 5 bytes: not enough for an 8-byte LBox+TBox header. The top-level box walk
            // requires at least 8 bytes to attempt the mandatory signature box.
            var data = new byte[] { 0, 0, 0, 20, 0 };

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_TruncatedNestedBoxHeaderThrows()
        {
            // The jp2h box's declared length leaves only 5 bytes for its child box, which is
            // not enough for an 8-byte LBox+TBox header.
            var truncatedChild = new byte[] { 0, 0, 0, 20, 0 };
            var data = Concat(SignatureBox(), FileTypeBox(), Box("jp2h", truncatedChild));

            Assert.Throws<EndOfStreamException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_BoxLengthExceedsStreamThrows()
        {
            // LBox declares a length far larger than the remaining data.
            var box = Box("jp2h", new byte[4]);
            WriteUInt32(box, 0, 1000);

            var data = Concat(SignatureBox(), FileTypeBox(), box);

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_BoxLengthTooShortForHeaderThrows()
        {
            // LBox in [1, 7] (other than exactly 1, the XLBox sentinel) is invalid: a box must be
            // at least 8 bytes for its own LBox+TBox header.
            var box = new byte[8];
            WriteUInt32(box, 0, 4);
            Array.Copy(System.Text.Encoding.ASCII.GetBytes("jp2h"), 0, box, 4, 4);

            var data = Concat(SignatureBox(), FileTypeBox(), box);

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_XLBoxLengthTooSmallThrows()
        {
            var content = MinimalCodestream();
            var box = BoxXL("jp2c", content);

            // Corrupt the XLBox value to something smaller than the 16-byte minimum.
            WriteUInt64(box, 8, 4);

            var data = Concat(SignatureBox(), FileTypeBox(), Jp2HeaderBox(ColourSpecificationBox()), box);

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_XLBoxTruncatedThrows()
        {
            // LBox == 1 (XLBox marker) but not enough bytes remain for the 8-byte XLBox field.
            var box = new byte[10];
            WriteUInt32(box, 0, 1);
            Array.Copy(System.Text.Encoding.ASCII.GetBytes("jp2c"), 0, box, 4, 4);

            var data = Concat(SignatureBox(), FileTypeBox(), Jp2HeaderBox(ColourSpecificationBox()), box);

            Assert.Throws<EndOfStreamException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_DuplicateImageHeaderBoxIgnored()
        {
            // Jp2HeaderBox prepends a default 10x20 ihdr, so this file has a second, conflicting ihdr
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Jp2HeaderBox(ImageHeaderBox(width: 99, height: 98), ColourSpecificationBox()),
                CodestreamBox(MinimalCodestream())
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.AreEqual(10, container.ImageHeader.Width);
            Assert.AreEqual(20, container.ImageHeader.Height);
        }

        [Test]
        public void Parse_DuplicateJp2HeaderBoxIgnored()
        {
            var jp2header = Jp2HeaderBox(ColourSpecificationBox());
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                jp2header,
                jp2header,
                CodestreamBox(MinimalCodestream())
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.AreEqual(10, container.ImageHeader.Width);
            Assert.AreEqual(MinimalCodestream(), container.Codestream.ToArray());
        }

        [Test]
        public void Parse_CodestreamBeforeHeaderThrows()
        {
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                CodestreamBox(MinimalCodestream()),
                Jp2HeaderBox(ColourSpecificationBox())
            );

            Assert.Throws<JpxException>(() => JpxBoxParser.Parse(data, 0, data.Length));
        }

        [Test]
        public void Parse_UnrecognizedBoxIsSkipped()
        {
            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Box("xml ", new byte[] { 1, 2, 3, 4 }),
                Jp2HeaderBox(ColourSpecificationBox()),
                CodestreamBox(MinimalCodestream())
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.IsTrue(container.IsJp2);
            Assert.AreEqual(MinimalCodestream(), container.Codestream.ToArray());
        }

        [Test]
        public void Parse_SecondCodestreamBoxIgnored()
        {
            var firstCodestream = MinimalCodestream();
            var secondCodestream = new byte[] { 0xFF, 0x4F, 0x99 };

            var data = Concat(
                SignatureBox(),
                FileTypeBox(),
                Jp2HeaderBox(ColourSpecificationBox()),
                CodestreamBox(firstCodestream),
                CodestreamBox(secondCodestream)
            );

            var container = JpxBoxParser.Parse(data, 0, data.Length);

            Assert.AreEqual(firstCodestream, container.Codestream.ToArray());
        }

        // ITU-T T.800 (06/2019) Section I.4: box = LBox (4) + TBox (4) + [XLBox (8)] + content.
        private static byte[] Box(string type, byte[] content)
        {
            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            Assert.AreEqual(4, typeBytes.Length);

            var length = 8 + content.Length;
            var box = new byte[length];

            WriteUInt32(box, 0, (uint)length);
            Array.Copy(typeBytes, 0, box, 4, 4);
            Array.Copy(content, 0, box, 8, content.Length);

            return box;
        }

        // Builds a box with LBox == 0, meaning "extends to the end of the file/parent" (I.4).
        private static byte[] BoxLength0(string type, byte[] content)
        {
            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            var box = new byte[8 + content.Length];

            WriteUInt32(box, 0, 0);
            Array.Copy(typeBytes, 0, box, 4, 4);
            Array.Copy(content, 0, box, 8, content.Length);

            return box;
        }

        // Builds an XLBox (LBox == 1, 64-bit XLBox length follows TBox).
        private static byte[] BoxXL(string type, byte[] content)
        {
            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            var length = (ulong)(16 + content.Length);
            var box = new byte[16 + content.Length];

            WriteUInt32(box, 0, 1);
            Array.Copy(typeBytes, 0, box, 4, 4);
            WriteUInt64(box, 8, length);
            Array.Copy(content, 0, box, 16, content.Length);

            return box;
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static void WriteUInt64(byte[] buffer, int offset, ulong value)
        {
            for (var i = 0; i < 8; i++)
            {
                buffer[offset + i] = (byte)(value >> (56 - i * 8));
            }
        }

        private static byte[] Concat(params byte[][] chunks) => chunks.SelectMany(x => x).ToArray();

        private static byte[] SignatureBox() => Box("jP  ", new byte[] { 0x0d, 0x0a, 0x87, 0x0a });

        private static byte[] FileTypeBox(string brand = "jp2 ", string compatibility = "jp2 ") => Box("ftyp", Concat(
            System.Text.Encoding.ASCII.GetBytes(brand),
            new byte[] { 0, 0, 0, 0 }, // Minor version
            System.Text.Encoding.ASCII.GetBytes(compatibility)
        ));

        private static byte[] ImageHeaderBox(int width = 10, int height = 20, int numComponents = 3, int bitsPerComponent = 7)
        {
            var content = new byte[14];
            WriteUInt32(content, 0, (uint)height);
            WriteUInt32(content, 4, (uint)width);
            content[8] = 0;
            content[9] = (byte)numComponents;
            content[10] = (byte)bitsPerComponent;
            content[11] = 7; // Compression type (fixed value per I.5.3.1)
            content[12] = 0; // Unknown colour space
            content[13] = 0; // Intellectual property
            return Box("ihdr", content);
        }

        private static byte[] ColourSpecificationBox(int enumCs = 16 /* sRGB */)
        {
            var content = new byte[7];
            content[0] = 1; // METH = enumerated colour space
            content[1] = 0; // Precedence
            content[2] = 0; // Approximation
            WriteUInt32(content, 3, (uint)enumCs);
            return Box("colr", content);
        }

        private static byte[] Jp2HeaderBox(params byte[][] children) =>
            Box("jp2h", Concat(new[] { ImageHeaderBox() }.Concat(children).ToArray()));

        private static byte[] CodestreamBox(byte[] codestream) => Box("jp2c", codestream);

        private static byte[] MinimalCodestream() => new byte[] { 0xFF, 0x4F }; // SOC marker

        private static byte[] MinimalJp2File(byte[] codestream) => Concat(
            SignatureBox(),
            FileTypeBox(),
            Jp2HeaderBox(ColourSpecificationBox()),
            CodestreamBox(codestream)
        );
    }
}
