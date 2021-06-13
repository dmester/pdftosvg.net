// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApproximateCmyk
{
    internal static class Program
    {
        private static readonly object lockObject = new();

        public static void Main()
        {
            var samples = new List<Sample>();

            Sample whiteSample = null;
            Sample blackSample = null;

            // Source files can be found here: https://www.color.org/chardata/CGATS_TR_005.xalter
            foreach (var line in File.ReadLines(@"TR003_Char_Data.csv"))
            {
                var fields = line.Split(',');
                var sample = new Sample();

                if (fields.Length > 7 &&
                    float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out sample.CmykC) &&
                    float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out sample.CmykM) &&
                    float.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out sample.CmykY) &&
                    float.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out sample.CmykK) &&
                    float.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out sample.XyzX) &&
                    float.TryParse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture, out sample.XyzY) &&
                    float.TryParse(fields[7], NumberStyles.Float, CultureInfo.InvariantCulture, out sample.XyzZ))
                {
                    samples.Add(sample);

                    if (sample.CmykC == 0 && sample.CmykM == 0 && sample.CmykY == 0 && sample.CmykK == 0)
                    {
                        whiteSample = sample;
                    }

                    if (sample.CmykC == 100 && sample.CmykM == 100 && sample.CmykY == 100 && sample.CmykK == 100)
                    {
                        blackSample = sample;
                    }
                }
            }

            var scaleX = 0.9642f / whiteSample.XyzX;
            var scaleY = 1f / whiteSample.XyzY;
            var scaleZ = 0.8251f / whiteSample.XyzZ;

            var srcMinX = blackSample.XyzX;
            var srcMinY = blackSample.XyzY;
            var srcMinZ = blackSample.XyzZ;

            var srcMaxX = whiteSample.XyzX;
            var srcMaxY = whiteSample.XyzY;
            var srcMaxZ = whiteSample.XyzZ;

            // Rescale
            foreach (var sample in samples)
            {
                // Change CMYK range to [0, 1]
                sample.CmykC *= 0.01f;
                sample.CmykM *= 0.01f;
                sample.CmykY *= 0.01f;
                sample.CmykK *= 0.01f;

                // Scale XYZ
                sample.XyzX = Interpolate(sample.XyzX, srcMinY, srcMaxY, 0f, 1f);
                sample.XyzY = Interpolate(sample.XyzY, srcMinY, srcMaxY, 0f, 1f);
                sample.XyzZ = Interpolate(sample.XyzZ, srcMinY, srcMaxY, 0f, 1f);

                XyzToSRgb(
                    sample.XyzX, sample.XyzY, sample.XyzZ,
                    out sample.RgbR, out sample.RgbG, out sample.RgbB);
            }

            var rgbs = new[]
            {
                new { Name = "Red", Accessor = new Func<Sample, float>(x => x.RgbR) },
                new { Name = "Green", Accessor = new Func<Sample, float>(x => x.RgbG) },
                new { Name = "Blue", Accessor = new Func<Sample, float>(x => x.RgbB) },
            };

            Parallel.ForEach(rgbs, rgb =>
            {
                Approximate(rgb.Name, samples, rgb.Accessor);
            });
        }

        private class Sample
        {
            public float CmykC;
            public float CmykM;
            public float CmykY;
            public float CmykK;

            public float XyzX;
            public float XyzY;
            public float XyzZ;

            public float RgbR;
            public float RgbG;
            public float RgbB;

            public override string ToString()
            {
                return $"CMYK ( {CmykC:0.00} {CmykM:0.00} {CmykY:0.00} {CmykK:0.00} ) RGB ( {RgbR:0.00} {RgbG:0.00} {RgbB:0.00} )";
            }
        }

        private class Approximation
        {
            public float[] Multipliers;

            public float Min;
            public float Q1;
            public float Q2;
            public float Q3;
            public float Max;

            public float ComparableDiff =>
                Math.Abs(Min) +
                Math.Abs(Q1 * 4) +
                Math.Abs(Q2 * 8) +
                Math.Abs(Q3 * 4) +
                Math.Abs(Max);
        }

        private static float ComputeComponent(Sample sample, float[] multipliers)
        {
            return
                multipliers[0] +

                sample.CmykC * (
                    multipliers[1] +
                    sample.CmykC * (multipliers[5] + sample.CmykC * multipliers[15]) +
                    sample.CmykM * multipliers[6] +
                    sample.CmykY * multipliers[7] +
                    sample.CmykK * multipliers[8]
                ) +

                sample.CmykM * (
                    multipliers[2] +
                    sample.CmykM * (multipliers[9] + sample.CmykM * multipliers[16]) +
                    sample.CmykY * multipliers[10] +
                    sample.CmykK * multipliers[11]
                ) +

                sample.CmykY * (
                    multipliers[3] +
                    sample.CmykY * (multipliers[12] + sample.CmykY * multipliers[17]) +
                    sample.CmykK * multipliers[13]
                ) +

                sample.CmykK * (
                    multipliers[4] +
                    sample.CmykK * (multipliers[14] + sample.CmykK * multipliers[18])
                );
        }

        private static Approximation Approximate(string name, IList<Sample> samples, Func<Sample, float> rgb)
        {
            var multipliers = new float[25];

            multipliers[0] = 1f;

            // First get an initial estimation of each component individually
            for (var i = 1; i < 5; i++)
            {
                var copy = new float[multipliers.Length];

                var minSqDiff = double.MaxValue;
                var bestMultiplier = 0f;

                for (var probeValue = -10f; probeValue < 10f; probeValue += 0.02f)
                {
                    var sqDiff = 0.0;

                    copy[i] = probeValue;

                    foreach (var sample in samples)
                    {
                        var rgbValue = rgb(sample);
                        var computedRgbValue = ComputeComponent(sample, copy);

                        sqDiff += (computedRgbValue - rgbValue) * (computedRgbValue - rgbValue);
                    }

                    if (sqDiff < minSqDiff)
                    {
                        minSqDiff = sqDiff;
                        bestMultiplier = probeValue;
                    }
                }

                multipliers[i] = bestMultiplier;
            }

            Approximation bestResult = null;
            var random = new Random();

            for (var iteration = 0; iteration < 19; iteration++)
            {
                // Add random value to check if we can get better values to work with
                if (iteration == 12 ||
                    iteration == 14 ||
                    iteration == 16)
                {
                    for (var i = 1; i < multipliers.Length; i++)
                    {
                        multipliers[i] += (float)(random.NextDouble() * 0.08 - 0.04);
                    }
                }

                // The first iterations will try to stabilize the constant and first degree multipliers.
                var checkLength =
                    iteration < 4 ? 5 :
                    iteration < 7 ? 15 :
                    multipliers.Length;

                for (var i = 1; i < checkLength; i++)
                {
                    var originalMultiplier = multipliers[i];
                    var minSqDiff = double.MaxValue;
                    var bestMultiplier = 0f;

                    for (var probeDiff = -1.0; probeDiff < 1.0; probeDiff += 0.0001)
                    {
                        var sqDiff = 0.0;
                        var probeValue = (float)(originalMultiplier + probeDiff);
                        multipliers[i] = probeValue;

                        for (var sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
                        {
                            var sample = samples[sampleIndex];

                            var rgbValue = (double)Clamp(rgb(sample), 0f, 1f);
                            if (sample.CmykK == 1)
                            {
                                // Black
                                rgbValue = 0;
                            }

                            var component = (double)Clamp(ComputeComponent(sample, multipliers), 0f, 1f);

                            var diff = (component - rgbValue) * (component - rgbValue);

                            if (sample.CmykC == 0 && sample.CmykM == 0 && sample.CmykY == 0 && sample.CmykK == 0)
                            {
                                // White
                                diff *= 20;
                            }
                            else if (sample.CmykK == 1)
                            {
                                // Black
                                diff *= 3;
                            }

                            sqDiff += diff;
                        }

                        if (sqDiff < minSqDiff)
                        {
                            minSqDiff = sqDiff;
                            bestMultiplier = probeValue;
                        }
                    }

                    multipliers[i] = bestMultiplier;
                }

                var diffs = samples
                    .Select(sample => ComputeComponent(sample, multipliers) - rgb(sample))
                    .OrderBy(d => d)
                    .ToList();

                var resultThisIteration = new Approximation
                {
                    Multipliers = (float[])multipliers.Clone(),
                    Min = diffs[0],
                    Q1 = diffs[diffs.Count / 4],
                    Q2 = diffs[diffs.Count / 2],
                    Q3 = diffs[diffs.Count * 3 / 4],
                    Max = diffs[^1],
                };

                if (bestResult == null ||
                    bestResult.ComparableDiff > resultThisIteration.ComparableDiff)
                {
                    bestResult = resultThisIteration;
                }

                lock (lockObject)
                {
                    Console.WriteLine("{0} {1:00} Score: {2} {3}",
                        name[0],
                        iteration,
                        bestResult.ComparableDiff,
                        iteration == 12 || iteration == 14 || iteration == 16 ? "(randomized)" : "");
                }
            }

            lock (lockObject)
            {
                Console.WriteLine("==========================");
                Console.WriteLine(name);
                Console.WriteLine("Equation:");

                Console.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "    {0:0.####}f +\r\n" +

                    "    c * (\r\n" +
                    "        {1:0.####}f + \r\n" +
                    "        c * ({5:0.####}f + c * {15:0.####}f) +\r\n" +
                    "        m * {6:0.####}f +\r\n" +
                    "        y * {7:0.####}f +\r\n" +
                    "        k * {8:0.####}f\r\n" +
                    "    ) +\r\n" +

                    "    m * (\r\n" +
                    "        {2:0.####}f + \r\n" +
                    "        m * ({9:0.####}f + m * {16:0.####}f) +\r\n" +
                    "        y * {10:0.####}f +\r\n" +
                    "        k * {11:0.####}f\r\n" +
                    "    ) +\r\n" +

                    "    y * (\r\n" +
                    "        {3:0.####}f +\r\n" +
                    "        y * ({12:0.####}f + y * {17:0.####}f) +\r\n" +
                    "        k * {13:0.####}f\r\n" +
                    "    ) +\r\n" +

                    "    k * (\r\n" +
                    "        {4:0.####}f + \r\n" +
                    "        k * ({14:0.####}f + k * {18:0.####}f)\r\n" +
                    "    )",

                    bestResult.Multipliers.Cast<object>().ToArray()
                    ));

                Console.WriteLine("Min: {0}", bestResult.Min);
                Console.WriteLine("Q1: {0}", bestResult.Q1);
                Console.WriteLine("Q2: {0}", bestResult.Q2);
                Console.WriteLine("Q3: {0}", bestResult.Q3);
                Console.WriteLine("Max: {0}", bestResult.Max);
                Console.WriteLine("==========================");
            }

            return bestResult;
        }

        private static float LinearRgbToSRgb(float v)
        {
            return v <= 0.0031308f ? 12.92f * v : (float)(1.055 * Math.Pow(v, 1 / 2.4) - 0.055);
        }

        private static void XyzToSRgb(float x, float y, float z, out float r, out float g, out float b)
        {
            // https://en.wikipedia.org/wiki/SRGB#The_forward_transformation_(CIE_XYZ_to_sRGB)
            // http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html
            r = LinearRgbToSRgb(+3.1338561f * x - 1.6168667f * y - 0.4906146f * z);
            g = LinearRgbToSRgb(-0.9787684f * x + 1.9161415f * y + 0.0334540f * z);
            b = LinearRgbToSRgb(+0.0719453f * x - 0.2289914f * y + 1.4052427f * z);
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v <= min) return min;
            if (v >= max) return max;
            return v;
        }

        private static float Interpolate(float value, float srcMin, float srcMax, float dstMin, float dstMax)
        {
            return dstMin + (dstMax - dstMin) * (value - srcMin) / (srcMax - srcMin);
        }
    }
}
