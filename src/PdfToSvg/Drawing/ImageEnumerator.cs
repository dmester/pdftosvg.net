// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing.Patterns;
using PdfToSvg.Imaging;
using PdfToSvg.IO;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class ImageEnumerator
    {
        private const int MaxDepth = 10;

        private readonly Action permissionAssert;
        private readonly CancellationToken cancellationToken;

        public ImageEnumerator(Action permissionAssert, CancellationToken cancellationToken)
        {
            this.permissionAssert = permissionAssert;
            this.cancellationToken = cancellationToken;
        }

        private class AccessControlledImage : Image
        {
            private readonly Image baseImage;
            private readonly Action permissionAssert;

            public AccessControlledImage(Image baseImage, Action permissionAssert) :
                base(baseImage.ContentType, baseImage.Extension, baseImage.Width, baseImage.Height)
            {
                this.baseImage = baseImage;
                this.permissionAssert = permissionAssert;
            }

            public override bool Equals(object? obj) =>
                obj is AccessControlledImage image &&
                image.baseImage.Equals(baseImage);

            public override int GetHashCode() => 456648984 ^ baseImage.GetHashCode();

            public override byte[] GetContent(CancellationToken cancellationToken = default)
            {
                permissionAssert();
                return baseImage.GetContent(cancellationToken);
            }

#if HAVE_ASYNC
            public override Task<byte[]> GetContentAsync(CancellationToken cancellationToken = default)
            {
                permissionAssert();
                return baseImage.GetContentAsync(cancellationToken);
            }
#endif
        }

#if HAVE_ASYNC
        public async Task<List<Image>> ToListAsync(IEnumerable<PdfDictionary> pageDicts)
        {
            var images = new HashSet<Image>();

            foreach (var pageDict in pageDicts)
            {
                var content = await ContentStream.CombineAsync(pageDict, cancellationToken).ConfigureAwait(false);
                await VisitContentAsync(images, pageDict, content, depth: 0).ConfigureAwait(false);
            }

            return images.ToList();
        }

        private async Task VisitObjectAsync(HashSet<Image> images, PdfDictionary objectDict, int depth)
        {
            if (objectDict.Stream != null && depth <= MaxDepth)
            {
                ArraySegment<byte> content;

                using (var decodedStream = await objectDict.Stream.OpenDecodedAsync(cancellationToken).ConfigureAwait(false))
                {
                    content = await decodedStream.ToArraySegmentAsync().ConfigureAwait(false);
                }

                await VisitContentAsync(images, objectDict, content, depth).ConfigureAwait(false);
            }
        }

        private async Task VisitContentAsync(HashSet<Image> images, PdfDictionary ownerDict, ArraySegment<byte> content, int depth)
        {
            var handledGStates = new HashSet<PdfName>();
            var handledObjects = new HashSet<PdfName>();
            var handledPatterns = new HashSet<PdfName>();

            foreach (var op in ContentParser.Parse(content))
            {
                switch (op.Operator)
                {
                    case "gs":
                        if (TryGetGState(ownerDict, op, handledGStates, out var gstateDict))
                        {
                            await VisitObjectAsync(images, gstateDict, depth + 1).ConfigureAwait(false);
                        }
                        break;

                    case "Do":
                        if (TryGetXObject(ownerDict, op, handledObjects, out var xobjectDict, out var subtype))
                        {
                            if (subtype == Names.Form)
                            {
                                await VisitObjectAsync(images, xobjectDict, depth + 1).ConfigureAwait(false);
                            }
                            else if (subtype == Names.Image)
                            {
                                var image = await CreateImageAsync(xobjectDict, ownerDict).ConfigureAwait(false);
                                if (image != null)
                                {
                                    images.Add(image);
                                }
                            }
                        }
                        break;

                    case "SCN":
                    case "scn":
                        if (TryGetTiledPattern(ownerDict, op, handledPatterns, out var patternDict))
                        {
                            await VisitObjectAsync(images, patternDict, depth + 1).ConfigureAwait(false);
                        }
                        break;

                    case "BI":
                        var inlineImage = await CreateImageAsync(ownerDict, op).ConfigureAwait(false);
                        if (inlineImage != null)
                        {
                            images.Add(inlineImage);
                        }
                        break;
                }
            }
        }

        private Task<Image?> CreateImageAsync(PdfDictionary ownerDict, ContentOperation op)
        {
            if (op.Operands.Length == 1 &&
                op.Operands[0] is PdfDictionary imageDict)
            {
                imageDict[Names.Subtype] = Names.Image;

                return CreateImageAsync(imageDict, ownerDict);
            }

            return Task.FromResult<Image?>(null);
        }

        private async Task<Image?> CreateImageAsync(PdfDictionary imageDict, PdfDictionary ownerDict)
        {
            var colorSpace = GetColorSpace(imageDict, ownerDict);
            
            Image? image = null;

            try
            {
                image = await ImageFactory.CreateAsync(imageDict, colorSpace).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Initializing image failed with exception: " + ex);
            }

            if (image != null)
            {
                image = new AccessControlledImage(image, permissionAssert);
            }

            return image;
        }
#endif

#if HAVE_ASYNC_ENUMERABLE
        public async IAsyncEnumerable<Image> VisitPageAsync(PdfDictionary pageDict)
        {
            var content = await ContentStream.CombineAsync(pageDict, cancellationToken).ConfigureAwait(false);

            await foreach (var image in VisitContentAsync(pageDict, content, depth: 0).ConfigureAwait(false))
            {
                yield return image;
            }
        }

        private async IAsyncEnumerable<Image> VisitObjectAsync(PdfDictionary objectDict, int depth)
        {
            if (objectDict.Stream != null && depth <= MaxDepth)
            {
                ArraySegment<byte> content;

                // Important to not keep the file stream open when images are returned to the user, as it could cause deadlocks
                using (var decodedStream = await objectDict.Stream.OpenDecodedAsync(cancellationToken).ConfigureAwait(false))
                {
                    content = await decodedStream.ToArraySegmentAsync(cancellationToken).ConfigureAwait(false);
                }

                await foreach (var image in VisitContentAsync(objectDict, content, depth).ConfigureAwait(false))
                {
                    yield return image;
                }
            }
        }

        private async IAsyncEnumerable<Image> VisitContentAsync(PdfDictionary ownerDict, ArraySegment<byte> content, int depth)
        {
            var handledGStates = new HashSet<PdfName>();
            var handledObjects = new HashSet<PdfName>();
            var handledPatterns = new HashSet<PdfName>();

            foreach (var op in ContentParser.Parse(content))
            {
                switch (op.Operator)
                {
                    case "gs":
                        if (TryGetGState(ownerDict, op, handledGStates, out var gstateDict))
                        {
                            await foreach (var image in VisitObjectAsync(gstateDict, depth + 1).ConfigureAwait(false))
                            {
                                yield return image;
                            }
                        }
                        break;

                    case "Do":
                        if (TryGetXObject(ownerDict, op, handledObjects, out var xobjectDict, out var subtype))
                        {
                            if (subtype == Names.Form)
                            {
                                await foreach (var image in VisitObjectAsync(xobjectDict, depth + 1).ConfigureAwait(false))
                                {
                                    yield return image;
                                }
                            }
                            else if (subtype == Names.Image)
                            {
                                var image = await CreateImageAsync(xobjectDict, ownerDict).ConfigureAwait(false);
                                if (image != null)
                                {
                                    yield return image;
                                }
                            }
                        }
                        break;

                    case "SCN":
                    case "scn":
                        if (TryGetTiledPattern(ownerDict, op, handledPatterns, out var patternDict))
                        {
                            await foreach (var image in VisitObjectAsync(patternDict, depth + 1).ConfigureAwait(false))
                            {
                                yield return image;
                            }
                        }
                        break;

                    case "BI":
                    var inlineImage = await CreateImageAsync(ownerDict, op).ConfigureAwait(false);
                        if (inlineImage != null)
                        {
                            yield return inlineImage;
                        }
                        break;
                }
            }
        }
#endif

        public IEnumerable<Image> VisitPage(PdfDictionary pageDict)
        {
            var content = ContentStream.Combine(pageDict, cancellationToken);

            foreach (var image in VisitContent(pageDict, content, depth: 0))
            {
                yield return image;
            }
        }

        private IEnumerable<Image> VisitObject(PdfDictionary objectDict, int depth)
        {
            if (objectDict.Stream != null && depth <= MaxDepth)
            {
                ArraySegment<byte> content;

                // Important to not keep the file stream open when images are returned to the user, as it could cause deadlocks
                using (var decodedStream = objectDict.Stream.OpenDecoded(cancellationToken))
                {
                    content = decodedStream.ToArraySegment(cancellationToken);
                }

                foreach (var image in VisitContent(objectDict, content, depth))
                {
                    yield return image;
                }
            }
        }

        private IEnumerable<Image> VisitContent(PdfDictionary ownerDict, ArraySegment<byte> content, int depth)
        {
            var handledGStates = new HashSet<PdfName>();
            var handledObjects = new HashSet<PdfName>();
            var handledPatterns = new HashSet<PdfName>();

            foreach (var op in ContentParser.Parse(content))
            {
                switch (op.Operator)
                {
                    case "gs":
                        if (TryGetGState(ownerDict, op, handledGStates, out var gstateDict))
                        {
                            foreach (var image in VisitObject(gstateDict, depth + 1))
                            {
                                yield return image;
                            }
                        }
                        break;

                    case "Do":
                        if (TryGetXObject(ownerDict, op, handledObjects, out var xobjectDict, out var subtype))
                        {
                            if (subtype == Names.Form)
                            {
                                foreach (var image in VisitObject(xobjectDict, depth + 1))
                                {
                                    yield return image;
                                }
                            }
                            else if (subtype == Names.Image)
                            {
                                var image = CreateImage(xobjectDict, ownerDict);
                                if (image != null)
                                {
                                    yield return image;
                                }
                            }
                        }
                        break;

                    case "SCN":
                    case "scn":
                        if (TryGetTiledPattern(ownerDict, op, handledPatterns, out var patternDict))
                        {
                            foreach (var image in VisitObject(patternDict, depth + 1))
                            {
                                yield return image;
                            }
                        }
                        break;

                    case "BI":
                        var inlineImage = CreateImage(ownerDict, op);
                        if (inlineImage != null)
                        {
                            yield return inlineImage;
                        }
                        break;
                }
            }
        }

        private Image? CreateImage(PdfDictionary ownerDict, ContentOperation op)
        {
            if (op.Operands.Length == 1 &&
                op.Operands[0] is PdfDictionary imageDict)
            {
                imageDict[Names.Subtype] = Names.Image;
                return CreateImage(imageDict, ownerDict);
            }

            return null;
        }

        private Image? CreateImage(PdfDictionary imageDict, PdfDictionary ownerDict)
        {
            var colorSpace = GetColorSpace(imageDict, ownerDict);

            Image? image = null;
            try
            {
                image = ImageFactory.Create(imageDict, colorSpace);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Initializing image failed with exception: " + ex);
            }

            if (image != null)
            {
                image = new AccessControlledImage(image, permissionAssert);
            }

            return image;
        }

        private ColorSpace GetColorSpace(PdfDictionary imageDict, PdfDictionary ownerDict)
        {
            if (imageDict.GetValueOrDefault(Names.ImageMask, false))
            {
                return new IndexedColorSpace(new DeviceRgbColorSpace(),
                [
                    /* 0 */ 255, 255, 255,
                    /* 1 */ 0, 0, 0,
                ]);
            }
            else
            {
                return ColorSpaceParser.Parse(
                    imageDict[Names.ColorSpace],
                    ownerDict.GetDictionaryOrEmpty(Names.Resources / Names.ColorSpace),
                    cancellationToken);
            }
        }

        private bool TryGetGState(PdfDictionary ownerDict, ContentOperation op, HashSet<PdfName> handledGStates,
            [NotNullWhen(true)] out PdfDictionary? groupDict)
        {
            if (op.Operands.Length == 1 &&
                op.Operands[0] is PdfName gstateName &&
                handledGStates.Add(gstateName) &&
                ownerDict.TryGetDictionary(Names.Resources / Names.ExtGState / gstateName / Names.SMask / Names.G, out groupDict) &&
                groupDict.Stream != null)
            {
                return true;
            }

            groupDict = null;
            return false;
        }

        private bool TryGetTiledPattern(PdfDictionary ownerDict, ContentOperation op, HashSet<PdfName> handledPatterns,
            [NotNullWhen(true)] out PdfDictionary? patternDict)
        {
            if (op.Operands.LastOrDefault() is PdfName patternName &&
                handledPatterns.Add(patternName) &&
                ownerDict.TryGetDictionary(Names.Resources / Names.Pattern / patternName, out patternDict) &&
                patternDict.GetValueOrDefault(Names.PatternType, 0) == (int)PatternType.Tiling &&
                patternDict.Stream != null)
            {
                return true;
            }

            patternDict = null;
            return false;
        }

        private bool TryGetXObject(PdfDictionary ownerDict, ContentOperation op, HashSet<PdfName> handledObjects,
            [NotNullWhen(true)] out PdfDictionary? xobject, [NotNullWhen(true)] out PdfName? subtype)
        {
            if (op.Operands.Length == 1 &&
                op.Operands[0] is PdfName xobjectName &&
                handledObjects.Add(xobjectName) &&
                ownerDict.TryGetDictionary(Names.Resources / Names.XObject / xobjectName, out xobject) &&
                xobject.TryGetName(Names.Subtype, out subtype) &&
                xobject.Stream != null)
            {
                return true;
            }

            xobject = null;
            subtype = null;
            return false;
        }

    }
}
