// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Cli
{
    internal class ProgressBar
    {
        private readonly int cursorLeft;
        private readonly int cursorTop;

        private readonly int width;

        private int progressPercent;

        public ProgressBar(string label, int width = 34)
        {
            if (width < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            this.width = width;

            if (!Console.IsOutputRedirected)
            {
                Console.Write("{0,-24}", label);

                cursorTop = Console.CursorTop;
                cursorLeft = Console.CursorLeft;

                Update(restoreCursor: false);

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        public int ProgressPercent
        {
            get => progressPercent;
            set
            {
                progressPercent =
                    value <= 0 ? 0 :
                    value >= 100 ? 100 :
                    value;

                Update(restoreCursor: true);
            }
        }

        private void Update(bool restoreCursor)
        {
            if (Console.IsOutputRedirected)
            {
                return;
            }

            var originalLeft = Console.CursorLeft;
            var originalTop = Console.CursorTop;
            var cursorWasVisible = Console.CursorVisible;

            Console.CursorVisible = false;
            Console.SetCursorPosition(cursorLeft, cursorTop);

            Console.Write("[");

            var filledWidth =
                progressPercent == 0 ? 0 :
                progressPercent == 100 ? width :
                1 + (width - 2) * progressPercent / 100;

            var unfilledWidth = width - filledWidth;

            ColoredConsole.WriteError(new string('#', filledWidth), ConsoleColor.Green);

            Console.CursorLeft += unfilledWidth;

            Console.Write("]   {0,3}%  ", progressPercent);

            if (restoreCursor)
            {
                Console.SetCursorPosition(originalLeft, originalTop);
            }

            Console.CursorVisible = cursorWasVisible;
        }
    }
}
