// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal struct JpegBlockEnumerator
    {
        public int ComponentId { get; private set; } = int.MaxValue - 1;
        public JpegComponent Component { get; private set; } = new JpegComponent();

        public int SubSampleX { get; private set; } = int.MaxValue - 1;
        public int SubSampleY { get; private set; } = int.MaxValue - 1;

        public int BlockIndex { get; private set; } = -1;
        public bool ShouldRestart { get; private set; }

        public int RestartInterval { get; }

        private int leftUntilRestart;

        private readonly JpegComponent[] components;


        public JpegBlockEnumerator(JpegComponent[] components, int restartInterval)
        {
            this.components = components;
            RestartInterval = restartInterval;
        }

        public void ResetRestartInterval()
        {
            leftUntilRestart = RestartInterval;
            ShouldRestart = false;
        }

        public void MoveNext()
        {
            ShouldRestart = false;
            BlockIndex++;

            if (++SubSampleX < Component.HorizontalSamplingFactor)
            {
                return;
            }
            SubSampleX = 0;

            if (++SubSampleY < Component.VerticalSamplingFactor)
            {
                return;
            }
            SubSampleY = 0;

            if (++ComponentId < components.Length)
            {
                Component = components[ComponentId];
                return;
            }

            ComponentId = 0;
            Component = components[ComponentId];

            if (RestartInterval > 0 && leftUntilRestart-- <= 0)
            {
                leftUntilRestart += RestartInterval;
                ShouldRestart = true;
            }
        }
    }
}
