// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Coding
{
    internal static class JbigStandardHuffmanTable
    {
        /// <summary>
        /// Table B.1 – Standard Huffman table A
        /// </summary>
        public static JbigHuffmanTable TableB1 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(0, 1, 4),
                JbigHuffmanRange.Range(16, 2, 8),
                JbigHuffmanRange.Range(272, 3, 16),
                JbigHuffmanRange.Upper(65808, 3),
            ]);

        /// <summary>
        /// Table B.2 – Standard Huffman table B
        /// </summary>
        public static JbigHuffmanTable TableB2 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(0, 1, 0),
                JbigHuffmanRange.Range(1, 2, 0),
                JbigHuffmanRange.Range(2, 3, 0),
                JbigHuffmanRange.Range(3, 4, 3),
                JbigHuffmanRange.Range(11, 5, 6),
                JbigHuffmanRange.Upper(75, 6),
                JbigHuffmanRange.OutOfBand(6),
            ]);

        /// <summary>
        /// Table B.3 – Standard Huffman table C
        /// </summary>
        public static JbigHuffmanTable TableB3 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(-256, 8, 8),
                JbigHuffmanRange.Range(0, 1, 0),
                JbigHuffmanRange.Range(1, 2, 0),
                JbigHuffmanRange.Range(2, 3, 0),
                JbigHuffmanRange.Range(3, 4, 3),
                JbigHuffmanRange.Range(11, 5, 6),
                JbigHuffmanRange.Lower(-257, 8),
                JbigHuffmanRange.Upper(75, 7),
                JbigHuffmanRange.OutOfBand(6),
            ]);

        /// <summary>
        /// Table B.4 – Standard Huffman table D
        /// </summary>
        public static JbigHuffmanTable TableB4 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(1, 1, 0),
                JbigHuffmanRange.Range(2, 2, 0),
                JbigHuffmanRange.Range(3, 3, 0),
                JbigHuffmanRange.Range(4, 4, 3),
                JbigHuffmanRange.Range(12, 5, 6),
                JbigHuffmanRange.Upper(76, 5),
            ]);

        /// <summary>
        /// Table B.5 – Standard Huffman table E
        /// </summary>
        public static JbigHuffmanTable TableB5 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(-255, 7, 8),
                JbigHuffmanRange.Range(1, 1, 0),
                JbigHuffmanRange.Range(2, 2, 0),
                JbigHuffmanRange.Range(3, 3, 0),
                JbigHuffmanRange.Range(4, 4, 3),
                JbigHuffmanRange.Range(12, 5, 6),
                JbigHuffmanRange.Lower(-256, 7),
                JbigHuffmanRange.Upper(76, 6),
            ]);

        /// <summary>
        /// Table B.6 – Standard Huffman table F
        /// </summary>
        public static JbigHuffmanTable TableB6 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(-2048, 5, 10),
                JbigHuffmanRange.Range(-1024, 4, 9),
                JbigHuffmanRange.Range(-512, 4, 8),
                JbigHuffmanRange.Range(-256, 4, 7),
                JbigHuffmanRange.Range(-128, 5, 6),
                JbigHuffmanRange.Range(-64, 5, 5),
                JbigHuffmanRange.Range(-32, 4, 5),
                JbigHuffmanRange.Range(0, 2, 7),
                JbigHuffmanRange.Range(128, 3, 7),
                JbigHuffmanRange.Range(256, 3, 8),
                JbigHuffmanRange.Range(512, 4, 9),
                JbigHuffmanRange.Range(1024, 4, 10),
                JbigHuffmanRange.Lower(-2049, 6),
                JbigHuffmanRange.Upper(2048, 6),
            ]);

        /// <summary>
        /// Table B.7 – Standard Huffman table G
        /// </summary>
        public static JbigHuffmanTable TableB7 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(-1024, 4, 9),
                JbigHuffmanRange.Range(-512, 3, 8),
                JbigHuffmanRange.Range(-256, 4, 7),
                JbigHuffmanRange.Range(-128, 5, 6),
                JbigHuffmanRange.Range(-64, 5, 5),
                JbigHuffmanRange.Range(-32, 4, 5),
                JbigHuffmanRange.Range(0, 4, 5),
                JbigHuffmanRange.Range(32, 5, 5),
                JbigHuffmanRange.Range(64, 5, 6),
                JbigHuffmanRange.Range(128, 4, 7),
                JbigHuffmanRange.Range(256, 3, 8),
                JbigHuffmanRange.Range(512, 3, 9),
                JbigHuffmanRange.Range(1024, 3, 10),
                JbigHuffmanRange.Lower(-1025, 5),
                JbigHuffmanRange.Upper(2048, 5),
            ]);

        /// <summary>
        /// Table B.8 – Standard Huffman table H
        /// </summary>
        public static JbigHuffmanTable TableB8 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(-15, 8, 3),
                JbigHuffmanRange.Range(-7, 9, 1),
                JbigHuffmanRange.Range(-5, 8, 1),
                JbigHuffmanRange.Range(-3, 9, 0),
                JbigHuffmanRange.Range(-2, 7, 0),
                JbigHuffmanRange.Range(-1, 4, 0),
                JbigHuffmanRange.Range(0, 2, 1),
                JbigHuffmanRange.Range(2, 5, 0),
                JbigHuffmanRange.Range(3, 6, 0),
                JbigHuffmanRange.Range(4, 3, 4),
                JbigHuffmanRange.Range(20, 6, 1),
                JbigHuffmanRange.Range(22, 4, 4),
                JbigHuffmanRange.Range(38, 4, 5),
                JbigHuffmanRange.Range(70, 5, 6),
                JbigHuffmanRange.Range(134, 5, 7),
                JbigHuffmanRange.Range(262, 6, 7),
                JbigHuffmanRange.Range(390, 7, 8),
                JbigHuffmanRange.Range(646, 6, 10),
                JbigHuffmanRange.Lower(-16, 9),
                JbigHuffmanRange.Upper(1670, 9),
                JbigHuffmanRange.OutOfBand(2),
            ]);

        /// <summary>
        /// Table B.9 – Standard Huffman table I
        /// </summary>
        public static JbigHuffmanTable TableB9 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(-31, 8, 4),
                JbigHuffmanRange.Range(-15, 9, 2),
                JbigHuffmanRange.Range(-11, 8, 2),
                JbigHuffmanRange.Range(-7, 9, 1),
                JbigHuffmanRange.Range(-5, 7, 1),
                JbigHuffmanRange.Range(-3, 4, 1),
                JbigHuffmanRange.Range(-1, 3, 1),
                JbigHuffmanRange.Range(1, 3, 1),
                JbigHuffmanRange.Range(3, 5, 1),
                JbigHuffmanRange.Range(5, 6, 1),
                JbigHuffmanRange.Range(7, 3, 5),
                JbigHuffmanRange.Range(39, 6, 2),
                JbigHuffmanRange.Range(43, 4, 5),
                JbigHuffmanRange.Range(75, 4, 6),
                JbigHuffmanRange.Range(139, 5, 7),
                JbigHuffmanRange.Range(267, 5, 8),
                JbigHuffmanRange.Range(523, 6, 8),
                JbigHuffmanRange.Range(779, 7, 9),
                JbigHuffmanRange.Range(1291, 6, 11),
                JbigHuffmanRange.Lower(-32, 9),
                JbigHuffmanRange.Upper(3339, 9),
                JbigHuffmanRange.OutOfBand(2),
            ]);

        /// <summary>
        /// Table B.10 – Standard Huffman table J
        /// </summary>
        public static JbigHuffmanTable TableB10 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(-21, 7, 4),
                JbigHuffmanRange.Range(-5, 8, 0),
                JbigHuffmanRange.Range(-4, 7, 0),
                JbigHuffmanRange.Range(-3, 5, 0),
                JbigHuffmanRange.Range(-2, 2, 2),
                JbigHuffmanRange.Range(2, 5, 0),
                JbigHuffmanRange.Range(3, 6, 0),
                JbigHuffmanRange.Range(4, 7, 0),
                JbigHuffmanRange.Range(5, 8, 0),
                JbigHuffmanRange.Range(6, 2, 6),
                JbigHuffmanRange.Range(70, 5, 5),
                JbigHuffmanRange.Range(102, 6, 5),
                JbigHuffmanRange.Range(134, 6, 6),
                JbigHuffmanRange.Range(198, 6, 7),
                JbigHuffmanRange.Range(326, 6, 8),
                JbigHuffmanRange.Range(582, 6, 9),
                JbigHuffmanRange.Range(1094, 6, 10),
                JbigHuffmanRange.Range(2118, 7, 11),
                JbigHuffmanRange.Lower(-22, 8),
                JbigHuffmanRange.Upper(4166, 8),
                JbigHuffmanRange.OutOfBand(2),
            ]);

        /// <summary>
        /// Table B.11 – Standard Huffman table K
        /// </summary>
        public static JbigHuffmanTable TableB11 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(1, 1, 0),
                JbigHuffmanRange.Range(2, 2, 1),
                JbigHuffmanRange.Range(4, 4, 0),
                JbigHuffmanRange.Range(5, 4, 1),
                JbigHuffmanRange.Range(7, 5, 1),
                JbigHuffmanRange.Range(9, 5, 2),
                JbigHuffmanRange.Range(13, 6, 2),
                JbigHuffmanRange.Range(17, 7, 2),
                JbigHuffmanRange.Range(21, 7, 3),
                JbigHuffmanRange.Range(29, 7, 4),
                JbigHuffmanRange.Range(45, 7, 5),
                JbigHuffmanRange.Range(77, 7, 6),
                JbigHuffmanRange.Upper(141, 7),
            ]);

        /// <summary>
        /// Table B.12 – Standard Huffman table L
        /// </summary>
        public static JbigHuffmanTable TableB12 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(1, 1, 0),
                JbigHuffmanRange.Range(2, 2, 0),
                JbigHuffmanRange.Range(3, 3, 1),
                JbigHuffmanRange.Range(5, 5, 0),
                JbigHuffmanRange.Range(6, 5, 1),
                JbigHuffmanRange.Range(8, 6, 1),
                JbigHuffmanRange.Range(10, 7, 0),
                JbigHuffmanRange.Range(11, 7, 1),
                JbigHuffmanRange.Range(13, 7, 2),
                JbigHuffmanRange.Range(17, 7, 3),
                JbigHuffmanRange.Range(25, 7, 4),
                JbigHuffmanRange.Range(41, 8, 5),
                JbigHuffmanRange.Upper(73, 8),
            ]);

        /// <summary>
        /// Table B.13 – Standard Huffman table M
        /// </summary>
        public static JbigHuffmanTable TableB13 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(1, 1, 0),
                JbigHuffmanRange.Range(2, 3, 0),
                JbigHuffmanRange.Range(3, 4, 0),
                JbigHuffmanRange.Range(4, 5, 0),
                JbigHuffmanRange.Range(5, 4, 1),
                JbigHuffmanRange.Range(7, 3, 3),
                JbigHuffmanRange.Range(15, 6, 1),
                JbigHuffmanRange.Range(17, 6, 2),
                JbigHuffmanRange.Range(21, 6, 3),
                JbigHuffmanRange.Range(29, 6, 4),
                JbigHuffmanRange.Range(45, 6, 5),
                JbigHuffmanRange.Range(77, 7, 6),
                JbigHuffmanRange.Upper(141, 7),
            ]);

        /// <summary>
        /// Table B.14 – Standard Huffman table N
        /// </summary>
        public static JbigHuffmanTable TableB14 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(-2, 3, 0),
                JbigHuffmanRange.Range(-1, 3, 0),
                JbigHuffmanRange.Range(0, 1, 0),
                JbigHuffmanRange.Range(1, 3, 0),
                JbigHuffmanRange.Range(2, 3, 0),
            ]);

        /// <summary>
        /// Table B.15 – Standard Huffman table O
        /// </summary>
        public static JbigHuffmanTable TableB15 { get; } = new JbigHuffmanTable(
            [
                JbigHuffmanRange.Range(-24, 7, 4),
                JbigHuffmanRange.Range(-8, 6, 2),
                JbigHuffmanRange.Range(-4, 5, 1),
                JbigHuffmanRange.Range(-2, 4, 0),
                JbigHuffmanRange.Range(-1, 3, 0),
                JbigHuffmanRange.Range(0, 1, 0),
                JbigHuffmanRange.Range(1, 3, 0),
                JbigHuffmanRange.Range(2, 4, 0),
                JbigHuffmanRange.Range(3, 5, 1),
                JbigHuffmanRange.Range(5, 6, 2),
                JbigHuffmanRange.Range(9, 7, 4),
                JbigHuffmanRange.Lower(-25, 7),
                JbigHuffmanRange.Upper(25, 7),
            ]);

    }
}
