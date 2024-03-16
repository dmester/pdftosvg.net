// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Fonts.OpenType.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    [DebuggerDisplay("OS/2")]
    internal class OS2Table : IBaseTable
    {
        public static TableFactory Factory => new("OS/2", Read);
        public string Tag => "OS/2";

        public ushort Version = 5;

        public short AvgXCharWidth;
        public FontWeight WeightClass = FontWeight.Normal;
        public FontWidth WidthClass = FontWidth.Medium;

        public ushort FsType;

        public UsagePermission UsagePermissions
        {
            get => (UsagePermission)(FsType & 0xf);
            set => FsType = (ushort)((FsType & ~0xf) | ((int)value & 0xf));
        }
        public bool NoSubsetting
        {
            get => (FsType & 0x100) != 0;
            set => FsType = (ushort)((FsType & ~0x100) | (value ? 0x100 : 0));
        }
        public bool BitmapEmbeddingOnly
        {
            get => (FsType & 0x200) != 0;
            set => FsType = (ushort)((FsType & ~0x200) | (value ? 0x200 : 0));
        }

        public short SubscriptXSize;
        public short SubscriptYSize;
        public short SubscriptXOffset;
        public short SubscriptYOffset;
        public short SuperscriptXSize;
        public short SuperscriptYSize;
        public short SuperscriptXOffset;
        public short SuperscriptYOffset;
        public short StrikeoutSize;
        public short StrikeoutPosition;
        public short FamilyClass;
        public byte[] Panose = new byte[10];
        public uint UnicodeRange1;
        public uint UnicodeRange2;
        public uint UnicodeRange3;
        public uint UnicodeRange4;
        public string AchVendID = "UKWN";
        public SelectionFlags Selection;
        public ushort FirstCharIndex;
        public ushort LastCharIndex;
        public short TypoAscender;
        public short TypoDescender;
        public short TypoLineGap;
        public ushort WinAscent;
        public ushort WinDescent;
        public uint CodePageRange1;
        public uint CodePageRange2;
        public short XHeight;
        public short CapHeight;
        public ushort DefaultChar;
        public ushort BreakChar;
        public ushort MaxContext;
        public ushort LowerOpticalPointSize;
        public ushort UpperOpticalPointSize = 0xffff;

        void IBaseTable.Write(OpenTypeWriter writer)
        {
            writer.WriteUInt16(Version);
            writer.WriteInt16(AvgXCharWidth);
            writer.WriteUInt16((ushort)WeightClass);
            writer.WriteUInt16((ushort)WidthClass);
            writer.WriteUInt16(FsType);
            writer.WriteInt16(SubscriptXSize);
            writer.WriteInt16(SubscriptYSize);
            writer.WriteInt16(SubscriptXOffset);
            writer.WriteInt16(SubscriptYOffset);
            writer.WriteInt16(SuperscriptXSize);
            writer.WriteInt16(SuperscriptYSize);
            writer.WriteInt16(SuperscriptXOffset);
            writer.WriteInt16(SuperscriptYOffset);
            writer.WriteInt16(StrikeoutSize);
            writer.WriteInt16(StrikeoutPosition);
            writer.WriteInt16(FamilyClass);
            writer.WritePaddedBytes(Panose, 10);
            writer.WriteUInt32(UnicodeRange1);
            writer.WriteUInt32(UnicodeRange2);
            writer.WriteUInt32(UnicodeRange3);
            writer.WriteUInt32(UnicodeRange4);
            writer.WriteAscii(AchVendID.PadRight(4).Substring(0, 4));
            writer.WriteUInt16((ushort)Selection);
            writer.WriteUInt16(FirstCharIndex);
            writer.WriteUInt16(LastCharIndex);
            writer.WriteInt16(TypoAscender);
            writer.WriteInt16(TypoDescender);
            writer.WriteInt16(TypoLineGap);
            writer.WriteUInt16(WinAscent);
            writer.WriteUInt16(WinDescent);

            if (Version < 1) return;

            writer.WriteUInt32(CodePageRange1);
            writer.WriteUInt32(CodePageRange2);

            if (Version < 2) return;

            writer.WriteInt16(XHeight);
            writer.WriteInt16(CapHeight);
            writer.WriteUInt16(DefaultChar);
            writer.WriteUInt16(BreakChar);
            writer.WriteUInt16(MaxContext);

            if (Version < 5) return;

            writer.WriteUInt16(LowerOpticalPointSize);
            writer.WriteUInt16(UpperOpticalPointSize);
        }

        private static IBaseTable? Read(OpenTypeReader reader)
        {
            var table = new OS2Table();

            table.Version = reader.ReadUInt16();
            table.AvgXCharWidth = reader.ReadInt16();
            table.WeightClass = (FontWeight)reader.ReadUInt16();
            table.WidthClass = (FontWidth)reader.ReadUInt16();
            table.FsType = reader.ReadUInt16();
            table.SubscriptXSize = reader.ReadInt16();
            table.SubscriptYSize = reader.ReadInt16();
            table.SubscriptXOffset = reader.ReadInt16();
            table.SubscriptYOffset = reader.ReadInt16();
            table.SuperscriptXSize = reader.ReadInt16();
            table.SuperscriptYSize = reader.ReadInt16();
            table.SuperscriptXOffset = reader.ReadInt16();
            table.SuperscriptYOffset = reader.ReadInt16();
            table.StrikeoutSize = reader.ReadInt16();
            table.StrikeoutPosition = reader.ReadInt16();
            table.FamilyClass = reader.ReadInt16();
            table.Panose = reader.ReadBytes(10);
            table.UnicodeRange1 = reader.ReadUInt32();
            table.UnicodeRange2 = reader.ReadUInt32();
            table.UnicodeRange3 = reader.ReadUInt32();
            table.UnicodeRange4 = reader.ReadUInt32();
            table.AchVendID = reader.ReadAscii(4);
            table.Selection = (SelectionFlags)reader.ReadUInt16();
            table.FirstCharIndex = reader.ReadUInt16();
            table.LastCharIndex = reader.ReadUInt16();
            table.TypoAscender = reader.ReadInt16();
            table.TypoDescender = reader.ReadInt16();
            table.TypoLineGap = reader.ReadInt16();
            table.WinAscent = reader.ReadUInt16();
            table.WinDescent = reader.ReadUInt16();

            if (table.Version >= 1)
            {
                table.CodePageRange1 = reader.ReadUInt32();
                table.CodePageRange2 = reader.ReadUInt32();

                if (table.Version >= 2)
                {
                    table.XHeight = reader.ReadInt16();
                    table.CapHeight = reader.ReadInt16();
                    table.DefaultChar = reader.ReadUInt16();
                    table.BreakChar = reader.ReadUInt16();
                    table.MaxContext = reader.ReadUInt16();

                    if (table.Version >= 5)
                    {
                        table.LowerOpticalPointSize = reader.ReadUInt16();
                        table.UpperOpticalPointSize = reader.ReadUInt16();
                    }
                }
            }

            return table;
        }
    }
}
