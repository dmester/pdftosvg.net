// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Drawing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing.Paths
{
    internal class PathData : IEnumerable<PathCommand>
    {
        private List<PathCommand> commands = new List<PathCommand>();

        public int Count => commands.Count;

        public PathCommand this[int index] => commands[index];

        public void ClosePath()
        {
            if (commands.LastOrDefault() != ClosePathCommand.Value)
            {
                commands.Add(ClosePathCommand.Value);
            }
        }

        public void MoveTo(double x, double y)
        {
            commands.Add(new MoveToCommand(x, y));
        }

        public void LineTo(double x, double y)
        {
            commands.Add(new LineToCommand(x, y));
        }

        public void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            commands.Add(new CurveToCommand(x1, y1, x2, y2, x3, y3));
        }

        public PathData Transform(Matrix matrix)
        {
            var result = new PathData();

            foreach (var command in commands)
            {
                result.commands.Add(command.Transform(matrix));
            }

            return result;
        }

        public PathData Clone()
        {
            return new PathData { commands = commands.ToList() };
        }

        public override string ToString()
        {
            return string.Join(" ", commands);
        }

        public IEnumerator<PathCommand> GetEnumerator()
        {
            return commands.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return commands.GetEnumerator();
        }
    }
}
