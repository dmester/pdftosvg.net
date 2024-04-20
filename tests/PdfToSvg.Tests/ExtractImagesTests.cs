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
using static System.Net.Mime.MediaTypeNames;

namespace PdfToSvg.Tests
{
    internal class ExtractImagesTests
    {
        //
        // Regarding the test file:
        //
        // Contains images of different sizes, used to identify exported images without having to compare the actual
        // image data.
        //
        // * 20px = Inline image
        // * 30px = Standalone image accessed with `Do`
        // * 40px = Referenced in form
        // * 50px = Referenced in tiling pattern
        // * 60px = Shared image used as standalone image, in a pattern and in a form. Should only be exported once
        // * 70px = Included in resource dicts, but not invoked from content stream. Should not be exported
        // * 80px = Soft mask
        //

        private static string GetProtectedInputFilePath(string fileName)
        {
            return Path.Combine(TestFiles.ProtectedInputDirectory, fileName);
        }

        private static string GetInputFilePath(string fileName)
        {
            return Path.Combine(TestFiles.InputDirectory, fileName);
        }

        [Test]
        public void PdfDocument_Images_Enumerable_Protected_NotAllowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath);
            var forbiddenImages = 0;

            foreach (var image in doc.Images)
            {
                Assert.Throws<PermissionException>(() => image.GetContent());
                forbiddenImages++;
            }

            Assert.AreEqual(6, forbiddenImages);
        }

        [Test]
        public void PdfPage_Images_Enumerable_Protected_NotAllowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath);
            var page = doc.Pages[0];
            var forbiddenImages = 0;

            foreach (var image in page.Images)
            {
                Assert.Throws<PermissionException>(() => image.GetContent());
                forbiddenImages++;
            }

            Assert.AreEqual(6, forbiddenImages);
        }

        [Test]
        public void PdfDocument_Images_Enumerable_Protected_Allowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath, new OpenOptions { Password = "owner" });
            var extractedImages = 0;

            foreach (var image in doc.Images) 
            {
                image.GetContent();
                extractedImages++;
            }

            Assert.AreEqual(6, extractedImages);
        }

        [Test]
        public void PdfPage_Images_Enumerable_Protected_Allowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath, new OpenOptions { Password = "owner" });
            var page = doc.Pages[0];
            var extractedImages = 0;

            foreach (var image in page.Images)
            {
                image.GetContent();
                extractedImages++;
            }

            Assert.AreEqual(6, extractedImages);
        }

        [Test]
        public void PdfDocument_Images_Enumerable_Unprotected()
        {
            var pdfPath = GetInputFilePath("images-for-extract.pdf");
            using var doc = PdfDocument.Open(pdfPath);

            var sizes = string.Join(", ", doc.Images.Select(image => image.Width).OrderBy(x => x));

            // The 70px image is not referenced
            Assert.AreEqual("20, 30, 40, 50, 60, 80", sizes);
        }

        [Test]
        public void PdfPage_Images_Enumerable_Unprotected()
        {
            var pdfPath = GetInputFilePath("images-for-extract.pdf");
            using var doc = PdfDocument.Open(pdfPath);
            var page = doc.Pages[0];

            var sizes = string.Join(", ", page.Images.Select(image => image.Width).OrderBy(x => x));

            // The 70px image is not referenced
            Assert.AreEqual("20, 30, 40, 50, 60, 80", sizes);
        }

