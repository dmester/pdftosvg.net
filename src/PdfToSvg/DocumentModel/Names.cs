﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal static class InternalNames
    {
        public static PdfName SecurityHandler { get; } = new PdfName("$$SecurityHandler");
        public static PdfName ObjectId { get; } = new PdfName("$$ObjectId");
        public static PdfName Implicit { get; } = new PdfName("$$Implicit");
        public static PdfName FallbackFont { get; } = new PdfName("$$FallbackFont");
    }

    internal static class Names
    {
        public static PdfName WinAnsiEncoding { get; } = new PdfName("WinAnsiEncoding");
        public static PdfName MacRomanEncoding { get; } = new PdfName("MacRomanEncoding");
        public static PdfName MacExpertEncoding { get; } = new PdfName("MacExpertEncoding");
        public static PdfName IdentityH { get; } = new PdfName("Identity-H");
        public static PdfName IdentityV { get; } = new PdfName("Identity-V");

        public static PdfName Info { get; } = new PdfName("Info");
        public static PdfName Root { get; } = new PdfName("Root");
        public static PdfName Encrypt { get; } = new PdfName("Encrypt");
        public static PdfName Standard { get; } = new PdfName("Standard");
        public static PdfName ID { get; } = new PdfName("ID");
        public static PdfName R { get; } = new PdfName("R");
        public static PdfName V { get; } = new PdfName("V");
        public static PdfName O { get; } = new PdfName("O");
        public static PdfName U { get; } = new PdfName("U");
        public static PdfName OE { get; } = new PdfName("OE");
        public static PdfName UE { get; } = new PdfName("UE");
        public static PdfName P { get; } = new PdfName("P");
        public static PdfName CF { get; } = new PdfName("CF");
        public static PdfName CFM { get; } = new PdfName("CFM");
        public static PdfName None { get; } = new PdfName("None");
        public static PdfName V2 { get; } = new PdfName("V2");
        public static PdfName AESV2 { get; } = new PdfName("AESV2");
        public static PdfName AESV3 { get; } = new PdfName("AESV3");
        public static PdfName StmF { get; } = new PdfName("StmF");
        public static PdfName StrF { get; } = new PdfName("StrF");
        public static PdfName Name { get; } = new PdfName("Name");
        public static PdfName Perms { get; } = new PdfName("Perms");
        public static PdfName Identity { get; } = new PdfName("Identity");
        public static PdfName EncryptMetadata { get; } = new PdfName("EncryptMetadata");
        public static PdfName XRef { get; } = new PdfName("XRef");

        public static PdfName Type { get; } = new PdfName("Type");
        public static PdfName Subtype { get; } = new PdfName("Subtype");
        public static PdfName First { get; } = new PdfName("First");
        public static PdfName Prev { get; } = new PdfName("Prev");
        public static PdfName Index { get; } = new PdfName("Index");
        public static PdfName Length { get; } = new PdfName("Length");
        public static PdfName Length1 { get; } = new PdfName("Length1");
        public static PdfName Length2 { get; } = new PdfName("Length2");

        public static PdfName Page { get; } = new PdfName("Page");
        public static PdfName Pages { get; } = new PdfName("Pages");
        public static PdfName Kids { get; } = new PdfName("Kids");

        public static PdfName MediaBox { get; } = new PdfName("MediaBox");
        public static PdfName CropBox { get; } = new PdfName("CropBox");

        public static PdfName UserUnit { get; } = new PdfName("UserUnit");
        public static PdfName Rotate { get; } = new PdfName("Rotate");

        public static PdfName Matrix { get; } = new PdfName("Matrix");
        public static PdfName BBox { get; } = new PdfName("BBox");

        public static PdfName Contents { get; } = new PdfName("Contents");
        public static PdfName Resources { get; } = new PdfName("Resources");
        public static PdfName ExtGState { get; } = new PdfName("ExtGState");
        public static PdfName Font { get; } = new PdfName("Font");
        public static PdfName Annots { get; } = new PdfName("Annots");
        public static PdfName Link { get; } = new PdfName("Link");
        public static PdfName Rect { get; } = new PdfName("Rect");
        public static PdfName QuadPoints { get; } = new PdfName("QuadPoints");
        public static PdfName A { get; } = new PdfName("A");
        public static PdfName AP { get; } = new PdfName("AP");
        public static PdfName AS { get; } = new PdfName("AS");
        public static PdfName S { get; } = new PdfName("S");
        public static PdfName URI { get; } = new PdfName("URI");
        public static PdfName Popup { get; } = new PdfName("Popup");
        public static PdfName F { get; } = new PdfName("F");
        public static PdfName EF { get; } = new PdfName("EF");
        public static PdfName UF { get; } = new PdfName("UF");
        public static PdfName T { get; } = new PdfName("T");
        public static PdfName FileAttachment { get; } = new PdfName("FileAttachment");
        public static PdfName FS { get; } = new PdfName("FS");

        public static PdfName OCProperties { get; } = new PdfName("OCProperties");
        public static PdfName Properties { get; } = new PdfName("Properties");
        public static PdfName OCGs { get; } = new PdfName("OCGs");
        public static PdfName OCMD { get; } = new PdfName("OCMD");
        public static PdfName VE { get; } = new PdfName("VE");
        public static PdfName AllOn { get; } = new PdfName("AllOn");
        public static PdfName AllOff { get; } = new PdfName("AllOff");
        public static PdfName AnyOn { get; } = new PdfName("AnyOn");
        public static PdfName AnyOff { get; } = new PdfName("AnyOff");
        public static PdfName Not { get; } = new PdfName("Not");
        public static PdfName And { get; } = new PdfName("And");
        public static PdfName Or { get; } = new PdfName("Or");
        public static PdfName Intent { get; } = new PdfName("Intent");
        public static PdfName BaseState { get; } = new PdfName("BaseState");
        public static PdfName Usage { get; } = new PdfName("Usage");
        public static PdfName View { get; } = new PdfName("View");
        public static PdfName ViewState { get; } = new PdfName("ViewState");
        public static PdfName Export { get; } = new PdfName("Export");
        public static PdfName ExportState { get; } = new PdfName("ExportState");
        public static PdfName OFF { get; } = new PdfName("OFF");
        public static PdfName ON { get; } = new PdfName("ON");
        public static PdfName OC { get; } = new PdfName("OC");
        public static PdfName D { get; } = new PdfName("D");

        public static PdfName Encoding { get; } = new PdfName("Encoding");
        public static PdfName BaseEncoding { get; } = new PdfName("BaseEncoding");
        public static PdfName Differences { get; } = new PdfName("Differences");

        public static PdfName CMapName { get; } = new PdfName("CMapName");

        public static PdfName Type0 { get; } = new PdfName("Type0");
        public static PdfName CIDFontType0 { get; } = new PdfName("CIDFontType0");
        public static PdfName CIDFontType2 { get; } = new PdfName("CIDFontType2");
        public static PdfName Type1 { get; } = new PdfName("Type1");
        public static PdfName Type3 { get; } = new PdfName("Type3");
        public static PdfName MMType1 { get; } = new PdfName("MMType1");
        public static PdfName TrueType { get; } = new PdfName("TrueType");
        public static PdfName OpenType { get; } = new PdfName("OpenType");
        public static PdfName BaseFont { get; } = new PdfName("BaseFont");
        public static PdfName ToUnicode { get; } = new PdfName("ToUnicode");
        public static PdfName DescendantFonts { get; } = new PdfName("DescendantFonts");
        public static PdfName FontDescriptor { get; } = new PdfName("FontDescriptor");
        public static PdfName CIDToGIDMap { get; } = new PdfName("CIDToGIDMap");
        public static PdfName CIDSystemInfo { get; } = new PdfName("CIDSystemInfo");
        public static PdfName Registry { get; } = new PdfName("Registry");
        public static PdfName Ordering { get; } = new PdfName("Ordering");
        public static PdfName FontName { get; } = new PdfName("FontName");
        public static PdfName FontFile { get; } = new PdfName("FontFile");
        public static PdfName FontFile2 { get; } = new PdfName("FontFile2");
        public static PdfName FontFile3 { get; } = new PdfName("FontFile3");
        public static PdfName W { get; } = new PdfName("W");
        public static PdfName DW { get; } = new PdfName("DW");
        public static PdfName MissingWidth { get; } = new PdfName("MissingWidth");
        public static PdfName Widths { get; } = new PdfName("Widths");
        public static PdfName FirstChar { get; } = new PdfName("FirstChar");
        public static PdfName LastChar { get; } = new PdfName("LastChar");
        public static PdfName CharProcs { get; } = new PdfName("CharProcs");
        public static PdfName FontMatrix { get; } = new PdfName("FontMatrix");
        public static PdfName FontBBox { get; } = new PdfName("FontBBox");
        public static PdfName Flags { get; } = new PdfName("Flags");

        public static PdfName XObject { get; } = new PdfName("XObject");
        public static PdfName Form { get; } = new PdfName("Form");
        public static PdfName Image { get; } = new PdfName("Image");
        public static PdfName Width { get; } = new PdfName("Width");
        public static PdfName Height { get; } = new PdfName("Height");
        public static PdfName SMask { get; } = new PdfName("SMask");
        public static PdfName Mask { get; } = new PdfName("Mask");
        public static PdfName ImageMask { get; } = new PdfName("ImageMask");
        public static PdfName Group { get; } = new PdfName("Group");
        public static PdfName Transparency { get; } = new PdfName("Transparency");
        public static PdfName Decode { get; } = new PdfName("Decode");
        public static PdfName G { get; } = new PdfName("G");
        public static PdfName Alpha { get; } = new PdfName("Alpha");
        public static PdfName Luminosity { get; } = new PdfName("Luminosity");

        public static PdfName PatternType { get; } = new PdfName("PatternType");
        public static PdfName Shading { get; } = new PdfName("Shading");
        public static PdfName ShadingType { get; } = new PdfName("ShadingType");
        public static PdfName Coords { get; } = new PdfName("Coords");
        public static PdfName Extend { get; } = new PdfName("Extend");
        public static PdfName Background { get; } = new PdfName("Background");
        public static PdfName Function { get; } = new PdfName("Function");
        public static PdfName PaintType { get; } = new PdfName("PaintType");
        public static PdfName XStep { get; } = new PdfName("XStep");
        public static PdfName YStep { get; } = new PdfName("YStep");
        public static PdfName VerticesPerRow { get; } = new PdfName("VerticesPerRow");

        public static PdfName Filter { get; } = new PdfName("Filter");
        public static PdfName Crypt { get; } = new PdfName("Crypt");
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
        public static PdfName Rows { get; } = new PdfName("Rows");
        public static PdfName EncodedByteAlign { get; } = new PdfName("EncodedByteAlign");
        public static PdfName BlackIs1 { get; } = new PdfName("BlackIs1");
        public static PdfName K { get; } = new PdfName("K");
        public static PdfName BitsPerComponent { get; } = new PdfName("BitsPerComponent");
        public static PdfName BitsPerCoordinate { get; } = new PdfName("BitsPerCoordinate");
        public static PdfName BitsPerFlag { get; } = new PdfName("BitsPerFlag");
        public static PdfName Colors { get; } = new PdfName("Colors");
        public static PdfName Interpolate { get; } = new PdfName("Interpolate");

        public static PdfName ColorSpace { get; } = new PdfName("ColorSpace");
        public static PdfName DeviceGray { get; } = new PdfName("DeviceGray");
        public static PdfName DeviceRGB { get; } = new PdfName("DeviceRGB");
        public static PdfName DeviceCMYK { get; } = new PdfName("DeviceCMYK");
        public static PdfName Indexed { get; } = new PdfName("Indexed");
        public static PdfName CalGray { get; } = new PdfName("CalGray");
        public static PdfName CalRGB { get; } = new PdfName("CalRGB");
        public static PdfName Lab { get; } = new PdfName("Lab");
        public static PdfName ICCBased { get; } = new PdfName("ICCBased");
        public static PdfName Separation { get; } = new PdfName("Separation");
        public static PdfName DeviceN { get; } = new PdfName("DeviceN");
        public static PdfName Pattern { get; } = new PdfName("Pattern");

        public static PdfName Alternate { get; } = new PdfName("Alternate");
        public static PdfName N { get; } = new PdfName("N");
        public static PdfName WhitePoint { get; } = new PdfName("WhitePoint");
        public static PdfName BlackPoint { get; } = new PdfName("BlackPoint");
        public static PdfName Gamma { get; } = new PdfName("Gamma");

        public static PdfName FunctionType { get; } = new PdfName("FunctionType");
        public static PdfName Functions { get; } = new PdfName("Functions");
        public static PdfName Domain { get; } = new PdfName("Domain");
        public static PdfName Bounds { get; } = new PdfName("Bounds");
        public static PdfName Encode { get; } = new PdfName("Encode");
        public static PdfName Range { get; } = new PdfName("Range");
        public static PdfName C0 { get; } = new PdfName("C0");
        public static PdfName C1 { get; } = new PdfName("C1");
        public static PdfName Size { get; } = new PdfName("Size");
        public static PdfName BitsPerSample { get; } = new PdfName("BitsPerSample");
        public static PdfName Order { get; } = new PdfName("Order");

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

        public static PdfName BPC { get; } = new PdfName("BPC");
        public static PdfName CS { get; } = new PdfName("CS");
        public static PdfName D { get; } = new PdfName("D");
        public static PdfName DP { get; } = new PdfName("DP");
        public static PdfName F { get; } = new PdfName("F");
        public static PdfName H { get; } = new PdfName("H");
        public static PdfName IM { get; } = new PdfName("IM");
        public static PdfName I { get; } = new PdfName("I");
        public static PdfName W { get; } = new PdfName("W");
        public static PdfName G { get; } = new PdfName("G");
        public static PdfName RGB { get; } = new PdfName("RGB");
        public static PdfName CMYK { get; } = new PdfName("CMYK");
        public static PdfName AHx { get; } = new PdfName("AHx");
        public static PdfName A85 { get; } = new PdfName("A85");
        public static PdfName LZW { get; } = new PdfName("LZW");
        public static PdfName Fl { get; } = new PdfName("Fl");
        public static PdfName RL { get; } = new PdfName("RL");
        public static PdfName CCF { get; } = new PdfName("CCF");
        public static PdfName DCT { get; } = new PdfName("DCT");

        public static PdfName Translate(PdfName input, bool isRootDictionary)
        {
            switch (input.Value)
            {
                case nameof(BPC): return Names.BitsPerComponent;
                case nameof(CS): return Names.ColorSpace;
                case nameof(D): return Names.Decode;
                case nameof(DP): return Names.DecodeParms;
                case nameof(F): return Names.Filter;
                case nameof(H): return Names.Height;
                case nameof(IM): return Names.ImageMask;
                case nameof(I): return isRootDictionary ? Names.Interpolate : Names.Indexed;
                case nameof(W): return Names.Width;
                case nameof(G): return Names.DeviceGray;
                case nameof(RGB): return Names.DeviceRGB;
                case nameof(CMYK): return Names.DeviceCMYK;
                case nameof(AHx): return Names.ASCIIHexDecode;
                case nameof(A85): return Names.ASCII85Decode;
                case nameof(LZW): return Names.LZWDecode;
                case nameof(Fl): return Names.FlateDecode;
                case nameof(RL): return Names.RunLengthDecode;
                case nameof(CCF): return Names.CCITTFaxDecode;
                case nameof(DCT): return Names.DCTDecode;
                default: return input;
            }
        }
    }
}
