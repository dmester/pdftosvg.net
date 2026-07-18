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
        public static TableFactory Factory => new("hmtx", Read);
        public string Tag => "hmtx";

        public LongHorMetricRecord[] HorMetrics = ArrayUtils.Empty<LongHorMetricRecord>();
        public short[] LeftSideBearings = ArrayUtils.Empty<short>();

        void IBaseTable.Write(OpenTypeWriter writer, IList<IBaseTable> _)
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

        private static IBaseTable? Read(OpenTypeReader reader, OpenTypeReaderContext context)
        {
            var numberOfHMetrics = context.ReadTables
                .OfType<HheaTable>()
                .Select(x => x.NumberOfHMetrics)
                .FirstOrDefault();

            var table = new HmtxTable();

            var horMetrics = new LongHorMetricRecord[numberOfHMetrics];

            for (var i = 0; i < horMetrics.Length; i++)
            {
                ref var record = ref horMetrics[i];
                record.AdvanceWidth = reader.ReadUInt16();
                record.LeftSideBearing = reader.ReadInt16();
            }

            table.HorMetrics = horMetrics;
            table.LeftSideBearings = new short[(reader.Length - reader.Position) / 2];

            for (var i = 0; i < table.LeftSideBearings.Length; i++)
            {
                table.LeftSideBearings[i] = reader.ReadInt16();
            }

            return table;
        }
    }

    [DebuggerDisplay("LSB {LeftSideBearing}, AW {AdvanceWidth}")]
    internal struct LongHorMetricRecord
    {
        public ushort AdvanceWidth;
        public short LeftSideBearing;
    }
}
