// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

#if NET8_0_OR_GREATER
using NUnit.Framework;

namespace PdfToSvg.Tests
{
    [SetUpFixture]
    public sealed class TestSetup
    {
        [OneTimeSetUp]
        public void LogIntrinsics()
        {
            TestContext.Progress.WriteLine("-------------");
            TestContext.Progress.WriteLine("IsHardwareAccelerated:");
            TestContext.Progress.WriteLine("  Vector128: " + System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated);
            TestContext.Progress.WriteLine("  Vector256: " + System.Runtime.Intrinsics.Vector256.IsHardwareAccelerated);
            TestContext.Progress.WriteLine("  Vector512: " + System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated);
            TestContext.Progress.WriteLine("ARM");
            TestContext.Progress.WriteLine("  AdvSimd: " + System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported);
            TestContext.Progress.WriteLine("  AdvSimd.Arm64: " + System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported);
            TestContext.Progress.WriteLine("  Aes: " + System.Runtime.Intrinsics.Arm.Aes.IsSupported);
            TestContext.Progress.WriteLine("x86");
            TestContext.Progress.WriteLine("  Sse2: " + System.Runtime.Intrinsics.X86.Sse2.IsSupported);
            TestContext.Progress.WriteLine("  Ssse3: " + System.Runtime.Intrinsics.X86.Ssse3.IsSupported);
            TestContext.Progress.WriteLine("  Sse41: " + System.Runtime.Intrinsics.X86.Sse41.IsSupported);
            TestContext.Progress.WriteLine("  Sse42: " + System.Runtime.Intrinsics.X86.Sse42.IsSupported);
            TestContext.Progress.WriteLine("  Fma: " + System.Runtime.Intrinsics.X86.Fma.IsSupported);
            TestContext.Progress.WriteLine("  Avx: " + System.Runtime.Intrinsics.X86.Avx.IsSupported);
            TestContext.Progress.WriteLine("  Avx2: " + System.Runtime.Intrinsics.X86.Avx2.IsSupported);
            TestContext.Progress.WriteLine("  Avx512F: " + System.Runtime.Intrinsics.X86.Avx512F.IsSupported);
            TestContext.Progress.WriteLine("  Aes: " + System.Runtime.Intrinsics.X86.Aes.IsSupported);
            TestContext.Progress.WriteLine("-------------");
        }
    }
}
#endif
