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
    [DebuggerDisplay("maxp")]
    internal class MaxpTableV10 : MaxpTable
    {
        private const uint Version = 0x00010000;

        public ushort MaxPoints;
        public ushort MaxContours;
        public ushort MaxCompositePoints;
        public ushort MaxCompositeContours;
        public ushort MaxZones;
        public ushort MaxTwilightPoints;
        public ushort MaxStorage;
        public ushort MaxFunctionDefs;
        public ushort MaxInstructionDefs;
        public ushort MaxStackElements;
        public ushort MaxSizeOfInstructions;
        public ushort MaxComponentElements;
        public ushort MaxComponentDepth;

        protected override void Write(OpenTypeWriter writer)
        {
            writer.WriteUInt32(Version);
            writer.WriteUInt16(NumGlyphs);
            writer.WriteUInt16(MaxPoints);
            writer.WriteUInt16(MaxContours);
            writer.WriteUInt16(MaxCompositePoints);
            writer.WriteUInt16(MaxCompositeContours);
            writer.WriteUInt16(MaxZones);
            writer.WriteUInt16(MaxTwilightPoints);
            writer.WriteUInt16(MaxStorage);
            writer.WriteUInt16(MaxFunctionDefs);
            writer.WriteUInt16(MaxInstructionDefs);
            writer.WriteUInt16(MaxStackElements);
            writer.WriteUInt16(MaxSizeOfInstructions);
            writer.WriteUInt16(MaxComponentElements);
            writer.WriteUInt16(MaxComponentDepth);
        }

        [OpenTypeTableReader("maxp")]
        public static IBaseTable? Read(OpenTypeReader reader)
        {
            var version = reader.ReadUInt32();
            if (version != Version)
            {
                return null;
            }

            var table = new MaxpTableV10();
            table.NumGlyphs = reader.ReadUInt16();
            table.MaxPoints = reader.ReadUInt16();
            table.MaxContours = reader.ReadUInt16();
            table.MaxCompositePoints = reader.ReadUInt16();
            table.MaxCompositeContours = reader.ReadUInt16();
            table.MaxZones = reader.ReadUInt16();
            table.MaxTwilightPoints = reader.ReadUInt16();
            table.MaxStorage = reader.ReadUInt16();
            table.MaxFunctionDefs = reader.ReadUInt16();
            table.MaxInstructionDefs = reader.ReadUInt16();
            table.MaxStackElements = reader.ReadUInt16();
            table.MaxSizeOfInstructions = reader.ReadUInt16();
            table.MaxComponentElements = reader.ReadUInt16();
            table.MaxComponentDepth = reader.ReadUInt16();

            return table;
        }
    }
}
