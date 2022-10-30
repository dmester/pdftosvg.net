// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    [DebuggerDisplay("hmtx")]
    internal class HmtxTable : IBaseTable
    {
        public string Tag => "hmtx";

        public LongHorMetricRecord[] HorMetrics = ArrayUtils.Empty<LongHorMetricRecord>();
        public short[] LeftSideBearings = ArrayUtils.Empty<short>();

        void IBaseTable.Write(OpenTypeWriter writer)
        {
            foreach (var hMetric in HorMetrics)
            {
                writer.WriteUInt16(hMetric.AdvanceWidth);
                writer.WriteInt16(hMetric.LeftSideBearing);
            }

            foreach (var leftSideBearing in LeftSideBearings)
            {
                writer.WriteInt16(leftSideBearing);
            }
        }

        [OpenTypeTableReader("hmtx")]
        public static IBaseTable? Read(OpenTypeReader reader, OpenTypeReaderContext context)
        {
            var numberOfHMetrics = context.ReadTables
                .OfType<HheaTable>()
                .Select(x => x.NumberOfHMetrics)
                .FirstOrDefault();

            var table = new HmtxTable();

            table.HorMetrics = new LongHorMetricRecord[numberOfHMetrics];

            for (var i = 0; i < table.HorMetrics.Length; i++)
            {
                var record = table.HorMetrics[i] = new LongHorMetricRecord();
                record.AdvanceWidth = reader.ReadUInt16();
                record.LeftSideBearing = reader.ReadInt16();
            }

            table.LeftSideBearings = new short[(reader.Length - reader.Position) / 2];

            for (var i = 0; i < table.LeftSideBearings.Length; i++)
            {
                table.LeftSideBearings[i] = reader.ReadInt16();
            }

            return table;
        }
    }

    [DebuggerDisplay("LSB {LeftSideBearing}, AW {AdvanceWidth}")]
    internal class LongHorMetricRecord
    {
        public ushort AdvanceWidth;
        public short LeftSideBearing;
    }
}
