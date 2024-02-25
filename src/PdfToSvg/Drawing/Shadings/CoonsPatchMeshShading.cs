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
    internal class CoonsPatchMeshShading : MeshShading
    {
        public CoonsPatchMeshShading(PdfDictionary definition, CancellationToken cancellationToken) : base(definition, cancellationToken)
        {
            ReadData();
        }

        private void ReadData()
        {
            var prevCoordinates = new Point[12];
            var prevColors = new float[4][];

            while (!reader.EndOfInput)
            {
                var coordinates = new Point[12];
                var colors = new float[4][];

                var flag = reader.ReadBits(bitsPerFlag);

                int keptCoordinates, keptColors;

                switch (flag)
                {
                    case 0:
                        keptCoordinates = 0;
                        keptColors = 0;
                        break;

                    case 1:
                        coordinates[0] = prevCoordinates[3];
                        coordinates[1] = prevCoordinates[4];
                        coordinates[2] = prevCoordinates[5];
                        coordinates[3] = prevCoordinates[6];
                        keptCoordinates = 4;

                        colors[0] = prevColors[1];
                        colors[1] = prevColors[2];
                        keptColors = 2;
                        break;

                    case 2:
                        coordinates[0] = prevCoordinates[6];
                        coordinates[1] = prevCoordinates[7];
                        coordinates[2] = prevCoordinates[8];
                        coordinates[3] = prevCoordinates[9];
                        keptCoordinates = 4;

                        colors[0] = prevColors[2];
                        colors[1] = prevColors[3];
                        keptColors = 2;
                        break;

                    case 3:
                        coordinates[0] = prevCoordinates[9];
                        coordinates[1] = prevCoordinates[10];
                        coordinates[2] = prevCoordinates[11];
                        coordinates[3] = prevCoordinates[0];
                        keptCoordinates = 4;

                        colors[0] = prevColors[3];
                        colors[1] = prevColors[0];
                        keptColors = 2;
                        break;

                    case -1:
                        // End of data
                        return;

                    default:
                        Log.WriteLine("Unknown Coons Patch flag " + flag + ".");
                        return;
                }

                for (var i = keptCoordinates; i < 12; i++)
                {
                    if (!TryReadCoordinate(out coordinates[i]))
                    {
                        Log.WriteLine("Incomplete coordinate in Coons Patch Mesh shading.");
                        return;
                    }
                }

                for (var c = keptColors; c < 4; c++)
                {
                    if (!TryReadColor(out colors[c]))
                    {
                        Log.WriteLine("Incomplete color in Coons Patch Mesh shading.");
                        return;
                    }
                }

                patches.Add(new Patch(coordinates, colors));

                prevCoordinates = coordinates;
                prevColors = colors;

                // PDF.js and Pdfium don't do this, but Adobe does. The spec is a bit unclear, but refers to shading 4,
                // which states the entries should be padded to byte boundaries.
                reader.AlignByte();
            }
        }
    }
}
