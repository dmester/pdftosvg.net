// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace PdfToSvg.Drawing.Shadings
{
    internal class LatticeFormGouraudShading : MeshShading
    {
        private readonly int verticesPerRow;

        public LatticeFormGouraudShading(PdfDictionary definition, CancellationToken cancellationToken) : base(definition, cancellationToken)
        {
            verticesPerRow = definition.GetValueOrDefault(Names.VerticesPerRow, 2);
            ReadData();
        }

        private void ReadData()
        {
            Point[]? prevRow = null;
            float[][]? prevColors = null;

            while (!reader.EndOfInput)
            {
                var row = new Point[verticesPerRow];
                var colors = new float[verticesPerRow][];

                for (var i = 0; i < row.Length; i++)
                {
                    if (!TryReadCoordinate(out row[i]) ||
                        !TryReadColor(out colors[i]))
                    {
                        Log.WriteLine("Incomplete row encountered in Lattice-Form Gouraud shading.");
                        return;
                    }
                }

                if (prevRow != null && prevColors != null)
                {
                    for (var i = 1; i < row.Length; i++)
                    {
                        patches.Add(new Patch(new[]
                        {
                            row[i - 1],
                            prevRow[i - 1],
                            prevRow[i],
                            row[i],
                        }, new[]
                        {
                            colors[i - 1],
                            prevColors[i - 1],
                            prevColors[i],
                            colors[i],
                        }));
                    }
                }

                prevRow = row;
                prevColors = colors;
            }
        }
    }
}
