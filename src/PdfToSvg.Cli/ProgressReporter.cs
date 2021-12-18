// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Cli
{
    internal abstract class ProgressReporter
    {
        private int progressPercent;

        public int ProgressPercent
        {
            get => progressPercent;
            set
            {
                progressPercent =
                    value <= 0 ? 0 :
                    value >= 100 ? 100 :
                    value;

                OnProgressPercentChanged();
            }
        }

        protected virtual void OnProgressPercentChanged()
        {
        }

        public static ProgressReporter CreateNullReporter() => new NullReporter();

        public static ProgressReporter CreateCliProgressBar(string label, int width = 34) => new CliProgressBar(label, width);

        private class NullReporter : ProgressReporter { }

        private class CliProgressBar : ProgressReporter
        {
            private readonly int cursorLeft;
            private readonly int cursorTop;

            private readonly int width;

            public CliProgressBar(string label, int width)
            {
                if (width < 4)
                {
                    throw new ArgumentOutOfRangeException(nameof(width));
                }

                this.width = width;

                Console.Write("{0,-24}", label);

                cursorTop = Console.CursorTop;
                cursorLeft = Console.CursorLeft;

                Update(restoreCursor: false);
            }

            protected override void OnProgressPercentChanged()
            {
                Update(restoreCursor: true);
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
}
