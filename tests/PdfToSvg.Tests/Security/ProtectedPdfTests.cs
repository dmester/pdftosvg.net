// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Security
{
    public class ProtectedPdfTests
    {
        private const string UserPassword = "user";
        private const string OwnerPassword = "owner";
        private const string LongUserPasswordNC = "åäö0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890";
        private const string LongUserPasswordND = "a\u030aa\u0308o\u03080123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890";

        public static List<TestCaseData> AllowedCases => new List<TestCaseData>
        {
            new TestCaseData(UserPassword, "protected-40-userpw.pdf"),
            new TestCaseData(null, "protected-128-allowextract.pdf"),
            new TestCaseData(null, "protected-aes128.pdf"),
            new TestCaseData(UserPassword, "protected-aes256-rev5.pdf"),
            new TestCaseData(null, "protected-aes256-rev6.pdf"),
            new TestCaseData(null, "protected-aes256-rev6-identity.pdf"),
            new TestCaseData(OwnerPassword, "protected-40-userpw.pdf"),
            new TestCaseData(OwnerPassword, "protected-128-allowextract.pdf"),
            new TestCaseData(OwnerPassword, "protected-128-noextract.pdf"),
            new TestCaseData(OwnerPassword, "protected-aes128.pdf"),
            new TestCaseData(OwnerPassword, "protected-aes256-rev5.pdf"),
            new TestCaseData(OwnerPassword, "protected-aes256-rev6.pdf"),
            new TestCaseData(OwnerPassword, "protected-aes256-rev6-identity.pdf"),
            new TestCaseData(LongUserPasswordNC, "protected-aes256-rev6-longpass.pdf"),
            new TestCaseData(LongUserPasswordNC + "xxx", "protected-aes256-rev6-longpass.pdf"),
#if !NETCOREAPP2_1
            new TestCaseData(LongUserPasswordND, "protected-aes256-rev6-longpass.pdf"),
            new TestCaseData(LongUserPasswordND + "xxx", "protected-aes256-rev6-longpass.pdf"),
#endif
        };

        public static List<TestCaseData> DisallowedCases => new List<TestCaseData>
        {
            new TestCaseData(null, "protected-128-noextract.pdf"),
        };

        [TestCaseSource(nameof(AllowedCases))]
        public void AllowedDecrypt(string password, string filename)
        {
            var path = Path.Combine(TestFiles.TestFilesPath, "Protected", filename);

            using (var doc = PdfDocument.Open(path, new OpenOptions { Password = password }))
            {
                Assert.IsTrue(doc.Pages[0].ToSvgString().Contains("This is an encrypted PDF"), "Decrypted");
                Assert.AreEqual("PdfToSvg.NET", doc.Author);
            }
        }

        [TestCaseSource(nameof(DisallowedCases))]
        public void DisallowedDecrypt(string password, string filename)
        {
            var path = Path.Combine(TestFiles.TestFilesPath, "Protected", filename);

            Assert.Throws<PermissionException>(() =>
            {
                using (var doc = PdfDocument.Open(path, new OpenOptions { Password = password }))
                {
                    doc.Pages[0].ToSvgString().Contains("This is an encrypted PDF");
                }
            });
        }

#if !NET40
        [TestCaseSource(nameof(AllowedCases))]
        public async Task AllowedDecryptAsync(string password, string filename)
        {
            var path = Path.Combine(TestFiles.TestFilesPath, "Protected", filename);

            using (var doc = await PdfDocument.OpenAsync(path, new OpenOptions { Password = password }))
            {
                Assert.IsTrue((await doc.Pages[0].ToSvgStringAsync()).Contains("This is an encrypted PDF"), "Decrypted");
                Assert.AreEqual("PdfToSvg.NET", doc.Author);
            }
        }

        [TestCaseSource(nameof(DisallowedCases))]
        public void DisallowedDecryptAsync(string password, string filename)
        {
            var path = Path.Combine(TestFiles.TestFilesPath, "Protected", filename);

            Assert.ThrowsAsync<PermissionException>(async () =>
            {
                using (var doc = await PdfDocument.OpenAsync(path, new OpenOptions { Password = password }))
                {
                    doc.Pages[0].ToSvgString().Contains("This is an encrypted PDF");
                }
            });
        }
#endif
    }
}
