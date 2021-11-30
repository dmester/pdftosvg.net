// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    [DebuggerDisplay("hhea")]
    internal class HheaTable : IBaseTable
    {
        public string Tag => "hhea";

        public short Ascender;
        public short Descender;
        public short LineGap;
        public ushort AdvanceWidthMax;
        public short MinLeftSideBearing;
        public short MinRightSideBearing;
        public short MaxXExtent;
        public short CaretSlopeRise;
        public short CaretSlopeRun;
        public short CaretOffset;
        public short MetricDataFormat;
        public ushort NumberOfHMetrics;

        void IBaseTable.Write(OpenTypeWriter writer)
        {
            writer.WriteUInt16(1);
            writer.WriteUInt16(0);
            writer.WriteInt16(Ascender);
            writer.WriteInt16(Descender);
            writer.WriteInt16(LineGap);
            writer.WriteUInt16(AdvanceWidthMax);
            writer.WriteInt16(MinLeftSideBearing);
            writer.WriteInt16(MinRightSideBearing);
            writer.WriteInt16(MaxXExtent);
            writer.WriteInt16(CaretSlopeRise);
            writer.WriteInt16(CaretSlopeRun);
            writer.WriteInt16(CaretOffset);
            writer.WriteInt16(0);
            writer.WriteInt16(0);
            writer.WriteInt16(0);
            writer.WriteInt16(0);
            writer.WriteInt16(MetricDataFormat);
            writer.WriteUInt16(NumberOfHMetrics);
        }

        [OpenTypeTableReader("hhea")]
        public static IBaseTable? Read(OpenTypeReader reader)
        {
            var table = new HheaTable();

            reader.ReadUInt16(); // majorVersion
            reader.ReadUInt16(); // minorVersion
            table.Ascender = reader.ReadInt16();
            table.Descender = reader.ReadInt16();
            table.LineGap = reader.ReadInt16();
            table.AdvanceWidthMax = reader.ReadUInt16();
            table.MinLeftSideBearing = reader.ReadInt16();
            table.MinRightSideBearing = reader.ReadInt16();
            table.MaxXExtent = reader.ReadInt16();
            table.CaretSlopeRise = reader.ReadInt16();
            table.CaretSlopeRun = reader.ReadInt16();
            table.CaretOffset = reader.ReadInt16();
            reader.ReadInt16();
            reader.ReadInt16();
            reader.ReadInt16();
            reader.ReadInt16();
            table.MetricDataFormat = reader.ReadInt16();
            table.NumberOfHMetrics = reader.ReadUInt16();

            return table;
        }
    }
}
