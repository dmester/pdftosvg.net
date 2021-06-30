// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Drawing.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal static class PathConverter
    {
        public static bool TryConvertToRectangle(PathData data, out Rectangle result)
        {
            result = default(Rectangle);

            if (data.Count >= 4 || data.Count <= 6)
            {
                if (data[0] is MoveToCommand move)
                {
                    var lastX = move.X;
                    var lastY = move.Y;
                    var otherX = double.NaN;
                    var otherY = double.NaN;

                    for (var i = 1; i < data.Count; i++)
                    {
                        if (data[i] is LineToCommand lineTo)
                        {
                            if (lineTo.X == lastX)
                            {
                                if (lineTo.Y != lastY && (double.IsNaN(otherY) || otherY == lineTo.Y))
                                {
                                    otherY = lastY;
                                    lastY = lineTo.Y;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else if (lineTo.Y == lastY)
                            {
                                if (lineTo.X != lastX && (double.IsNaN(otherX) || otherX == lineTo.X))
                                {
                                    otherX = lastX;
                                    lastX = lineTo.X;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else if (data[i] is ClosePathCommand && i + 1 == data.Count)
                        {
                            // OK
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (!double.IsNaN(otherX) && !double.IsNaN(otherY))
                    {
                        var x1 = Math.Min(lastX, otherX);
                        var x2 = Math.Max(lastX, otherX);

                        var y1 = Math.Min(lastY, otherY);
                        var y2 = Math.Max(lastY, otherY);

                        result = new Rectangle(x1, y1, x2, y2);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
