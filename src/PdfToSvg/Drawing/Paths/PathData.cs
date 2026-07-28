// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
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
        private PathCommand[] commands;
        private int count;

        public int Count => count;

        public ref PathCommand this[int index] => ref commands[index];

        public PathData()
        {
            this.commands = new PathCommand[10];
            this.count = 0;
        }

        private PathData(PathCommand[] commands)
        {
            this.commands = commands;
            this.count = commands.Length;
        }

        public void ClosePath()
        {
            if (count == 0 || commands[count - 1].Type != PathCommandType.ClosePath)
            {
                Add(PathCommand.ClosePath);
            }
        }

        public void MoveTo(double x, double y)
        {
            Add(PathCommand.MoveTo(x, y));
        }

        public void LineTo(double x, double y)
        {
            Add(PathCommand.LineTo(x, y));
        }

        public void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (count + 2 >= commands.Length)
            {
                Array.Resize(ref commands, Math.Max(10, commands.Length * 2));
            }

            commands[count + 0] = PathCommand.CurveTo(x3, y3);
            commands[count + 1] = PathCommand.CurveToP1(x1, y1);
            commands[count + 2] = PathCommand.CurveToP2(x2, y2);

            count += 3;
        }

        public void Add(PathCommand command)
        {
            if (count == commands.Length)
            {
                Array.Resize(ref commands, Math.Max(10, commands.Length * 2));
            }

            commands[count++] = command;
        }

        public void Polygon(IEnumerable<Point> points)
        {
            using var enumerator = points.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var multiplePoints = false;

                MoveTo(enumerator.Current.X, enumerator.Current.Y);

                while (enumerator.MoveNext())
                {
                    LineTo(enumerator.Current.X, enumerator.Current.Y);
                    multiplePoints = true;
                }

                if (multiplePoints)
                {
                    ClosePath();
                }
            }
        }

        public void Clear()
        {
            count = 0;
        }

        public void Transform(Matrix matrix)
        {
            for (var i = 0; i < count; i++)
            {
                ref var command = ref commands[i];
                command = command.Transform(matrix);
            }
        }

        public PathData Clone()
        {
            var commandsCopy = new PathCommand[count];
            Array.Copy(this.commands, 0, commandsCopy, 0, count);
            return new PathData(commandsCopy);
        }

        public override string ToString()
        {
            return string.Join(" ", this);
        }

        IEnumerator<PathCommand> IEnumerable<PathCommand>.GetEnumerator()
        {
            for (var i = 0; i < count; i++)
            {
                yield return commands[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (var i = 0; i < count; i++)
            {
                yield return commands[i];
            }
        }
    }
}
