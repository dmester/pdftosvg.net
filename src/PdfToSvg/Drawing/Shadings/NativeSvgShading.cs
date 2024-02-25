// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace PdfToSvg.Drawing.Shadings
{
    internal abstract class NativeSvgShading : Shading
    {
        private static readonly XNamespace ns = "http://www.w3.org/2000/svg";

        private delegate void Adder(double offset, double[] color);

        // The shading will leave some room for an additional transparent stop if Extend is false.
        private const double StopOffsetStart = 0.00001;
        private const double StopOffsetEnd = 0.99999;

        public bool ExtendStart { get; }
        public bool ExtendEnd { get; }

        public double DomainStart { get; }
        public double DomainEnd { get; } = 1d;

        public ShadingStop[] Stops { get; }

        public NativeSvgShading(PdfDictionary definition, CancellationToken cancellationToken) : base(definition, cancellationToken)
        {
            // Extend
            var extend = definition.GetArrayOrNull<bool>(Names.Extend);
            if (extend != null && extend.Length >= 2)
            {
                ExtendStart = extend[0];
                ExtendEnd = extend[1];
            }

            // Domain
            var domain = definition.GetArrayOrNull<double>(Names.Domain);
            if (domain != null && domain.Length >= 2)
            {
                DomainStart = domain[0];
                DomainEnd = domain[1];
            }

            // Function
            var functionDefinition = definition.GetDictionaryOrNull(Names.Function);
            var function = Function.Parse(functionDefinition, cancellationToken);

            var stops = new List<ShadingStop>();
            var previousStop = default(ShadingStop);
            var floatColor = new float[ColorSpace.ComponentsPerSample];

            void AddStop(double offset, double[] color)
            {
                for (var j = 0; j < floatColor.Length && j < color.Length; j++)
                {
                    floatColor[j] = (float)color[j];
                }

                var stop = new ShadingStop(offset, new RgbColor(ColorSpace, floatColor));

                if (previousStop.Color != stop.Color || previousStop.Offset != stop.Offset)
                {
                    stops.Add(stop);
                    previousStop = stop;
                }
            }

            if (function != null)
            {
                GetStops(AddStop, function, DomainStart, DomainEnd, StopOffsetStart, StopOffsetEnd);
            }

            Stops = stops.ToArray();
        }

        protected void AddStopElements(XElement gradientEl, bool inPattern)
        {
            if (!ExtendStart)
            {
                // Background is not applicable when using the sh operator according to the spec
                if (Background != null && inPattern)
                {
                    gradientEl.Add(CreateStop(offset: 0, Background.Value, transparent: false));
                }
                else
                {
                    gradientEl.Add(CreateStop(offset: 0, Stops.First().Color, transparent: true));
                }
            }

            foreach (var stop in Stops)
            {
                gradientEl.Add(CreateStop(stop));
            }

            if (!ExtendEnd)
            {
                if (Background != null && inPattern)
                {
                    gradientEl.Add(CreateStop(offset: 0, Background.Value, transparent: false));
                }
                else
                {
                    gradientEl.Add(CreateStop(offset: 1, Stops.Last().Color, transparent: true));
                }
            }
        }

        private static XElement CreateStop(ShadingStop stop) => CreateStop(stop.Offset, stop.Color, false);

        private static XElement CreateStop(double offset, RgbColor color, bool transparent)
        {
            var stopEl = new XElement(ns + "stop");

            stopEl.SetAttributeValue("offset", SvgConversion.FormatCoordinate(offset * 100) + "%");
            stopEl.SetAttributeValue("stop-color", SvgConversion.FormatColor(color));

            if (transparent)
            {
                stopEl.SetAttributeValue("stop-opacity", "0");
            }

            return stopEl;
        }

        private void GetStops(Adder adder, Function function, double domainFrom, double domainTo, double offsetRangeFrom, double offsetRangeTo)
        {
            if (function is ExponentialFunction exponential && exponential.N == 1)
            {
                // For linear functions, it is enough to add two stops

                adder(offsetRangeFrom, exponential.Evaluate(domainFrom));
                adder(offsetRangeTo, exponential.Evaluate(domainTo));
            }
            else if (function is StitchingFunction stitching)
            {
                // Sometimes multiple linear exponential functions are stitched together.
                // Handle each sub-function separately. This produces a more accurate SVG representation.

                var boundMultiplier = (offsetRangeTo - offsetRangeFrom) / (stitching.Domain[1] - stitching.Domain[0]);

                for (var i = 0; i < stitching.Functions.Length; i++)
                {
                    var subFunction = stitching.Functions[i];

                    GetStops(adder, subFunction,
                        domainFrom: stitching.Encode[i * 2],
                        domainTo: stitching.Encode[i * 2 + 1],
                        offsetRangeFrom: i == 0
                            ? offsetRangeFrom
                            : offsetRangeFrom + stitching.Bounds[i - 1] * boundMultiplier,
                        offsetRangeTo: i >= stitching.Bounds.Length
                            ? offsetRangeTo
                            : offsetRangeFrom + stitching.Bounds[i] * boundMultiplier
                        );
                }
            }
            else
            {
                // For other function types

                const int Samples = 15;

                var offset = offsetRangeFrom;
                var deltaOffset = (offsetRangeTo - offsetRangeFrom) / (Samples - 1);

                var domainMultiplier = (domainTo - domainFrom) / (Samples - 1);

                for (var i = 0; i < Samples; i++)
                {
                    var color = function.Evaluate(domainFrom + i * domainMultiplier);

                    adder(offset, color);
                    offset += deltaOffset;
                }
            }
        }
    }
}
