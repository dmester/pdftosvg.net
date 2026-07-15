// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;

namespace PdfToSvg.Imaging.Jpx.Packets
{
    internal readonly struct JpxPacketKey(int component, int resolution, int precinct, int layer) :
        IEquatable<JpxPacketKey>
    {
        public readonly int Component = component;

        public readonly int Resolution = resolution;

        /// <summary>Precinct index in raster order within the resolution level (Section B.6).</summary>
        public readonly int Precinct = precinct;

        public readonly int Layer = layer;

        public bool Equals(JpxPacketKey other) =>
            Component == other.Component &&
            Resolution == other.Resolution &&
            Precinct == other.Precinct &&
            Layer == other.Layer;

        public override bool Equals(object? obj) => obj is JpxPacketKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Component;
                hash = hash * 397 + Resolution;
                hash = hash * 397 + Precinct;
                hash = hash * 397 + Layer;
                return hash;
            }
        }

        public override string ToString() =>
            "(c=" + Component + ", r=" + Resolution + ", p=" + Precinct + ", l=" + Layer + ")";
    }
}
