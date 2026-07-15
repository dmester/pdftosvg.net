// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.ImageModel;
using PdfToSvg.Imaging.Jpx.IO;
using PdfToSvg.Imaging.Jpx.Packets;
using System;
using System.Collections.Generic;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    internal static class JpxMarkerSegmentReader
    {
        // ITU-T T.800 (06/2019) Section A.5.1 Image and tile size (SIZ).
        public static void ReadSiz(JpxDataReader reader, JpxImageInfo image)
        {
            reader = reader.ReadSegmentContent();

            image.Rsiz = reader.ReadUInt16();
            image.Xsiz = reader.ReadInt32();
            image.Ysiz = reader.ReadInt32();
            image.XOsiz = reader.ReadInt32();
            image.YOsiz = reader.ReadInt32();
            image.XTsiz = reader.ReadInt32();
            image.YTsiz = reader.ReadInt32();
            image.XTOsiz = reader.ReadInt32();
            image.YTOsiz = reader.ReadInt32();

            if (image.Xsiz <= 0 || image.Ysiz <= 0)
            {
                throw new JpxException("Invalid image size");
            }
            if (image.XTsiz <= 0 || image.YTsiz <= 0)
            {
                throw new JpxException("Invalid tile size");
            }

            // References below are to ITU-T T.800 (06/2019):
            if (0 > image.XTOsiz || image.XTOsiz > image.XOsiz ||      // Equation B-3
                0 > image.YTOsiz || image.YTOsiz > image.YOsiz ||      // Equation B-3 
                (long)image.XTsiz + image.XTOsiz <= image.XOsiz ||     // Equation B-4
                (long)image.YTsiz + image.YTOsiz <= image.YOsiz ||     // Equation B-4
                image.XOsiz >= image.Xsiz || image.YOsiz >= image.Ysiz // Section B.2
                )
            {
                throw new JpxException("Invalid JPEG 2000 image or tile offsets");
            }

            if (image.Xsiz > JpxConstraints.MaxResolution ||
                image.Ysiz > JpxConstraints.MaxResolution)
            {
                throw new JpxException(
                    "The JPEG 2000 image is too large (" + image.Xsiz + "x" + image.Ysiz + "). " +
                    "Max resolution is " + JpxConstraints.MaxResolution + "x" + JpxConstraints.MaxResolution);
            }

            var numTiles = (long)image.NumXTiles * image.NumYTiles;
            if (numTiles > JpxConstraints.MaxTileCount)
            {
                throw new JpxException(
                    "Number of tiles (" + numTiles + ") in this JPEG 2000 image exceeds the " +
                    "maximum allowed tiles (" + JpxConstraints.MaxTileCount + ")");
            }

            var componentCount = reader.ReadUInt16();
            if (componentCount == 0)
            {
                throw new JpxException("Invalid component count");
            }
            if (componentCount > JpxConstraints.MaxComponents)
            {
                throw new JpxException(
                    "The number of components (" + componentCount + ") in the JPEG 2000 image " +
                    "exceeds the maximum allowed number of components (" + JpxConstraints.MaxComponents + ")");
            }

            var components = new JpxComponent[componentCount];

            for (var i = 0; i < components.Length; i++)
            {
                var component = new JpxComponent
                {
                    Ssizi = reader.ReadByte(),
                    XRsizi = reader.ReadByte(),
                    YRsizi = reader.ReadByte(),
                };

                if (component.XRsizi == 0 || component.YRsizi == 0)
                {
                    throw new JpxException("Invalid sample separation");
                }
                if (component.Precision > JpxConstraints.MaxComponentPrecision)
                {
                    throw new JpxException(
                        "Unsupported JPEG 2000 component precision (" + component.Precision + " bits). " +
                        "Maximum supported precision is " + JpxConstraints.MaxComponentPrecision + " bits.");
                }

                components[i] = component;
            }

            image.Components = components;
        }

        public static JpxTilePart ReadSot(JpxDataReader reader)
        {
            var length = reader.ReadUInt16();

            // ITU-T T.800 (06/2019) Section A.4.2 Start of tile-part (SOT): Lsot is fixed at 10.
            if (length != 10)
            {
                throw new JpxException("Invalid SOT marker: Lsot must be 10");
            }

            return new JpxTilePart
            {
                TileIndex = reader.ReadUInt16(),
                TileLength = reader.ReadInt32(),
                TilePartIndex = reader.ReadByte(),
                TilePartCount = reader.ReadByte(),
            };
        }

        // ITU-T T.800 (06/2019) Sections A.7.4 (PPM) and A.7.5 (PPT).
        public static JpxPackedPacketHeaderSegment ReadPackedPacketHeaders(JpxDataReader reader)
        {
            reader = reader.ReadSegmentContent();

            var result = new JpxPackedPacketHeaderSegment
            {
                Index = reader.ReadByte(),
                Data = reader.ReadBytes(reader.Length - reader.Cursor),
            };

            return result;
        }

        // ITU-T T.800 (06/2019) Section A.6.1 Coding style default (COD).
        public static JpxCodingStyleDefaults ReadCod(JpxDataReader reader)
        {
            reader = reader.ReadSegmentContent();

            var result = new JpxCodingStyleDefaults();

            // Table A.13 – Coding style parameter values for the Scod parameter.
            var scod = reader.ReadByte();
            result.CodingStyle.UserDefinedPrecincts = (scod & 1) != 0;
            result.CodingStyle.SopMarkerSegments = (scod & 2) != 0;
            result.CodingStyle.EphMarkerSegments = (scod & 4) != 0;

            // Table A.14 – SGcod parameters (tile-scoped; not present in COC).
            var progressionOrder = (JpxCodingProgressionOrder)reader.ReadByte();
            var numberOfLayers = reader.ReadUInt16();
            var multipleComponentTransformation = reader.ReadByte();

            if (!Enum.IsDefined(typeof(JpxCodingProgressionOrder), progressionOrder))
            {
                throw new JpxException("Invalid JPEG 2000 progression order " + progressionOrder);
            }
            if (numberOfLayers < 1)
            {
                throw new JpxException("Number of layers must not be zero");
            }
            if (multipleComponentTransformation > 1)
            {
                throw new JpxException("Invalid JPEG 2000 multiple component transformation " +
                    multipleComponentTransformation);
            }

            result.ProgressionOrder = progressionOrder;
            result.NumberOfLayers = numberOfLayers;
            result.MultipleComponentTransformation = multipleComponentTransformation != 0;

            // Table A.15 – SPcod parameters.
            var componentParameters = ReadCodingStyleParameters(reader, result.CodingStyle.UserDefinedPrecincts);
            componentParameters.CopyTo(result);

            return result;
        }

        // ITU-T T.800 (06/2019) Section A.6.2 Coding style component (COC).
        public static JpxCodingStyleComponent ReadCoc(JpxDataReader reader, int componentCount, out int componentIndex)
        {
            reader = reader.ReadSegmentContent();

            componentIndex = ReadComponentIndex(reader, componentCount);

            // Table A.23 – Coding style parameter values for the Scoc parameter.
            var scoc = reader.ReadByte();

            // Table A.15 – SPcoc parameters (same layout as SPcod).
            return ReadCodingStyleParameters(reader, userDefinedPrecincts: (scoc & 1) != 0);
        }

        // ITU-T T.800 (06/2019) Tables A.15/A.18 – SPcod/SPcoc coding style parameters.
        private static JpxCodingStyleComponent ReadCodingStyleParameters(JpxDataReader reader, bool userDefinedPrecincts)
        {
            var result = new JpxCodingStyleComponent
            {
                UserDefinedPrecincts = userDefinedPrecincts,
            };

            var numberOfDecompositionLevels = reader.ReadByte();
            var xcb = reader.ReadByte() + 2;
            var ycb = reader.ReadByte() + 2;

            if (numberOfDecompositionLevels > 32)
            {
                throw new JpxException(
                    "Invalid number of decomposition levels (" + numberOfDecompositionLevels + ").");
            }
            if (xcb > 10 || ycb > 10 || xcb + ycb > 12)
            {
                throw new JpxException("Invalid code block size " + xcb + "x" + ycb);
            }

            result.NumberOfDecompositionLevels = numberOfDecompositionLevels;
            result.CodeBlockWidth = 1 << xcb;
            result.CodeBlockHeight = 1 << ycb;

            // Table A.19 – Code-block style for the SPcod and SPcoc parameters.
            var codeBlockStyle = reader.ReadByte();

            // ITU-T T.800 (06/2019) Table A.19 reserves the two most significant bits, but ITU-T T.814 (HTJ2K)
            // redefines bit 6 to signal HT code-blocks, which we currently don't support
            if ((codeBlockStyle & 64) != 0)
            {
                throw new JpxException("HTJ2K (ITU-T T.814) codestreams are not supported.");
            }

            result.CodeBlockStyle = new JpxCodeBlockStyle
            {
                SelectiveArithmeticCodingBypass = (codeBlockStyle & 1) != 0,
                ResetContextProbabilitiesOnCodingPassBoundaries = (codeBlockStyle & 2) != 0,
                TerminationOnEachCodingPass = (codeBlockStyle & 4) != 0,
                VerticallyCausalContext = (codeBlockStyle & 8) != 0,
                PredictableTermination = (codeBlockStyle & 16) != 0,
                SegmentationSymbolsAreUsed = (codeBlockStyle & 32) != 0
            };

            var transformation = reader.ReadByte();
            if (transformation > 1)
            {
                throw new JpxException("Invalid JPEG 2000 wavelet transformation.");
            }
            result.ReversibleFilter = transformation == 1;

            var precinctSize = new JpxPrecinctSize[result.NumberOfDecompositionLevels + 1];

            if (userDefinedPrecincts)
            {
                // Table A.21 – Precinct width and height for the SPcod and SPcoc parameters.
                for (var i = 0; i < precinctSize.Length; i++)
                {
                    var value = reader.ReadByte();
                    var ppx = value & 15;
                    var ppy = value >> 4;

                    // ITU-T T.800 (06/2019) Section B.6: PPx/PPy may be zero only for
                    // resolution level r = 0; all other resolution levels require at least 1.
                    if (i > 0 && (ppx == 0 || ppy == 0))
                    {
                        throw new JpxException("Invalid JPEG 2000 precinct size.");
                    }

                    precinctSize[i] = new JpxPrecinctSize
                    {
                        PPx = ppx,
                        PPy = ppy,
                    };
                }
            }
            else
            {
                // ITU-T T.800 (06/2019) Table A.13: maximum precincts, PPx = PPy = 15.
                for (var i = 0; i < precinctSize.Length; i++)
                {
                    precinctSize[i] = new JpxPrecinctSize { PPx = 15, PPy = 15 };
                }
            }

            result.PrecinctSize = precinctSize;
            return result;
        }

        // ITU-T T.800 (06/2019) Section A.6.4 Quantization default (QCD).
        public static JpxQuantizationDefaults ReadQcd(JpxDataReader reader)
        {
            reader = reader.ReadSegmentContent();
            return ReadQuantizationParameters(reader);
        }

        // ITU-T T.800 (06/2019) Section A.6.5 Quantization component (QCC). Unlike COC, a QCC
        // carries a complete quantization specification (Sqcc/SPqcc share the Sqcd/SPqcd layout).
        public static JpxQuantizationDefaults ReadQcc(JpxDataReader reader, int componentCount, out int componentIndex)
        {
            reader = reader.ReadSegmentContent();
            componentIndex = ReadComponentIndex(reader, componentCount);
            var result = ReadQuantizationParameters(reader);
            result.ComponentSpecific = true;
            return result;
        }

        private static JpxQuantizationDefaults ReadQuantizationParameters(JpxDataReader reader)
        {
            const int StyleNoQuantization = 0;
            const int StyleScalarDerived = 1;
            const int StyleScalarExpounded = 2;

            var result = new JpxQuantizationDefaults();

            // Table A.28 – Quantization default values for the Sqcd and Sqcc parameters.
            var sqcd = reader.ReadByte();
            result.NumberOfGuardBits = sqcd >> 5;

            var style = sqcd & 0b11111;
            if (style == StyleNoQuantization)
            {
                // No quantization
                var valueCount = reader.Length - reader.Cursor;
                var values = new JpxQuantizationDefaultValues[valueCount];

                for (var i = 0; i < values.Length; i++)
                {
                    // Table A.29 – Reversible step size values for the SPqcd and SPqcc parameters.
                    values[i] = new JpxQuantizationDefaultValues
                    {
                        Exponent = reader.ReadByte() >> 3,
                    };
                }

                result.ScalarDerived = false;
                result.ScalarExpounded = false;
                result.Values = values;
            }
            else if (style == StyleScalarDerived || style == StyleScalarExpounded)
            {
                var remainingBytes = reader.Length - reader.Cursor;
                // Ignore left-over byte after reading values

                var valueCount = remainingBytes / 2;
                var values = new JpxQuantizationDefaultValues[valueCount];

                for (var i = 0; i < values.Length; i++)
                {
                    // Table A.30 – Quantization values for the SPqcd and SPqcc parameters.
                    var value = reader.ReadUInt16();

                    values[i] = new JpxQuantizationDefaultValues
                    {
                        Exponent = value >> 11,
                        Mantissa = value & 0b111_1111_1111,
                    };
                }

                result.ScalarDerived = style == StyleScalarDerived;
                result.ScalarExpounded = style == StyleScalarExpounded;
                result.Values = values;
            }
            else
            {
                throw new JpxException("Invalid JPEG 2000 quantization style");
            }

            return result;
        }

        // ITU-T T.800 (06/2019) Tables A.22/A.24/A.31:
        //  *  8 bits when Csiz < 257
        //  * 16 bits when Csiz >= 257
        private static int ReadComponentIndex(JpxDataReader reader, int componentCount)
        {
            var componentIndex = componentCount < 257
                ? reader.ReadByte()
                : reader.ReadUInt16();

            if (componentIndex < 0 || componentIndex >= componentCount)
            {
                throw new JpxException("Invalid component index");
            }

            return componentIndex;
        }

        // ITU-T T.800 (06/2019) Section A.6.3 Region of interest (RGN).
        public static bool ReadRgn(
            JpxDataReader reader,
            int componentCount,
            out int componentIndex,
            out int maxShift)
        {
            reader = reader.ReadSegmentContent();

            // Table A.24 – Region-of-interest parameter values.
            componentIndex = ReadComponentIndex(reader, componentCount);
            var srgn = reader.ReadByte();
            maxShift = reader.ReadByte();

            if (srgn != 0)
            {
                Log.WriteLine("Unsupported JPEG 2000 ROI style (Srgn=" + srgn + "); ignoring RGN marker.");
                maxShift = 0;
                return false;
            }
            if (maxShift > JpxConstraints.MaxRegionOfInterestShift)
            {
                Log.WriteLine("Invalid JPEG 2000 ROI maxshift (SPrgn=" + maxShift + "); ignoring RGN marker.");
                maxShift = 0;
                return false;
            }

            return true;
        }

        // ITU-T T.800 (06/2019) Section A.6.6 Progression order change (POC). LYEpoc is validated
        // structurally only (>= 1, Table A.32); its relation to the number of layers depends on the
        // resolved per-tile COD and is enforced by JpxParameterResolver.
        public static List<JpxProgressionVolume> ReadPoc(JpxDataReader reader, int componentCount)
        {
            reader = reader.ReadSegmentContent();
            var componentBytes = componentCount < 257 ? 1 : 2;

            var entrySize = componentBytes == 1 ? 7 : 9;
            if ((reader.Length - reader.Cursor) % entrySize != 0)
            {
                throw new JpxException("Invalid JPEG 2000 POC marker length.");
            }

            var volumes = new List<JpxProgressionVolume>();

            while (!reader.EndOfStream)
            {
                int resolutionStart = reader.ReadByte();
                int componentStart = componentBytes == 1 ? reader.ReadByte() : reader.ReadUInt16();
                int layerEnd = reader.ReadUInt16();
                int resolutionEnd = reader.ReadByte();
                int componentEnd = componentBytes == 1 ? reader.ReadByte() : reader.ReadUInt16();
                var progressionOrder = (JpxCodingProgressionOrder)reader.ReadByte();

                // Table A.32: CEpoc: 0 is interpreted as 256
                if (componentEnd == 0)
                {
                    componentEnd = 256;
                }

                // Table A.32 allows CEpoc values up to 255 (or 16384) regardless of Csiz.
                // Clamp to the actual count.
                if (componentEnd > componentCount)
                {
                    componentEnd = componentCount;
                }

                if (resolutionStart >= resolutionEnd ||
                    resolutionEnd > 33 ||
                    componentStart >= componentEnd ||
                    layerEnd < 1 ||
                    !Enum.IsDefined(typeof(JpxCodingProgressionOrder), progressionOrder))
                {
                    throw new JpxException("Invalid JPEG 2000 progression order change.");
                }

                volumes.Add(new JpxProgressionVolume
                {
                    ResolutionStart = resolutionStart,
                    ResolutionEnd = resolutionEnd,
                    ComponentStart = componentStart,
                    ComponentEnd = componentEnd,
                    LayerEnd = layerEnd,
                    ProgressionOrder = progressionOrder,
                });
            }

            return volumes;
        }

        public static uint ReadExtendedCapabilities(JpxDataReader reader)
        {
            reader = reader.ReadSegmentContent();

            // ITU-T T.800 (06/2019) Section A.5.2 Extended capabilities (CAP).
            if (reader.Length - reader.Cursor < 4)
            {
                return 0;
            }

            var pcap = reader.ReadUInt32();

            if (pcap == 0)
            {
                return pcap;
            }

            // ITU-T T.800 (06/2019) Table A.11 ter: Pcap bit 15 (counted from the most significant bit) signals
            // that the codestream requires ITU-T T.814 (HTJ2K) capabilities. HT code-block data cannot be decoded
            // by the Part 1 block decoder, so such codestreams are rejected instead of producing garbage samples.
            if ((pcap & (1u << (32 - 15))) != 0)
            {
                throw new JpxException("HTJ2K (ITU-T T.814) codestreams are not supported.");
            }

            Log.WriteLine(
                "JPEG 2000 codestream requires extended capabilities. Pcap=" + pcap + ". " +
                "Decoding will continue with best-effort.");

            return pcap;
        }
    }
}
