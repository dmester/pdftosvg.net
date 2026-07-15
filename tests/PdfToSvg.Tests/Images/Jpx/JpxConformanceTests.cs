// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;
using System.IO;

namespace PdfToSvg.Tests.Images.Jpx
{
    // Decodes ITU-T T.803 (02/2024) conformance codestreams through JpxDecoder and compares the result against the
    // suite's PGX reference images, using the Class 1 tolerances from Tables C.6 (Profile-0) and C.7 (Profile-1) of
    // T.803
    [Parallelizable(ParallelScope.Children)]
    internal class JpxConformanceTests
    {
        [Test]
        public void Conformance_P0_01_ExactMatch()
        {
            AssertExactMatch(
                "codestreams_profile0/p0_01.j2k", 0,
                "reference_class1_profile0/c1p0_01-0.pgx");
        }

        // The PGX reference uses the component's native sample grid, while GetComponentIndices resamples to the image
        // grid. The expected pixels replicate the reference samples using the mapping from ITU-T T.800 (06/2019)
        // Section B.2, clamping pixels before the first sample to that sample.
        [Test]
        public void Conformance_P0_02_SubsampledResampledExactMatch()
        {
            var data = ReadConformanceFile("codestreams_profile0/p0_02.j2k");

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(data, 0, data.Length);

            var actual = decoder.GetComponentIndices(0);

            var image = new JpxImageInfo();
            new JpxCodestreamReader(new ArraySegment<byte>(data)).ReadMainHeader(image);
            var component = image.Components[0];

            Assert.IsTrue(component.XRsizi > 1, "p0_02 is expected to be subsampled");

            var reference = ReadPgx("reference_class1_profile0/c1p0_02-0.pgx");
            var width = image.Xsiz - image.XOsiz;
            var height = image.Ysiz - image.YOsiz;
            var planeX0 = MathUtils.CeilDiv(image.XOsiz, component.XRsizi);
            var planeY0 = MathUtils.CeilDiv(image.YOsiz, component.YRsizi);

            Assert.AreEqual(width * height, actual.Length);

            var expected = new byte[width * height];
            for (var y = 0; y < height; y++)
            {
                var planeY = Math.Max((image.YOsiz + y) / component.YRsizi - planeY0, 0);
                for (var x = 0; x < width; x++)
                {
                    var planeX = Math.Max((image.XOsiz + x) / component.XRsizi - planeX0, 0);
                    expected[y * width + x] = (byte)reference.Samples[planeY * reference.Width + planeX];
                }
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Conformance_P0_02_SubsampledExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_02.j2k", 0,
                "reference_class1_profile0/c1p0_02-0.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        [Test]
        public void Conformance_P0_03_SignedExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_03.j2k", 0,
                "reference_class1_profile0/c1p0_03-0.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        [Test]
        public void Conformance_P0_04_WithinLossyTolerance()
        {
            AssertConformance(
                "codestreams_profile0/p0_04.j2k", 0, "reference_class1_profile0/c1p0_04-0.pgx",
                maxPeakAbsoluteError: 5, maxMeanSquaredError: 0.776);
            AssertConformance(
                "codestreams_profile0/p0_04.j2k", 1, "reference_class1_profile0/c1p0_04-1.pgx",
                maxPeakAbsoluteError: 4, maxMeanSquaredError: 0.626);
            AssertConformance(
                "codestreams_profile0/p0_04.j2k", 2, "reference_class1_profile0/c1p0_04-2.pgx",
                maxPeakAbsoluteError: 6, maxMeanSquaredError: 1.070);
        }

        // Component 3 overrides the default irreversible 9-7 transform with reversible 5-3 coding and must match
        // exactly; components 0-2 use 9-7 coding and the lossy tolerances from ITU-T T.803 Table C.6.
        [Test]
        public void Conformance_P0_05_WithinLossyTolerance()
        {
            AssertConformance(
                "codestreams_profile0/p0_05.j2k", 0, "reference_class1_profile0/c1p0_05-0.pgx",
                maxPeakAbsoluteError: 2, maxMeanSquaredError: 0.319);
            AssertConformance(
                "codestreams_profile0/p0_05.j2k", 1, "reference_class1_profile0/c1p0_05-1.pgx",
                maxPeakAbsoluteError: 2, maxMeanSquaredError: 0.323);
            AssertConformance(
                "codestreams_profile0/p0_05.j2k", 2, "reference_class1_profile0/c1p0_05-2.pgx",
                maxPeakAbsoluteError: 2, maxMeanSquaredError: 0.317);
            AssertConformance(
                "codestreams_profile0/p0_05.j2k", 3, "reference_class1_profile0/c1p0_05-3.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        // Component 0 has RGN markers in both the main and tile-part headers, exercising the tile-part precedence of
        // ITU-T T.800 (06/2019) Section A.6.3. The unusually large Table C.6 tolerances are expected for this heavily
        // quantized ROI codestream and were cross-checked against an independent OpenJPEG decode.
        [Test]
        public void Conformance_P0_06_WithinLossyTolerance()
        {
            AssertConformance(
                "codestreams_profile0/p0_06.j2k", 0, "reference_class1_profile0/c1p0_06-0.pgx",
                maxPeakAbsoluteError: 635, maxMeanSquaredError: 11287);
            AssertConformance(
                "codestreams_profile0/p0_06.j2k", 1, "reference_class1_profile0/c1p0_06-1.pgx",
                maxPeakAbsoluteError: 403, maxMeanSquaredError: 6124);
            AssertConformance(
                "codestreams_profile0/p0_06.j2k", 2, "reference_class1_profile0/c1p0_06-2.pgx",
                maxPeakAbsoluteError: 378, maxMeanSquaredError: 3968);
            AssertConformance(
                "codestreams_profile0/p0_06.j2k", 3, "reference_class1_profile0/c1p0_06-3.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        // POC may occur in any tile-part header and every occurrence contributes additional progression order changes;
        // later POC marker segments must extend rather than replace the list
        // (ITU-T T.800 (06/2019) Sections A.6.6 and B.12.3).
        [Test]
        public void Conformance_P0_07_MultiTilePartPocExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_07.j2k",
                [
                    "reference_class1_profile0/c1p0_07-0.pgx",
                    "reference_class1_profile0/c1p0_07-1.pgx",
                    "reference_class1_profile0/c1p0_07-2.pgx",
                ],
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        // Unlike the other class-1 references, these PGX files use resolution reduction 1. Their 257x1536 dimensions
        // match ITU-T T.800 (06/2019) Section B.5 for the 513x3072 image with its highest resolution level discarded.
        [Test]
        public void Conformance_P0_08_ExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_08.j2k",
                [
                    "reference_class1_profile0/c1p0_08-0.pgx",
                    "reference_class1_profile0/c1p0_08-1.pgx",
                    "reference_class1_profile0/c1p0_08-2.pgx",
                ],
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0, maxResolution: 1536);
        }

        // Despite the irreversible 9-7 transform, its floating-point reconstruction noise rounds to the exact same
        // 8-bit samples, matching the zero tolerance specified in ITU-T T.803 Table C.6.
        [Test]
        public void Conformance_P0_09_ExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_09.j2k", 0,
                "reference_class1_profile0/c1p0_09-0.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        [Test]
        public void Conformance_P0_10_SubsampledExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_10.j2k",
                [
                    "reference_class1_profile0/c1p0_10-0.pgx",
                    "reference_class1_profile0/c1p0_10-1.pgx",
                    "reference_class1_profile0/c1p0_10-2.pgx",
                ],
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        [Test]
        public void Conformance_P0_11_ExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_11.j2k", 0,
                "reference_class1_profile0/c1p0_11-0.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        [Test]
        public void Conformance_P0_12_ExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_12.j2k", 0,
                "reference_class1_profile0/c1p0_12-0.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        [Test]
        public void Conformance_P0_13_ManyComponentsExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_13.j2k",
                [
                    "reference_class1_profile0/c1p0_13-0.pgx",
                    "reference_class1_profile0/c1p0_13-1.pgx",
                    "reference_class1_profile0/c1p0_13-2.pgx",
                    "reference_class1_profile0/c1p0_13-3.pgx",
                ],
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        [Test]
        public void Conformance_P0_14_ExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_14.j2k",
                [
                    "reference_class1_profile0/c1p0_14-0.pgx",
                    "reference_class1_profile0/c1p0_14-1.pgx",
                    "reference_class1_profile0/c1p0_14-2.pgx",
                ],
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        [Test]
        public void Conformance_P0_15_SignedExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_15.j2k", 0,
                "reference_class1_profile0/c1p0_15-0.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        // JpxDecoder decodes all three quality layers, so the full-quality reference applies.
        [Test]
        public void Conformance_P0_16_ExactMatch()
        {
            AssertConformance(
                "codestreams_profile0/p0_16.j2k", 0,
                "reference_class1_profile0/c1p0_16-0.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        [Test]
        public void Conformance_P1_01_SubsampledExactMatch()
        {
            AssertConformance(
                "codestreams_profile1/p1_01.j2k", 0,
                "reference_class1_profile1/c1p1_01-0.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        [Test]
        public void Conformance_P1_02_WithinLossyTolerance()
        {
            AssertConformance(
                "codestreams_profile1/p1_02.j2k", 0, "reference_class1_profile1/c1p1_02-0.pgx",
                maxPeakAbsoluteError: 5, maxMeanSquaredError: 0.765);
            AssertConformance(
                "codestreams_profile1/p1_02.j2k", 1, "reference_class1_profile1/c1p1_02-1.pgx",
                maxPeakAbsoluteError: 4, maxMeanSquaredError: 0.616);
            AssertConformance(
                "codestreams_profile1/p1_02.j2k", 2, "reference_class1_profile1/c1p1_02-2.pgx",
                maxPeakAbsoluteError: 6, maxMeanSquaredError: 1.051);
        }

        [Test]
        public void Conformance_P1_03_WithinLossyTolerance()
        {
            AssertConformance(
                "codestreams_profile1/p1_03.j2k", 0, "reference_class1_profile1/c1p1_03-0.pgx",
                maxPeakAbsoluteError: 2, maxMeanSquaredError: 0.311);
            AssertConformance(
                "codestreams_profile1/p1_03.j2k", 1, "reference_class1_profile1/c1p1_03-1.pgx",
                maxPeakAbsoluteError: 2, maxMeanSquaredError: 0.280);
            AssertConformance(
                "codestreams_profile1/p1_03.j2k", 2, "reference_class1_profile1/c1p1_03-2.pgx",
                maxPeakAbsoluteError: 1, maxMeanSquaredError: 0.267);
            AssertConformance(
                "codestreams_profile1/p1_03.j2k", 3, "reference_class1_profile1/c1p1_03-3.pgx",
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        // Every tile-part carries its own QCD. The unusually large Table C.7 tolerance was cross-checked against an
        // independent OpenJPEG decode.
        [Test]
        public void Conformance_P1_04_WithinLossyTolerance()
        {
            AssertConformance(
                "codestreams_profile1/p1_04.j2k", 0, "reference_class1_profile1/c1p1_04-0.pgx",
                maxPeakAbsoluteError: 624, maxMeanSquaredError: 3080);
        }

        // P1_05 and P1_06 exercise the one-sample reconstruction rule of ITU-T T.800 (06/2019) Section F.3.6: when a
        // resolution dimension collapses to one sample and its origin is odd, 1D_SR must halve the coefficient.
        [Test]
        public void Conformance_P1_05_WithinLossyTolerance()
        {
            AssertConformance(
                "codestreams_profile1/p1_05.j2k", 0, "reference_class1_profile1/c1p1_05-0.pgx",
                maxPeakAbsoluteError: 40, maxMeanSquaredError: 8.458);
            AssertConformance(
                "codestreams_profile1/p1_05.j2k", 1, "reference_class1_profile1/c1p1_05-1.pgx",
                maxPeakAbsoluteError: 40, maxMeanSquaredError: 9.716);
            AssertConformance(
                "codestreams_profile1/p1_05.j2k", 2, "reference_class1_profile1/c1p1_05-2.pgx",
                maxPeakAbsoluteError: 40, maxMeanSquaredError: 10.154);
        }

        [Test]
        public void Conformance_P1_06_WithinLossyTolerance()
        {
            AssertConformance(
                "codestreams_profile1/p1_06.j2k",
                [
                    "reference_class1_profile1/c1p1_06-0.pgx",
                    "reference_class1_profile1/c1p1_06-1.pgx",
                    "reference_class1_profile1/c1p1_06-2.pgx",
                ],
                maxPeakAbsoluteError: 2, maxMeanSquaredError: 0.600);
        }

        [Test]
        public void Conformance_P1_07_SubsampledExactMatch()
        {
            AssertConformance(
                "codestreams_profile1/p1_07.j2k",
                [
                    "reference_class1_profile1/c1p1_07-0.pgx",
                    "reference_class1_profile1/c1p1_07-1.pgx",
                ],
                maxPeakAbsoluteError: 0, maxMeanSquaredError: 0);
        }

        private static byte[] ReadConformanceFile(string relativePath)
        {
            relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

            var filePath = Path.Combine(TestFiles.Jpeg2000ConformanceDirectory, relativePath);
            if (!File.Exists(filePath))
            {
                Assert.Inconclusive("The T.803 conformance test file " + relativePath + " is not available.");
            }

            return File.ReadAllBytes(filePath);
        }

        private static JpxPgxFile ReadPgx(string relativePath) => JpxPgxFile.Parse(ReadConformanceFile(relativePath));

        private static JpxDecoder DecodeCodestream(string relativeCodestreamPath)
        {
            var data = ReadConformanceFile(relativeCodestreamPath);
            var decoder = new JpxDecoder();
            decoder.ReadMetadata(data, 0, data.Length);
            return decoder;
        }

        /// <summary>
        /// Asserts a decoded component is within the ITU-T T.803 tolerance of a PGX reference image, comparing at
        /// the component's own native (possibly subsampled) sample grid. A tolerance of
        /// (<paramref name="maxPeakAbsoluteError"/>, <paramref name="maxMeanSquaredError"/>) = (0, 0) requires a
        /// bit-exact match.
        /// </summary>
        private static void AssertConformance(
            string codestreamPath, int componentIndex, string referencePath,
            int maxPeakAbsoluteError, double maxMeanSquaredError, int? maxResolution = null)
        {
            var decoder = DecodeCodestream(codestreamPath);
            var reference = ReadPgx(referencePath);
            if (maxResolution.HasValue)
            {
                decoder.SetConstraints(1, JpxAlphaMode.None, maxResolution.Value);
            }
            var samples = decoder.GetComponentSamples(componentIndex);
            var component = decoder.Components[componentIndex];

            Assert.AreEqual(component.Signed, reference.Signed,
                "Component {0}: signedness does not match the reference.", componentIndex);
            Assert.AreEqual(reference.Width * reference.Height, samples.Length,
                "Component {0}: dimensions do not match the reference.", componentIndex);

            // The valid representable range of a reconstructed sample: signed components are centered on zero (no
            // DC level shift, ITU-T T.800 (06/2019) Section G.1.2), unsigned components span [0, MaxValue]. 9-7
            // irreversible reconstruction routinely overshoots this range with ringing near sharp edges; clamping
            // before comparison matches how the decoder's own normalized application output handles the same
            // overshoot, instead of mistaking it for a decode defect.
            int low, high;
            if (component.Signed)
            {
                high = (1 << (component.Precision - 1)) - 1;
                low = -high - 1;
            }
            else
            {
                low = 0;
                high = component.MaxValue;
            }

            long sumSquaredError = 0;
            var peakAbsoluteError = 0;

            for (var i = 0; i < samples.Length; i++)
            {
                var actualSample = MathUtils.Clamp((int)Math.Round(samples[i]), low, high);
                var error = Math.Abs(actualSample - reference.Samples[i]);

                if (error > peakAbsoluteError)
                {
                    peakAbsoluteError = error;
                }

                sumSquaredError += (long)error * error;
            }

            var meanSquaredError = (double)sumSquaredError / samples.Length;

            Assert.LessOrEqual(peakAbsoluteError, maxPeakAbsoluteError,
                "Component {0}: peak absolute error exceeds the ITU-T T.803 tolerance.", componentIndex);
            Assert.LessOrEqual(meanSquaredError, maxMeanSquaredError,
                "Component {0}: mean squared error exceeds the ITU-T T.803 tolerance.", componentIndex);
        }

        /// <summary>
        /// Asserts every one of <paramref name="referencePaths"/> (component 0, 1, ... in order) against the same
        /// (<paramref name="maxPeakAbsoluteError"/>, <paramref name="maxMeanSquaredError"/>) tolerance.
        /// </summary>
        private static void AssertConformance(
            string codestreamPath, string[] referencePaths,
            int maxPeakAbsoluteError, double maxMeanSquaredError, int? maxResolution = null)
        {
            for (var component = 0; component < referencePaths.Length; component++)
            {
                AssertConformance(
                    codestreamPath, component, referencePaths[component],
                    maxPeakAbsoluteError, maxMeanSquaredError, maxResolution);
            }
        }

        /// <summary>
        /// Asserts an exact match between one component's decoded indices and a PGX reference image. Exercises
        /// <see cref="JpxDecoder.GetComponentIndices"/> specifically (image-grid resampled, byte-valued output),
        /// unlike <see cref="AssertConformance(string, int, string, int, double)"/> above, which always compares on
        /// the native sample grid.
        /// </summary>
        private static void AssertExactMatch(string codestreamPath, int componentIndex, string pgxPath)
        {
            var decoder = DecodeCodestream(codestreamPath);
            var actual = decoder.GetComponentIndices(componentIndex);
            var reference = ReadPgx(pgxPath);

            Assert.AreEqual(reference.Width * reference.Height, actual.Length);

            var expected = new byte[actual.Length];
            for (var i = 0; i < expected.Length; i++)
            {
                expected[i] = (byte)reference.Samples[i];
            }

            Assert.AreEqual(expected, actual);
        }
    }
}
