// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests
{
    public class FontRepositoryTests
    {
        private static void DoesNotModifyReadOnlyRepository(Action<FontRepository> body)
        {
            var repo = FontRepository.SystemFonts;
            var countBefore = repo.Count;

            Assert.Throws<InvalidOperationException>(() =>
            {
                body(repo);
            });

            Assert.AreEqual(countBefore, repo.Count);
        }

        [Test]
        public void SystemFonts()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Assert.Greater(FontRepository.SystemFonts.Count, 0);
            }
        }

        [Test]
        public void AddSystemFonts_ReadOnly()
        {
            DoesNotModifyReadOnlyRepository(repo => repo.AddSystemFonts());
        }

        [Test]
        public void AddFont()
        {
            var font = Path.Combine(TestFiles.ExternalFontsDirectory, "PdfToSvgTTF-Installable.ttf");
            var repo = new FontRepository();

            repo.AddFont(font);

            Assert.AreEqual(1, repo.Count);
        }

        [Test]
        public void AddFont_NullPath()
        {
            var repo = new FontRepository();

            Assert.Throws<ArgumentNullException>(() => repo.AddFont(null!));
        }

        [Test]
        public void AddFont_EmptyPath()
        {
            var repo = new FontRepository();

            Assert.Throws<ArgumentException>(() => repo.AddFont(""));
        }

        [Test]
        public void AddFont_FileNotFound()
        {
            var font = Path.Combine(TestFiles.ExternalFontsDirectory, "PdfToSvgTTF-Installable-NOTEXISTING.ttf");
            var repo = new FontRepository();

            Assert.Throws<FileNotFoundException>(() => repo.AddFont(font));
        }

        [Test]
        public void AddFont_ReadOnly()
        {
            var font = Path.Combine(TestFiles.ExternalFontsDirectory, "PdfToSvgTTF-Installable.ttf");
            DoesNotModifyReadOnlyRepository(repo => repo.AddFont(font));
        }

        [Test]
        public void AddDirectory()
        {
            var repo = new FontRepository();

            repo.AddDirectory(TestFiles.ExternalFontsDirectory);

            Assert.AreEqual(13, repo.Count);
        }

        [Test]
        public void AddDirectory_NullPath()
        {
            var repo = new FontRepository();

            Assert.Throws<ArgumentNullException>(() => repo.AddDirectory(null!));
        }

        [Test]
        public void AddDirectory_EmptyPath()
        {
            var repo = new FontRepository();

            Assert.Throws<ArgumentException>(() => repo.AddDirectory(""));
        }

        [Test]
        public void AddDirectory_DirectoryNotFound()
        {
            var dir = Path.Combine(TestFiles.ExternalFontsDirectory, "NOTEXISTING");
            var repo = new FontRepository();

            Assert.Throws<DirectoryNotFoundException>(() => repo.AddDirectory(dir));
        }

        [Test]
        public void AddDirectory_ReadOnly()
        {
            DoesNotModifyReadOnlyRepository(repo => repo.AddDirectory(TestFiles.ExternalFontsDirectory));
        }

        [Test]
        public void GetFont_Found()
        {
            var repo = new FontRepository();
            repo.AddDirectory(TestFiles.ExternalFontsDirectory);

            var font = repo.GetFont("PdfToSvgTTF-Installable-en");

            Assert.IsNotNull(font);
        }

        [Test]
        public void GetFont_NotFound()
        {
            var repo = new FontRepository();
            repo.AddDirectory(TestFiles.ExternalFontsDirectory);

            var font = repo.GetFont("PdfToSvgTTF-Installable-NOT-EXISTING");

            Assert.IsNull(font);
        }

#if !NET40
        [Test]
        public async Task GetFontAsync_Found()
        {
            var repo = new FontRepository();
            repo.AddDirectory(TestFiles.ExternalFontsDirectory);

            var font = await repo.GetFontAsync("PdfToSvgTTF-Installable-en");

            Assert.IsNotNull(font);
        }

        [Test]
        public async Task GetFontAsync_NotFound()
        {
            var repo = new FontRepository();
            repo.AddDirectory(TestFiles.ExternalFontsDirectory);

            var font = await repo.GetFontAsync("PdfToSvgTTF-Installable-NOT-EXISTING");

            Assert.IsNull(font);
        }
#endif

        [Test]
        public void Clear_ReadOnly()
        {
            DoesNotModifyReadOnlyRepository(repo => repo.Clear());
        }
    }
}
