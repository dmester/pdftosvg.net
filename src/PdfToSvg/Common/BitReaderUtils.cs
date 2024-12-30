// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Common
{
    internal static class BitReaderUtils
    {
#if DEBUG
        public static string FormatDebugView(ArraySegment<byte> buffer, int byteCursor, int bitCursor)
        {
            if (buffer.Array == null)
            {
                return "";
            }
            else
            {
                return FormatDebugView(buffer,
                    byteCursor, bitCursor,
                    byteCursor < buffer.Count ? buffer.Array[buffer.Offset + byteCursor] : 0);
            }
        }

        public static string FormatDebugView(ArraySegment<byte> buffer, int byteCursor, int bitCursor, int currentByte)
        {
            var result = byteCursor + ":" + bitCursor + " ";

            string Format(int cursorOffset)
            {
                var byteValue = cursorOffset == 0
                    ? currentByte
                    : buffer.Array[buffer.Offset + byteCursor + cursorOffset];

                return Convert
                    .ToString(byteValue, 2)
                    .PadLeft(8, '0');
            }

            if (buffer.Array != null)
            {
                if (byteCursor < buffer.Count)
                {
                    result += byteCursor < 2 ? "|- " : "... ";

                    if (byteCursor > 0)
                    {
                        result += Format(-1) + " ";
                    }

                    var num = Format(0);

                    result += num.Substring(0, bitCursor) + "\u1401" + num.Substring(bitCursor);

                    if (byteCursor + 1 < buffer.Count)
                    {
                        result += " " + Format(+1);

                        if (byteCursor + 2 < buffer.Count)
                        {
                            result += " ...";
                        }
                        else
                        {
                            result += " -|";
                        }
                    }
                    else
                    {
                        result += " -|";
                    }
                }
                else
                {
                    result = "-|";
                }
            }

            return result;
        }
#endif
    }
}
