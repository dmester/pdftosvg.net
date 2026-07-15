// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace PdfToSvg.Tests.Images.Jpx
{
    /// <summary>
    /// Minimal reader for the PGX image format used by the ITU-T T.803 conformance suite for its reference images.
    /// Header: "PG &lt;ML|LL&gt; &lt;+|-&gt;&lt;bit depth&gt; &lt;width&gt; &lt;height&gt;\n" followed by
    /// one sample per pixel, row-major, big- or little-endian, 1 byte per sample for bit depths 1-8 and 2 bytes per
    /// sample for bit depths 9-16 (signed samples in two's complement).
    /// </summary>
    internal sealed class JpxPgxFile
    {
        public bool BigEndian;
        public bool Signed;
        public int BitDepth;
        public int Width;
        public int Height;

        /// <summary>Raw sample values, row-major, sign-extended when <see cref="Signed"/>.</summary>
        public int[] Samples = new int[0];

        public static JpxPgxFile Parse(byte[] data)
        {
            var headerEnd = Array.IndexOf(data, (byte)'\n');
            if (headerEnd < 0)
            {
                throw new FormatException("Malformed PGX file: missing header line.");
            }

            var header = System.Text.Encoding.ASCII.GetString(data, 0, headerEnd);
            var fields = header.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // fields: "PG", "ML"|"LL", "<sign><depth>", "<width>", "<height>"
            if (fields.Length != 5 || fields[0] != "PG")
            {
                throw new FormatException("Malformed PGX header: " + header);
            }

            var result = new JpxPgxFile
            {
                BigEndian = fields[1] == "ML",
                Width = int.Parse(fields[3], CultureInfo.InvariantCulture),
                Height = int.Parse(fields[4], CultureInfo.InvariantCulture),
            };

            var depthField = fields[2];
            if (depthField[0] == '+' || depthField[0] == '-')
            {
                result.Signed = depthField[0] == '-';
                depthField = depthField.Substring(1);
            }

            result.BitDepth = int.Parse(depthField, CultureInfo.InvariantCulture);

            var bytesPerSample = result.BitDepth <= 8 ? 1 : 2;
            var sampleCount = result.Width * result.Height;
            var expectedBodyLength = sampleCount * bytesPerSample;

            var bodyStart = headerEnd + 1;
            if (data.Length - bodyStart < expectedBodyLength)
            {
                throw new FormatException("Malformed PGX file: body shorter than width * height * bytesPerSample.");
            }

            var samples = new int[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                int value;

                if (bytesPerSample == 1)
                {
                    value = data[bodyStart + i];
                    if (result.Signed && value >= 0x80)
                    {
                        value -= 0x100;
                    }
                }
                else
                {
                    var offset = bodyStart + i * 2;
                    value = result.BigEndian
                        ? (data[offset] << 8) | data[offset + 1]
                        : (data[offset + 1] << 8) | data[offset];

                    if (result.Signed && value >= 0x8000)
                    {
                        value -= 0x10000;
                    }
                }

                samples[i] = value;
            }

            result.Samples = samples;
            return result;
        }
    }
}