#if !NET40

        [Test]
        public async Task PdfDocument_Images_ToListAsync_Protected_NotAllowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath);
            var forbiddenImages = 0;

            foreach (var image in await doc.Images.ToListAsync())
            {
                Assert.ThrowsAsync<PermissionException>(() => image.GetContentAsync());
                forbiddenImages++;
            }

            Assert.AreEqual(6, forbiddenImages);
        }

        [Test]
        public async Task PdfDocument_Images_ToListAsync_Protected_Allowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath, new OpenOptions { Password = "owner" });
            var extractedImages = 0;

            foreach (var image in await doc.Images.ToListAsync())
            {
                await image.GetContentAsync();
                extractedImages++;
            }

            Assert.AreEqual(6, extractedImages);
        }

        [Test]
        public async Task PdfPage_Images_ToListAsync_Protected_NotAllowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath);
            var page = doc.Pages[0];
            var forbiddenImages = 0;

            foreach (var image in await page.Images.ToListAsync())
            {
                Assert.ThrowsAsync<PermissionException>(() => image.GetContentAsync());
                forbiddenImages++;
            }

            Assert.AreEqual(6, forbiddenImages);
        }

        [Test]
        public async Task PdfPage_Images_ToListAsync_Protected_Allowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath, new OpenOptions { Password = "owner" });
            var page = doc.Pages[0];
            var extractedImages = 0;

            foreach (var image in await page.Images.ToListAsync())
            {
                await image.GetContentAsync();
                extractedImages++;
            }

            Assert.AreEqual(6, extractedImages);
        }

        [Test]
        public async Task PdfDocument_Images_ToListAsync_Unprotected()
        {
            var pdfPath = GetInputFilePath("images-for-extract.pdf");
            using var doc = await PdfDocument.OpenAsync(pdfPath);

            var images = await doc.Images.ToListAsync();
            var sizes = string.Join(", ", images.Select(image => image.Width).OrderBy(x => x));

            // The 70px image is not referenced
            Assert.AreEqual("20, 30, 40, 50, 60, 80", string.Join(", ", sizes));
        }

        [Test]
        public async Task PdfPage_Images_ToListAsync_Unprotected()
        {
            var pdfPath = GetInputFilePath("images-for-extract.pdf");
            using var doc = await PdfDocument.OpenAsync(pdfPath);
            var page = doc.Pages[0];

            var images = await page.Images.ToListAsync();
            var sizes = string.Join(", ", images.Select(image => image.Width).OrderBy(x => x));

            // The 70px image is not referenced
            Assert.AreEqual("20, 30, 40, 50, 60, 80", string.Join(", ", sizes));
        }

#endif

#if !NETFRAMEWORK && !NETCOREAPP2_1

        [Test]
        public async Task PdfDocument_Images_AsyncEnumerable_Protected_NotAllowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath);
            var forbiddenImages = 0;

            await foreach (var image in doc.Images)
            {
                Assert.ThrowsAsync<PermissionException>(() => image.GetContentAsync());
                forbiddenImages++;
            }

            Assert.AreEqual(6, forbiddenImages);
        }

        [Test]
        public async Task PdfDocument_Images_AsyncEnumerable_Protected_Allowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath, new OpenOptions { Password = "owner" });
            var extractedImages = 0;

            await foreach (var image in doc.Images)
            {
                await image.GetContentAsync();
                extractedImages++;
            }

            Assert.AreEqual(6, extractedImages);
        }

        [Test]
        public async Task PdfPage_Images_AsyncEnumerable_Protected_NotAllowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath);
            var page = doc.Pages[0];
            var forbiddenImages = 0;

            await foreach (var image in page.Images)
            {
                Assert.ThrowsAsync<PermissionException>(() => image.GetContentAsync());
                forbiddenImages++;
            }

            Assert.AreEqual(6, forbiddenImages);
        }

        [Test]
        public async Task PdfPage_Images_AsyncEnumerable_Protected_Allowed()
        {
            var pdfPath = GetProtectedInputFilePath("protected-40-images-noextract.pdf");
            using var doc = PdfDocument.Open(pdfPath, new OpenOptions { Password = "owner" });
            var page = doc.Pages[0];
            var extractedImages = 0;

            await foreach (var image in page.Images)
            {
                await image.GetContentAsync();
                extractedImages++;
            }

            Assert.AreEqual(6, extractedImages);
        }

        [Test]
        public async Task PdfDocument_Images_AsyncEnumerable_Unprotected()
        {
            var pdfPath = GetInputFilePath("images-for-extract.pdf");
            using var doc = await PdfDocument.OpenAsync(pdfPath);

            var sizes = new List<int>();
            await foreach (var image in doc.Images)
            {
                sizes.Add(image.Width);
            }
            sizes.Sort();

            // The 70px image is not referenced
            Assert.AreEqual("20, 30, 40, 50, 60, 80", string.Join(", ", sizes));
        }

        [Test]
        public async Task PdfPage_Images_AsyncEnumerable_Unprotected()
        {
            var pdfPath = GetInputFilePath("images-for-extract.pdf");
            using var doc = await PdfDocument.OpenAsync(pdfPath);
            var page = doc.Pages[0];

            var sizes = new List<int>();
            await foreach (var image in page.Images)
            {
                sizes.Add(image.Width);
            }
            sizes.Sort();
            
            // The 70px image is not referenced
            Assert.AreEqual("20, 30, 40, 50, 60, 80", string.Join(", ", sizes));
        }

#endif
    }
}
