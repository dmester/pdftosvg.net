// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Drawing.Shadings
{
    internal class FreeFormGouraudShading : MeshShading
    {
        public FreeFormGouraudShading(PdfDictionary definition, CancellationToken cancellationToken) : base(definition, cancellationToken)
        {
            ReadData();
        }

        private void ReadData()
        {
            var prevCoordinates = new Point[3];
            var prevColors = new float[3][];

            while (!reader.EndOfInput)
            {
                var coordinates = new Point[3];
                var colors = new float[3][];

                var flag = reader.ReadBits(bitsPerFlag);
                int keptVertices;

                switch (flag)
                {
                    case 0:
                        keptVertices = 0;
                        break;

                    case 1:
                        coordinates[0] = prevCoordinates[1];
                        coordinates[1] = prevCoordinates[2];
                        colors[0] = prevColors[1];
                        colors[1] = prevColors[2];
                        keptVertices = 2;
                        break;

                    case 2:
                        coordinates[0] = prevCoordinates[0];
                        coordinates[1] = prevCoordinates[2];
                        colors[0] = prevColors[0];
                        colors[1] = prevColors[2];
                        keptVertices = 2;
                        break;

                    case -1:
                        // End of data
                        return;

                    default:
                        Log.WriteLine("Unknown Free-Form Gouraud shading flag " + flag + ".");
                        return;
                }

                for (var i = keptVertices; i < 3; i++)
                {
                    if (i > keptVertices)
                    {
                        reader.ReadBits(bitsPerFlag);
                    }

                    if (!TryReadCoordinate(out coordinates[i]) ||
                        !TryReadColor(out colors[i]))
                    {
                        Log.WriteLine("Incomplete vertice encountered in Free-Form Gouraud shading.");
                        return;
                    }

                    reader.AlignByte();
                }

                patches.Add(new Patch(coordinates, colors));

                prevCoordinates = coordinates;
                prevColors = colors;
            }
        }
    }
}
