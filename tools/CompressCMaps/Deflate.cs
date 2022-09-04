// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressCMaps
{
    internal static class Deflate
    {
        private const string ZopfliFileName = "zopfli.exe";

        public static byte[] Compress(byte[] input)
        {
            var zopfli = CompressZopfli(input);
            var net = CompressNet(input);

            if (zopfli == null)
            {
                Console.WriteLine("The file might be better compressed by putting zopfli.exe in the output directory.");
                return net;
            }

            return zopfli.Length < net.Length ? zopfli : net;
        }

        private static byte[]? CompressZopfli(byte[] input)
        {
            var zopfliPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ZopfliFileName);

            if (File.Exists(zopfliPath))
            {
                var tempFilePath = Path.GetTempFileName();
                var outputTempFilePath = tempFilePath + ".deflate";

                try
                {
                    File.WriteAllBytes(tempFilePath, input);

                    var process = new Process();

                    process.StartInfo.FileName = zopfliPath;
                    process.StartInfo.Arguments = "--i100 --deflate \"" + tempFilePath + "\"";

                    Console.WriteLine("Zopfli is running...");

                    process.Start();
                    if (!process.WaitForExit(60000))
                    {
                        process.Kill();
                        throw new Exception("Zopfli did not exit in time.");
                    }

                    return File.ReadAllBytes(outputTempFilePath);
                }
                finally
                {
                    try { File.Delete(tempFilePath); } catch { }
                    try { File.Delete(outputTempFilePath); } catch { }
                }
            }

            return null;
        }

        private static byte[] CompressNet(byte[] input)
        {
            using var stream = new MemoryStream();

            using (var compressor = new DeflateStream(stream, CompressionLevel.Optimal))
            {
                compressor.Write(input, 0, input.Length);
            }

            return stream.ToArray();
        }
    }
}
