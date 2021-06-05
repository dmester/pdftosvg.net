// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal static class Names
    {
        public static PdfName WinAnsiEncoding { get; } = new PdfName("WinAnsiEncoding");
        public static PdfName MacRomanEncoding { get; } = new PdfName("MacRomanEncoding");
        public static PdfName MacExpertEncoding { get; } = new PdfName("MacExpertEncoding");

        public static PdfName Info { get; } = new PdfName("Info");
        public static PdfName Root { get; } = new PdfName("Root");
        public static PdfName Encrypt { get; } = new PdfName("Encrypt");

        public static PdfName Type { get; } = new PdfName("Type");
        public static PdfName Subtype { get; } = new PdfName("Subtype");
        public static PdfName First { get; } = new PdfName("First");
        public static PdfName Prev { get; } = new PdfName("Prev");
        public static PdfName Index { get; } = new PdfName("Index");
        public static PdfName Length { get; } = new PdfName("Length");

        public static PdfName Page { get; } = new PdfName("Page");
        public static PdfName Pages { get; } = new PdfName("Pages");
        public static PdfName Kids { get; } = new PdfName("Kids");

        public static PdfName MediaBox { get; } = new PdfName("MediaBox");
        public static PdfName CropBox { get; } = new PdfName("CropBox");

        public static PdfName Matrix { get; } = new PdfName("Matrix");
        public static PdfName BBox { get; } = new PdfName("BBox");

        public static PdfName Contents { get; } = new PdfName("Contents");
        public static PdfName Resources { get; } = new PdfName("Resources");
        public static PdfName ExtGState { get; } = new PdfName("ExtGState");
        public static PdfName Font { get; } = new PdfName("Font");

        public static PdfName Encoding { get; } = new PdfName("Encoding");
        public static PdfName BaseEncoding { get; } = new PdfName("BaseEncoding");
        public static PdfName Differences { get; } = new PdfName("Differences");

        public static PdfName Type0 { get; } = new PdfName("Type0");
        public static PdfName CIDFontType0 { get; } = new PdfName("CIDFontType0");
        public static PdfName CIDFontType2 { get; } = new PdfName("CIDFontType2");
        public static PdfName Type1 { get; } = new PdfName("Type1");
        public static PdfName Type3 { get; } = new PdfName("Type3");
        public static PdfName MMType1 { get; } = new PdfName("MMType1");
        public static PdfName TrueType { get; } = new PdfName("TrueType");
        public static PdfName BaseFont { get; } = new PdfName("BaseFont");
        public static PdfName ToUnicode { get; } = new PdfName("ToUnicode");
        public static PdfName DescendantFonts { get; } = new PdfName("DescendantFonts");
        public static PdfName FontDescriptor { get; } = new PdfName("FontDescriptor");
        public static PdfName W { get; } = new PdfName("W");
        public static PdfName MissingWidth { get; } = new PdfName("MissingWidth");
        public static PdfName Widths { get; } = new PdfName("Widths");
        public static PdfName FirstChar { get; } = new PdfName("FirstChar");
        public static PdfName LastChar { get; } = new PdfName("LastChar");


        public static PdfName XObject { get; } = new PdfName("XObject");
        public static PdfName Form { get; } = new PdfName("Form");
        public static PdfName Image { get; } = new PdfName("Image");
        public static PdfName Width { get; } = new PdfName("Width");
        public static PdfName Height { get; } = new PdfName("Height");
        public static PdfName SMask { get; } = new PdfName("SMask");
        public static PdfName Mask { get; } = new PdfName("Mask");
        public static PdfName ImageMask { get; } = new PdfName("ImageMask");
        public static PdfName Decode { get; } = new PdfName("Decode");

        public static PdfName Filter { get; } = new PdfName("Filter");
        public static PdfName DCTDecode { get; } = new PdfName("DCTDecode");
        public static PdfName FlateDecode { get; } = new PdfName("FlateDecode");
        public static PdfName ASCIIHexDecode { get; } = new PdfName("ASCIIHexDecode");
        public static PdfName ASCII85Decode { get; } = new PdfName("ASCII85Decode");
        public static PdfName LZWDecode { get; } = new PdfName("LZWDecode");
        public static PdfName CCITTFaxDecode { get; } = new PdfName("CCITTFaxDecode");
        public static PdfName RunLengthDecode { get; } = new PdfName("RunLengthDecode");

        public static PdfName DecodeParms { get; } = new PdfName("DecodeParms");
        public static PdfName EarlyChange { get; } = new PdfName("EarlyChange");
        public static PdfName Predictor { get; } = new PdfName("Predictor");
        public static PdfName Columns { get; } = new PdfName("Columns");
        public static PdfName BitsPerComponent { get; } = new PdfName("BitsPerComponent");
        public static PdfName Colors { get; } = new PdfName("Colors");
        public static PdfName Interpolate { get; } = new PdfName("Interpolate");

        public static PdfName ColorSpace { get; } = new PdfName("ColorSpace");
        public static PdfName DeviceGray { get; } = new PdfName("DeviceGray");
        public static PdfName DeviceRGB { get; } = new PdfName("DeviceRGB");
        public static PdfName DeviceCMYK { get; } = new PdfName("DeviceCMYK");
        public static PdfName Indexed { get; } = new PdfName("Indexed");
        public static PdfName ICCBased { get; } = new PdfName("ICCBased");
        public static PdfName Separation { get; } = new PdfName("Separation");

        public static PdfName Alternate { get; } = new PdfName("Alternate");
        public static PdfName N { get; } = new PdfName("N");

        public static PdfName Title { get; } = new PdfName("Title");
        public static PdfName Author { get; } = new PdfName("Author");
        public static PdfName Subject { get; } = new PdfName("Subject");
        public static PdfName Keywords { get; } = new PdfName("Keywords");
        public static PdfName Creator { get; } = new PdfName("Creator");
        public static PdfName Producer { get; } = new PdfName("Producer");
        public static PdfName CreationDate { get; } = new PdfName("CreationDate");
        public static PdfName ModDate { get; } = new PdfName("ModDate");
        public static PdfName Trapped { get; } = new PdfName("Trapped");
    }

    internal static class AbbreviatedNames
    {
        // PDF spec 1.7, table 93, page 223
        // Abbreviations used in inline images

        public static PdfName Translate(PdfName input, bool isRootDictionary)
        {
            switch (input.Value)
            {
                case "BPC": return Names.BitsPerComponent;
                case "CS": return Names.ColorSpace;
                case "D": return Names.Decode;
                case "DP": return Names.DecodeParms;
                case "F": return Names.Filter;
                case "H": return Names.Height;
                case "IM": return Names.ImageMask;
                case "I": return isRootDictionary ? Names.Interpolate : Names.Indexed; 
                case "W": return Names.Width;
                case "G": return Names.DeviceGray;
                case "RGB": return Names.DeviceRGB;
                case "CMYK": return Names.DeviceCMYK;
                case "AHx": return Names.ASCIIHexDecode;
                case "A85": return Names.ASCII85Decode;
                case "LZW": return Names.LZWDecode;
                case "Fl": return Names.FlateDecode;
                case "RL": return Names.RunLengthDecode;
                case "CCF": return Names.CCITTFaxDecode;
                case "DCT": return Names.DCTDecode;
                default: return input;
            }
        }
    }
}
