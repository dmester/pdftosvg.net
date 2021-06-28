// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing.Paths;
using PdfToSvg.Fonts;
using PdfToSvg.Imaging;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

#pragma warning disable IDE1006
#pragma warning disable IDE0051

namespace PdfToSvg.Drawing
{
    internal class SvgRenderer
    {
        private static readonly XNamespace ns = "http://www.w3.org/2000/svg";

        private const string LinkStyle = "a:active path{fill:#ffe4002e;}";
        private const string BrokenImageSymbolId = "pdftosvg_brokenimg";

        private GraphicsState graphicsState = new GraphicsState();

        private Stack<GraphicsState> graphicsStateStack = new Stack<GraphicsState>();

        private XElement svg;
        private XElement rootGraphics;

        private bool svgHasDefaultMiterLimit;

        private PathData currentPath = new PathData();

        private double currentPointX, currentPointY;

        private ResourceCache resources;

        private XElement defs = new XElement(ns + "defs");
        private bool hasBrokenImage = false;

        private HashSet<string> defIds = new HashSet<string>();

        private XElement? clipWrapper;
        private string? clipWrapperId;

        private static readonly OperationDispatcher dispatcher = new OperationDispatcher(typeof(SvgRenderer));

        private XElement style = new XElement(ns + "style", "text{white-space:pre;}");
        private HashSet<string> styleClassIds = new HashSet<string>();
        private HashSet<string> fontFaceNames = new HashSet<string>();

        private TextBuilder textBuilder;

        private readonly PdfDictionary pageDict;
        private readonly SvgConversionOptions options;
        private readonly CancellationToken cancellationToken;
        private readonly Matrix originalTransform;

        private Dictionary<string, ClipPath> clipPaths = new Dictionary<string, ClipPath>();

        private SvgRenderer(PdfDictionary pageDict, SvgConversionOptions? options, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                options = new SvgConversionOptions();
            }

            this.options = options;
            this.pageDict = pageDict;
            this.cancellationToken = cancellationToken;

            textBuilder = new TextBuilder(
                minSpaceEm: options.KerningThreshold,
                minSpacePx: 0.001 // Lower space will be rounded to "0" in SVG formatting.
                );

            resources = new ResourceCache(pageDict.GetDictionaryOrEmpty(Names.Resources));

            Rectangle cropBox;

            if (!pageDict.TryGetRectangle(Names.CropBox, out cropBox) &&
                !pageDict.TryGetRectangle(Names.MediaBox, out cropBox))
            {
                // Default to A4
                cropBox = RectangleUtils.GetA4();
            }

            var pageWidth = cropBox.Width;
            var pageHeight = cropBox.Height;

            rootGraphics = new XElement(ns + "g");

            if (pageDict.TryGetInteger(Names.Rotate, out var rotate))
            {
                rotate = (((rotate / 90) % 4) + 4) % 4;

                if (rotate % 2 == 1)
                {
                    pageWidth = cropBox.Height;
                    pageHeight = cropBox.Width;
                }

                // The page transform is applied on the root element instead of on the graphics transform, to avoid
                // a matrix being applied to all text elements, which are most probably rendered horizontally.
                var rootTransform =
                    Matrix.Translate(-cropBox.Width / 2, -cropBox.Height / 2) *
                    Matrix.Rotate(-Math.PI * 0.5 * rotate) *
                    Matrix.Translate(pageWidth / 2, pageHeight / 2);

                if (!rootTransform.IsIdentity)
                {
                    rootGraphics.Add(new XAttribute("transform", SvgConversion.Matrix(rootTransform)));
                }
            }

            svg = new XElement(ns + "svg",
                new XAttribute("width", pageWidth.ToString("0", CultureInfo.InvariantCulture)),
                new XAttribute("height", pageHeight.ToString("0", CultureInfo.InvariantCulture)),
                new XAttribute("preserveAspectRatio", "xMidYMid meet"),
                new XAttribute("viewBox",
                    string.Format(CultureInfo.InvariantCulture, "0 0 {0:0.####} {1:0.####}",
                    pageWidth, pageHeight
                )),
                style,
                defs,
                rootGraphics);

            // PDF coordinate system has its origin in the bottom left corner in opposite to SVG, 
            // which has its origin in the upper left corner.
            graphicsState.Transform = Matrix.Translate(0, -cropBox.Height, Matrix.Scale(1, -1));

            // Move origin
            graphicsState.Transform = Matrix.Translate(-cropBox.X1, -cropBox.Y1, graphicsState.Transform);

            originalTransform = graphicsState.Transform;
        }

        private void Convert(Stream contentStream)
        {
            // TODO For debugging, remove once no longer needed
            // var reader = new StreamReader(contentStream);
            // var content = reader.ReadToEnd();

            foreach (var op in ContentParser.Parse(contentStream))
            {
                cancellationToken.ThrowIfCancellationRequested();
                dispatcher.Dispatch(this, op.Operator, op.Operands);
            }

            SvgAttributeOptimizer.Optimize(rootGraphics);

            AddClipPaths(clipPaths.Values);

            if (options.IncludeLinks)
            {
                AddHyperlinks();
            }

            if (!defs.HasElements)
            {
                defs.Remove();
            }
        }

        private void AddHyperlinks()
        {
            if (pageDict.TryGetArray<PdfDictionary>(Names.Annots, out var annots))
            {
                var hasLink = false;

                foreach (var annot in annots)
                {
                    // PDF spec 1.7, Table 173
                    if (annot.GetNameOrNull(Names.Subtype) == Names.Link &&

                        annot.TryGetArray<double>(Names.Rect, out var arrRect) &&
                        arrRect.Length == 4 &&

                        annot.TryGetDictionary(Names.A, out var action) &&
                        action.GetNameOrNull(Names.S) == Names.URI &&
                        action.TryGetValue<PdfString>(Names.URI, out var uri))
                    {
                        var rect = new Rectangle(arrRect[0], arrRect[1], arrRect[2], arrRect[3]);
                        var path = new PathData();

                        if (annot.TryGetArray<double>(Names.QuadPoints, out var quadPoints) &&
                            quadPoints.Length >= 8)
                        {
                            var isValid = true;

                            // The QuadPoints should be ignored according to the spec if any of the points is outside Rect
                            for (var i = 0; isValid && i + 1 < quadPoints.Length; i += 2)
                            {
                                if (!rect.Contains(quadPoints[i], quadPoints[i + 1]))
                                {
                                    isValid = false;
                                }
                            }

                            if (isValid)
                            {
                                for (var i = 0; i + 7 < quadPoints.Length; i += 8)
                                {
                                    path.MoveTo(quadPoints[i], quadPoints[i + 1]);

                                    for (var offset = 2; offset < 8; offset += 2)
                                    {
                                        path.LineTo(quadPoints[i + offset], quadPoints[i + offset + 1]);
                                    }
                                }
                            }
                        }

                        if (path.Count == 0)
                        {
                            path.MoveTo(rect.X1, rect.Y1);
                            path.LineTo(rect.X2, rect.Y1);
                            path.LineTo(rect.X2, rect.Y2);
                            path.LineTo(rect.X1, rect.Y2);
                        }

                        path = path.Transform(originalTransform);

                        rootGraphics.Add(new XElement(ns + "a",
                            new XAttribute("href", uri.ToString()),
                            new XElement(ns + "path",
                                new XAttribute("d", SvgConversion.PathData(path)),
                                new XAttribute("fill", "transparent"))
                            ));

                        hasLink = true;
                    }
                }

                if (hasLink)
                {
                    style.Add(LinkStyle);
                }
            }
        }

        private void AddClipPaths(IEnumerable<ClipPath> paths)
        {
            foreach (var path in paths)
            {
                if (path.Referenced && defIds.Add(path.Id))
                {
                    XElement content;

                    if (path.IsRectangle)
                    {
                        content = new XElement(ns + "rect",
                            new XAttribute("x", SvgConversion.FormatCoordinate(path.Rectangle.X1)),
                            new XAttribute("y", SvgConversion.FormatCoordinate(path.Rectangle.Y1)),
                            new XAttribute("width", SvgConversion.FormatCoordinate(path.Rectangle.Width)),
                            new XAttribute("height", SvgConversion.FormatCoordinate(path.Rectangle.Height)));
                    }
                    else
                    {
                        content = new XElement(ns + "path",
                            new XAttribute("d", SvgConversion.PathData(path.Data)),
                            path.EvenOdd ? new XAttribute("fill-rule", "evenodd") : null);
                    }

                    defs.Add(new XElement(ns + "clipPath",
                        new XAttribute("id", path.Id),
                        path.Parent != null ? new XAttribute("clip-path", "url(#" + path.Parent.Id + ")") : null,
                        content));

                    AddClipPaths(path.Children.Values);
                }
            }
        }

        public static XElement Convert(PdfDictionary pageDict, SvgConversionOptions? options, CancellationToken cancellationToken)
        {
            var renderer = new SvgRenderer(pageDict, options, cancellationToken);
            var contentStream = ContentStream.Combine(pageDict, cancellationToken);
            renderer.Convert(contentStream);
            return renderer.svg;
        }

        public static async Task<XElement> ConvertAsync(PdfDictionary pageDict, SvgConversionOptions? options, CancellationToken cancellationToken)
        {
            var renderer = new SvgRenderer(pageDict, options, cancellationToken);
            var contentStream = await ContentStream.CombineAsync(pageDict, cancellationToken).ConfigureAwait(false);
            renderer.Convert(contentStream);
            return renderer.svg;
        }


        #region Graphics state operators

        [Operation("q")]
        private void q_SaveState()
        {
            graphicsStateStack.Push(graphicsState);
            graphicsState = graphicsState.Clone();
        }

        [Operation("Q")]
        private void Q_RestoreState()
        {
            if (graphicsStateStack.Count > 0)
            {
                graphicsState = graphicsStateStack.Pop();
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("cm")]
        private void cm_SetMatrix(double a, double b, double c, double d, double e, double f)
        {
            graphicsState.Transform = new Matrix(a, b, c, d, e, f) * graphicsState.Transform;
        }

        [Operation("w")]
        [Operation("gs/LW")]
        private void w_LineWidth(double lineWidth)
        {
            graphicsState.StrokeWidth = lineWidth;
        }

        [Operation("J")]
        [Operation("gs/LC")]
        private void J_LineCap(int lineCap)
        {
            graphicsState.StrokeLineCap = lineCap;
        }

        [Operation("j")]
        [Operation("gs/LJ")]
        private void j_LineJoin(int lineJoin)
        {
            graphicsState.StrokeLineJoin = lineJoin;
        }

        [Operation("M")]
        [Operation("gs/ML")]
        private void M_MiterLimit(double miterLimit)
        {
            graphicsState.StrokeMiterLimit = miterLimit;
        }

        [Operation("d")]
        private void d_DashArray(int[] dashArray, int dashPhase)
        {
            graphicsState.StrokeDashArray = dashArray;
            graphicsState.StrokeDashPhase = dashPhase;
        }

        [Operation("gs/D")]
        private void gs_D_DashArray(object[] args)
        {
            dispatcher.Dispatch(this, "d", args);
        }

        [Operation("gs")]
        private void gs_GraphicsStateFromDictionary(PdfName dictName)
        {
            if (resources.Dictionary.TryGetDictionary(Names.ExtGState / dictName, out var extGState))
            {
                foreach (var state in extGState)
                {
                    dispatcher.Dispatch(this, "gs" + state.Key, new[] { state.Value });
                }
            }
        }

        #endregion

        #region XObject operators

        private string? GetSvgImageId(PdfDictionary imageObject, out int width, out int height)
        {
            if (imageObject.Stream != null &&
                imageObject.TryGetName(Names.Subtype, out var subtype) && subtype == Names.Image &&
                imageObject.TryGetInteger(Names.Width, out width) &&
                imageObject.TryGetInteger(Names.Height, out height))
            {
                ColorSpace colorSpace;

                if (imageObject.GetValueOrDefault(Names.ImageMask, false))
                {
                    // PDF spec 1.7, Table 89
                    // Unmasked pixels should be rendered using the stroking color

                    static byte FloatToByte(float floatValue)
                    {
                        return (byte)MathUtils.Clamp(unchecked((int)(floatValue * 255f)), 0, 255);
                    }

                    var strokeColor = graphicsState.StrokeColor;

                    var colorLookup = new byte[]
                    {
                        /* 0 */ FloatToByte(strokeColor.Red), FloatToByte(strokeColor.Green), FloatToByte(strokeColor.Blue),
                        /* 1 */ 255, 255, 255
                    };

                    colorSpace = new IndexedColorSpace(new DeviceRgbColorSpace(), colorLookup);
                }
                else
                {
                    colorSpace = GetColorSpace(imageObject[Names.ColorSpace]);
                }

                if (colorSpace is UnsupportedColorSpace)
                {
                    return null;
                }

                var image = ImageFactory.Create(imageObject, colorSpace);
                if (image != null)
                {
                    // TODO add Dictinary<Image, string> to ensure stable ids.
                    // The key should be a combination of stroking color, image id, and some other data if it is an inline image.

                    var imageResolver = options.ImageResolver ?? new DataUriImageResolver();
                    var imageUrl = imageResolver.ResolveImageUrl(image, cancellationToken);
                    var imageId = StableID.Generate("im", imageUrl);

                    if (defIds.Add(imageId))
                    {
                        var svgImage = new XElement(ns + "image");

                        svgImage.SetAttributeValue("id", imageId);
                        svgImage.SetAttributeValue("href", imageUrl);
                        svgImage.SetAttributeValue("width", "1");
                        svgImage.SetAttributeValue("height", "1");
                        svgImage.SetAttributeValue("preserveAspectRatio", "none");

                        if (imageObject.GetValueOrDefault(Names.Interpolate, false) == false)
                        {
                            // Only respect disabling of interpolation for upscaled images.
                            // Most readers does not seem to consider the /Interpolate option for downscaled images.
                            graphicsState.Transform.DecomposeScaleXY(out var transformScaleX, out var transformScaleY);

                            if (transformScaleX > width * 4 && transformScaleY > height * 4)
                            {
                                svgImage.SetAttributeValue("image-rendering", "pixelated");
                            }
                        }

                        defs.Add(svgImage);
                    }

                    return imageId;
                }
            }

            width = 0;
            height = 0;
            return null;
        }

        private void RenderForm(PdfDictionary xobject)
        {
            if (xobject.Stream != null)
            {
                q_SaveState();

                var previousResources = resources;

                resources = new ResourceCache(xobject.GetDictionaryOrEmpty(Names.Resources));

                if (xobject.TryGetArray<double>(Names.Matrix, out var matrixArr) && matrixArr.Length == 6)
                {
                    var matrix = new Matrix(matrixArr[0], matrixArr[1], matrixArr[2], matrixArr[3], matrixArr[4], matrixArr[5]);
                    graphicsState.Transform = matrix * graphicsState.Transform;
                }

                if (xobject.TryGetArray<double>(Names.BBox, out var bbox) && bbox.Length == 4)
                {
                    re_Rectangle(bbox[0], bbox[1], bbox[2] - bbox[0], bbox[3] - bbox[1]);
                    W_Clip_NonZero();
                    n_EndPath();
                }

                // Buffer content since we might need to access the input file while rendering the page
                using var bufferedFormContent = new MemoryStream();
                using (var decodedFormContent = xobject.Stream.OpenDecoded(cancellationToken))
                {
                    decodedFormContent.CopyTo(bufferedFormContent);
                }
                bufferedFormContent.Position = 0;

                foreach (var operation in ContentParser.Parse(bufferedFormContent))
                {
                    dispatcher.Dispatch(this, operation.Operator, operation.Operands);
                }

                resources = previousResources;

                Q_RestoreState();
            }
        }

        private void RenderImage(PdfDictionary xobject)
        {
            var imageTransform = Matrix.Translate(0, -1) * Matrix.Scale(1, -1) * graphicsState.Transform;
            
            var imageId = GetSvgImageId(xobject, out var imageWidth, out var imageHeight);
            if (imageId == null)
            {
                // Missing image
                // Replace with broken image icon

                if (!hasBrokenImage)
                {
                    var brokenImageSymbol = BrokenImageSymbol.Create();
                    brokenImageSymbol.SetAttributeValue("id", BrokenImageSymbolId);
                    defs.AddAfterSelf(brokenImageSymbol);
                    hasBrokenImage = true;
                }

                AppendClipped(new XElement(ns + "g",
                    new XAttribute("transform", SvgConversion.Matrix(imageTransform)),
                    new XElement(ns + "rect",
                        new XAttribute("width", "1"),
                        new XAttribute("height", "1"),
                        new XAttribute("fill", "#7773")
                        ),
                    new XElement(ns + "use",
                        new XAttribute("x", "0.25"),
                        new XAttribute("y", "0.25"),
                        new XAttribute("width", "0.5"),
                        new XAttribute("height", "0.5"),
                        new XAttribute("href", "#" + BrokenImageSymbolId)
                        )
                    ));
                return;
            }

            var imageAttributes = new List<XAttribute>();

            // Positioning
            imageAttributes.Add(new XAttribute("transform", SvgConversion.Matrix(imageTransform)));

            // Mask
            if (xobject.TryGetDictionary(Names.SMask, out var smask))
            {
                var maskImageId = GetSvgImageId(smask, out var maskWidth, out var maskHeight);
                if (maskImageId != null)
                {
                    var maskId = StableID.Generate("m",
                        maskImageId + ";" +
                        imageWidth.ToString(CultureInfo.InvariantCulture) + ";" +
                        imageHeight.ToString(CultureInfo.InvariantCulture));

                    if (defIds.Add(maskId))
                    {
                        var use = new XElement(
                            ns + "use",
                            new XAttribute("href", "#" + maskImageId));

                        if (imageWidth != maskWidth || imageHeight != maskHeight)
                        {
                            var scaleX = (double)imageWidth / maskWidth;
                            var scaleY = (double)imageHeight / maskHeight;

                            use.Add(new XAttribute("transform", "scale(" +
                                SvgConversion.FormatCoordinate(scaleX) + " " +
                                SvgConversion.FormatCoordinate(scaleY) + ")"));
                        }

                        defs.Add(new XElement(
                            ns + "mask",
                            new XAttribute("id", maskId),
                            use));
                    }

                    imageAttributes.Add(new XAttribute("mask", "url(#" + maskId + ")"));
                }
            }

            AppendClipped(new XElement(ns + "g", imageAttributes,
                new XElement(ns + "use", new XAttribute("href", "#" + imageId))
                ));
        }

        [Operation("Do")]
        private void Do_InvokeObject(PdfName name)
        {
            if (resources.Dictionary.TryGetDictionary(Names.XObject / name, out var xobject) &&
                xobject.TryGetName(Names.Subtype, out var subtype))
            {
                if (subtype == Names.Image)
                {
                    RenderImage(xobject);
                }
                else if (subtype == Names.Form)
                {
                    RenderForm(xobject);
                }
            }
        }

        [Operation("BI")]
        private void BI_BeginImage(PdfDictionary imageDict)
        {
            imageDict[Names.Subtype] = Names.Image;

            RenderImage(imageDict);
        }

        #endregion

        #region Clipping path operators

        private void AppendClipping(bool evenOdd)
        {
            var path = currentPath.Transform(graphicsState.Transform);

            var clipPath = new ClipPath(path, evenOdd)
            {
                Parent = graphicsState.ClipPath,
            };

            if (PathConverter.TryConvertToRectangle(path, out var rect))
            {
                clipPath.Rectangle = rect;
                clipPath.IsRectangle = true;

                if (clipPath.Parent != null && clipPath.IsRectangle)
                {
                    clipPath.Rectangle = Rectangle.Intersection(rect, clipPath.Parent.Rectangle);
                    clipPath.Parent = clipPath.Parent.Parent;
                }

                clipPath.Id = StableID.Generate("cl",
                    clipPath.Parent?.Id,
                    "rect",
                    clipPath.Rectangle.X1,
                    clipPath.Rectangle.X2,
                    clipPath.Rectangle.Y1,
                    clipPath.Rectangle.Y2
                    );
            }
            else
            {
                clipPath.Id = StableID.Generate("cl",
                    clipPath.Parent?.Id,
                    evenOdd,
                    clipPath.Data);
            }

            var targetDictionary = clipPath.Parent?.Children ?? clipPaths;

            if (targetDictionary.TryGetValue(clipPath.Id, out var existing))
            {
                clipPath = existing;
            }
            else
            {
                targetDictionary[clipPath.Id] = clipPath;
            }

            graphicsState.ClipPath = clipPath;
        }

        [Operation("W")]
        private void W_Clip_NonZero()
        {
            AppendClipping(false);
        }

        [Operation("W*")]
        private void Wx_Clip_EvenOdd()
        {
            AppendClipping(true);
        }

        #endregion

        #region Path construction operators

        [Operation("m")]
        private void m_MoveTo(double x, double y)
        {
            currentPath.MoveTo(x, y);

            currentPointX = x;
            currentPointY = y;
        }

        [Operation("l")]
        private void l_LineTo(double x, double y)
        {
            currentPath.LineTo(x, y);

            currentPointX = x;
            currentPointY = y;
        }

        [Operation("c")]
        private void c_Bezier(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            currentPath.CurveTo(x1, y1, x2, y2, x3, y3);

            currentPointX = x3;
            currentPointY = y3;
        }

        [Operation("v")]
        private void v_Bezier(double x2, double y2, double x3, double y3)
        {
            currentPath.CurveTo(currentPointX, currentPointY, x2, y2, x3, y3);

            currentPointX = x3;
            currentPointY = y3;
        }

        [Operation("y")]
        private void y_Bezier(double x1, double y1, double x3, double y3)
        {
            currentPath.CurveTo(x1, y1, x3, y3, x3, y3);

            currentPointX = x3;
            currentPointY = y3;
        }

        [Operation("h")]
        private void h_ClosePath()
        {
            currentPath.ClosePath();
        }

        [Operation("re")]
        private void re_Rectangle(double x, double y, double width, double height)
        {
            currentPath.MoveTo(x, y);
            currentPath.LineTo(x + width, y);
            currentPath.LineTo(x + width, y + height);
            currentPath.LineTo(x, y + height);
            currentPath.ClosePath();

            currentPointX = x;
            currentPointY = y;
        }

        #endregion

        #region Path-Painting operators

        private XElement GetClipParent()
        {
            if (graphicsState.ClipPath == null)
            {
                clipWrapperId = null;
                clipWrapper = null;

                return rootGraphics;
            }
            else
            {
                if (clipWrapperId != graphicsState.ClipPath.Id)
                {
                    clipWrapper = new XElement(ns + "g", new XAttribute("clip-path", "url(#" + graphicsState.ClipPath.Id + ")"));
                    clipWrapperId = graphicsState.ClipPath.Id;
                    rootGraphics.Add(clipWrapper);

                    var cursor = graphicsState.ClipPath;
                    while (cursor != null && !cursor.Referenced)
                    {
                        cursor.Referenced = true;
                        cursor = cursor.Parent;
                    }
                }

                return clipWrapper!;
            }
        }

        private void AppendClipped(XElement el)
        {
            GetClipParent().Add(el);
        }

        private void DrawPath(bool stroke, bool fill, bool evenOddWinding)
        {
            var pathTransformed = false;

            graphicsState.Transform.DecomposeScaleXY(out var scaleX, out var scaleY);

            if (!stroke || scaleX == scaleY)
            {
                currentPath = currentPath.Transform(graphicsState.Transform);
                pathTransformed = true;
            }

            var contained =
                graphicsState.ClipPath != null &&
                graphicsState.ClipPath.Parent == null &&
                graphicsState.ClipPath.IsRectangle &&
                currentPath.All(cmd =>
                {
                    if (cmd is ClosePathCommand)
                    {
                        return true;
                    }

                    double x, y;

                    if (cmd is MoveToCommand move)
                    {
                        x = move.X;
                        y = move.Y;
                    }
                    else if (cmd is LineToCommand line)
                    {
                        x = line.X;
                        y = line.Y;
                    }
                    else
                    {
                        // Cannot determine if curves is within the clip rectangle
                        return false;
                    }

                    // TODO does not consider stroke widths
                    return graphicsState.ClipPath.Rectangle.Contains(x, y);
                });

            var pathString = SvgConversion.PathData(currentPath);
            currentPath = new PathData();

            var attributes = new List<object>();

            if (!pathTransformed && !graphicsState.Transform.IsIdentity)
            {
                attributes.Add(new XAttribute("transform", SvgConversion.Matrix(graphicsState.Transform)));
            }

            if (evenOddWinding)
            {
                attributes.Add(new XAttribute("fill-rule", "evenodd"));
            }

            if (fill)
            {
                attributes.Add(new XAttribute("fill", SvgConversion.FormatColor(graphicsState.FillColor)));
            }
            else
            {
                attributes.Add(new XAttribute("fill", "none"));
            }

            if (stroke)
            {
                var strokeWidth = graphicsState.StrokeWidth;
                var strokeWidthScale = pathTransformed ? 1d : Math.Min(scaleX, scaleY);

                if (strokeWidth == 0)
                {
                    // Zero width stroke should be rendered as the thinnest line that can be rendered.
                    // Since the SVG can be scaled, we will assume 0.5pt is the thinnest renderable line.
                    strokeWidth = 0.5d / strokeWidthScale;
                }
                else if (pathTransformed)
                {
                    strokeWidth *= scaleX;
                }

                if (strokeWidth * strokeWidthScale < options.MinStrokeWidth)
                {
                    strokeWidth = options.MinStrokeWidth / strokeWidthScale;
                }

                attributes.Add(new XAttribute("stroke", SvgConversion.FormatColor(graphicsState.StrokeColor)));
                attributes.Add(new XAttribute("stroke-width", SvgConversion.FormatCoordinate(strokeWidth)));

                if (graphicsState.StrokeLineCap == 1)
                {
                    attributes.Add(new XAttribute("stroke-linecap", "round"));
                }
                else if (graphicsState.StrokeLineCap == 2)
                {
                    attributes.Add(new XAttribute("stroke-linecap", "square"));
                }

                if (graphicsState.StrokeLineJoin == 1)
                {
                    attributes.Add(new XAttribute("stroke-linejoin", "round"));
                }
                else if (graphicsState.StrokeLineJoin == 2)
                {
                    attributes.Add(new XAttribute("stroke-linejoin", "bevel"));
                }
                else
                {
                    // Default to miter join

                    // Miter limit is applicable
                    // Default in SVG: 4
                    // Default in PDF: 10 (PDF 1.7 spec, Table 52)

                    if (graphicsState.StrokeMiterLimit != 10)
                    {
                        attributes.Add(new XAttribute("stroke-miterlimit", SvgConversion.FormatCoordinate(graphicsState.StrokeMiterLimit)));
                    }
                    else if (!svgHasDefaultMiterLimit)
                    {
                        // Change default miter limit on root element
                        rootGraphics.Add(new XAttribute("stroke-miterlimit", SvgConversion.FormatCoordinate(graphicsState.StrokeMiterLimit)));
                        svgHasDefaultMiterLimit = true;
                    }
                }

                if (graphicsState.StrokeDashArray != null)
                {
                    attributes.Add(new XAttribute("stroke-dasharray", string.Join(" ",
                        graphicsState.StrokeDashArray.Select(x => x.ToString(CultureInfo.InvariantCulture)))));

                    if (graphicsState.StrokeDashPhase != 0)
                    {
                        attributes.Add(new XAttribute("stroke-dashoffset",
                            graphicsState.StrokeDashPhase.ToString(CultureInfo.InvariantCulture)));
                    }
                }
            }

            var el = new XElement(
                ns + "path",
                new XAttribute("d", pathString.ToString()),
                attributes
                );
            if (graphicsState.ClipPath != null && contained)
            {
                clipWrapper = null;
                clipWrapperId = null;
                rootGraphics.Add(el);
            }
            else
            {
                AppendClipped(el);
            }
        }

        [Operation("S")]
        private void S_StrokePath()
        {
            DrawPath(true, false, false);
        }

        [Operation("s")]
        private void s_Close_StrokePath()
        {
            h_ClosePath();
            S_StrokePath();
        }

        [Operation("f")]
        private void f_Fill_NonZero()
        {
            DrawPath(false, true, false);
        }

        [Operation("F")]
        private void F_Fill_NonZero()
        {
            f_Fill_NonZero();
        }

        [Operation("f*")]
        private void fx_Fill_EvenOdd()
        {
            DrawPath(false, true, true);
        }

        [Operation("B")]
        private void B_FillStroke_NonZero()
        {
            DrawPath(true, true, false);
        }

        [Operation("B*")]
        private void Bx_FillStroke_EvenOdd()
        {
            DrawPath(true, true, true);
        }

        [Operation("b")]
        private void B_Close_FillStroke_NonZero()
        {
            h_ClosePath();
            B_FillStroke_NonZero();
        }

        [Operation("b*")]
        private void Bx_Close_FillStroke_EvenOdd()
        {
            h_ClosePath();
            Bx_FillStroke_EvenOdd();
        }

        [Operation("n")]
        private void n_EndPath()
        {
            currentPath = new PathData();
        }

        #endregion

        #region Color operators

        // PDF spec 1.7, Table 74, page 180

        private ColorSpace GetColorSpace(object? definition)
        {
            if (definition is PdfName name)
            {
                return resources.GetColorSpace(name, cancellationToken);
            }

            return ColorSpace.Parse(definition, resources.Dictionary.GetDictionaryOrNull(Names.ColorSpace), cancellationToken);
        }

        [Operation("CS")]
        private void CS_StrokeColorSpace(PdfName name)
        {
            graphicsState.StrokeColorSpace = GetColorSpace(name);
            graphicsState.StrokeColor = graphicsState.StrokeColorSpace.GetDefaultRgbColor();
            textBuilder.InvalidateStyle();
        }

        [Operation("cs")]
        private void cs_FillColorSpace(PdfName name)
        {
            graphicsState.FillColorSpace = GetColorSpace(name);
            graphicsState.FillColor = graphicsState.FillColorSpace.GetDefaultRgbColor();
            textBuilder.InvalidateStyle();
        }

        [Operation("SC")]
        public void SC_StrokeColor(params float[] components)
        {
            graphicsState.StrokeColor = new RgbColor(graphicsState.StrokeColorSpace, components);
            textBuilder.InvalidateStyle();
        }

        [Operation("sc")]
        public void sc_FillColor(params float[] components)
        {
            graphicsState.FillColor = new RgbColor(graphicsState.FillColorSpace, components);
            textBuilder.InvalidateStyle();
        }

        [Operation("SCN")]
        public void SCN_StrokeColor(params float[] components)
        {
            // TODO Should also accept a trailing name argument
            graphicsState.StrokeColor = new RgbColor(graphicsState.StrokeColorSpace, components);
            textBuilder.InvalidateStyle();
        }

        [Operation("scn")]
        public void scn_FillColor(params float[] components)
        {
            // TODO Should also accept a trailing name argument
            graphicsState.FillColor = new RgbColor(graphicsState.FillColorSpace, components);
            textBuilder.InvalidateStyle();
        }

        [Operation("G")]
        private void G_StrokeGray(float gray)
        {
            graphicsState.StrokeColorSpace = new DeviceGrayColorSpace();
            graphicsState.StrokeColor = new RgbColor(graphicsState.StrokeColorSpace, gray);
            textBuilder.InvalidateStyle();
        }

        [Operation("g")]
        private void g_FillGray(float gray)
        {
            graphicsState.FillColorSpace = new DeviceGrayColorSpace();
            graphicsState.FillColor = new RgbColor(graphicsState.FillColorSpace, gray);
            textBuilder.InvalidateStyle();
        }

        [Operation("RG")]
        private void RG_StrokeRgb(float r, float g, float b)
        {
            graphicsState.StrokeColorSpace = new DeviceRgbColorSpace();
            graphicsState.StrokeColor = new RgbColor(graphicsState.StrokeColorSpace, r, g, b);
            textBuilder.InvalidateStyle();
        }

        [Operation("rg")]
        private void rg_FillRgb(float r, float g, float b)
        {
            graphicsState.FillColorSpace = new DeviceRgbColorSpace();
            graphicsState.FillColor = new RgbColor(graphicsState.FillColorSpace, r, g, b);
            textBuilder.InvalidateStyle();
        }

        [Operation("K")]
        private void K_StrokeCmyk(float c, float m, float y, float k)
        {
            graphicsState.StrokeColorSpace = new DeviceCmykColorSpace();
            graphicsState.StrokeColor = new RgbColor(graphicsState.StrokeColorSpace, c, m, y, k);
            textBuilder.InvalidateStyle();
        }

        [Operation("k")]
        private void k_FillCmyk(float c, float m, float y, float k)
        {
            graphicsState.FillColorSpace = new DeviceCmykColorSpace();
            graphicsState.FillColor = new RgbColor(graphicsState.FillColorSpace, c, m, y, k);
            textBuilder.InvalidateStyle();
        }

        #endregion

        #region Text state operators

        [Operation("Tc")]
        private void Tc_CharSpace(double charSpace)
        {
            graphicsState.TextCharSpacingPx = charSpace;
            textBuilder.InvalidateStyle();
        }

        [Operation("Tw")]
        private void Tw_WordSpace(double wordSpace)
        {
            graphicsState.TextWordSpacingPx = wordSpace;
        }

        [Operation("Tz")]
        private void Tz_Scale(double scale)
        {
            graphicsState.TextScaling = scale;
            textBuilder.InvalidateStyle();
            textBuilder.UpdateLineMatrix(graphicsState);
        }

        [Operation("TL")]
        private void TL_Leading(double leading)
        {
            graphicsState.TextLeading = leading;
        }

        [Operation("Tf")]
        private void Tf_Font(PdfName fontName, double fontSize)
        {
            graphicsState.Font = resources.GetFont(fontName, options.FontResolver ?? DefaultFontResolver.Instance, cancellationToken) ?? InternalFont.Fallback;
            graphicsState.FontSize = fontSize;

            if (graphicsState.Font == null)
            {
                Log.WriteLine($"Could not find a font replacement for {fontName}.");
                graphicsState.Font = InternalFont.Fallback;
            }

            textBuilder.InvalidateStyle();
            textBuilder.UpdateLineMatrix(graphicsState);
        }

        [Operation("Tr")]
        private void Tr_Renderer(int renderer)
        {
            graphicsState.TextRenderingMode = renderer switch
            {
                0 => TextRenderingMode.Fill,
                1 => TextRenderingMode.Stroke,
                2 => TextRenderingMode.Fill | TextRenderingMode.Stroke,
                3 => TextRenderingMode.None,
                4 => TextRenderingMode.Fill | TextRenderingMode.Clip,
                5 => TextRenderingMode.Stroke | TextRenderingMode.Clip,
                6 => TextRenderingMode.Fill | TextRenderingMode.Stroke | TextRenderingMode.Clip,
                7 => TextRenderingMode.Clip,
                _ => TextRenderingMode.Fill,
            };
            textBuilder.InvalidateStyle();
        }

        [Operation("Ts")]
        private void Ts_Rise(double rise)
        {
            graphicsState.TextRisePx = rise;
            textBuilder.InvalidateStyle();
        }

        #endregion

        #region Text showing operators

        [Operation("Tj")]
        private void Tj_Show(PdfString text)
        {
            textBuilder.AddSpan(graphicsState, text);
        }

        [Operation("'")]
        private void MoveLine_Show(PdfString text)
        {
            Tx_StartOfLine();
            Tj_Show(text);
        }

        [Operation("\"")]
        private void MoveLine_Show(double wordSpacing, double charSpacing, PdfString text)
        {
            graphicsState.TextWordSpacingPx = wordSpacing;
            graphicsState.TextCharSpacingPx = charSpacing;
            textBuilder.InvalidateStyle();
            MoveLine_Show(text);
        }

        [Operation("TJ")]
        private void TJ_Show(object[] array)
        {
            foreach (var item in array)
            {
                if (item is int adjustXInt)
                {
                    textBuilder.AddSpace(graphicsState, adjustXInt);
                }
                else if (item is double adjustXReal)
                {
                    textBuilder.AddSpace(graphicsState, adjustXReal);
                }
                else if (item is PdfString text)
                {
                    textBuilder.AddSpan(graphicsState, text);
                }
            }
        }

        #endregion

        #region Text general

        [Operation("BT")]
        private void BT_BeginText()
        {
            graphicsState.TextMatrix = Matrix.Identity;
            graphicsState.LineMatrix = Matrix.Identity;
            textBuilder.Clear();
            textBuilder.UpdateLineMatrix(graphicsState);
        }

        private static List<TextParagraph> PrepareSvgSpans(List<TextParagraph> paragraphs, TextRenderingMode includedModes)
        {
            // Horizontal scaling is resalized using the textLength attribute, which ensures any stroke
            // is not affected by the scaling. However, the support for textLength is rather buggy in 
            // some browsers (looking at you, Chrome), so we need to ensure textLength is only used on
            // <text> elements without any child <tspan>.

            var result = new List<TextParagraph>(paragraphs.Count);

            foreach (var paragraph in paragraphs)
            {
                var x = paragraph.X;
                TextParagraph? newParagraph = null;

                foreach (var span in paragraph.Content)
                {
                    if ((span.Style.TextRenderingMode & includedModes) != 0)
                    {
                        if (newParagraph == null ||
                            newParagraph.Content.Last().Style.TextScaling != 100 ||
                            span.Style.TextScaling != 100)
                        {
                            newParagraph = new TextParagraph
                            {
                                Matrix = paragraph.Matrix,
                                X = x,
                                Y = paragraph.Y,
                            };
                            result.Add(newParagraph);
                        }

                        newParagraph.Content.Add(span);
                    }

                    x += span.Width + span.SpaceBefore;
                }
            }

            return result;
        }

        private string? BuildCssClass(GraphicsState style)
        {
            var className = default(string);
            var cssClass = new CssPropertyCollection();

            if (style.TextRenderingMode.HasFlag(TextRenderingMode.Fill))
            {
                if (style.FillColor != RgbColor.Black)
                {
                    cssClass["fill"] = SvgConversion.FormatColor(style.FillColor);
                }
            }
            else if (style.TextRenderingMode.HasFlag(TextRenderingMode.Stroke))
            {
                cssClass["fill"] = "none";
            }

            if (style.TextRenderingMode.HasFlag(TextRenderingMode.Stroke))
            {
                cssClass["stroke"] = SvgConversion.FormatColor(style.StrokeColor);
                // TODO specify width and other stroke related properties
            }

            if (style.Font.SubstituteFont is LocalFont localFont)
            {
                cssClass["font-family"] = localFont.FontFamily;
                cssClass["font-weight"] = localFont.FontWeight;
                cssClass["font-style"] = localFont.FontStyle;
            }
            else if (style.Font.SubstituteFont is WebFont webFont)
            {
                cssClass["font-family"] = webFont.FontFamily;

                if (fontFaceNames.Add(webFont.FontFamily))
                {
                    var sourceFonts = new[]
                    {
                        new { Format = "woff", Url = webFont.Woff2Url },
                        new { Format = "woff2", Url = webFont.Woff2Url },
                        new { Format = "truetype", Url = webFont.TrueTypeUrl },
                    };

                    var src = string.Join(",", sourceFonts
                        .Where(x => !string.IsNullOrEmpty(x.Url))
                        .Select(x => $"url('{x.Url}') format('{x.Format}')"));

                    var fontFace = new CssPropertyCollection
                    {
                        { "font-family", webFont.FontFamily },
                        { "src", src },
                    };

                    this.style.Add("@font-face{" + fontFace + "}");
                }
            }
            else
            {
                throw new Exception("Unexpected font type.");
            }

            cssClass["font-size"] = SvgConversion.FormatCoordinate(style.FontSize) + "px";

            if (style.TextCharSpacingPx != 0)
            {
                cssClass["letter-spacing"] = SvgConversion.FormatCoordinate(style.TextCharSpacingPx) + "px";
            }

            if (cssClass.Count > 0)
            {
                var styleString = cssClass.ToString();

                className = StableID.Generate("tx", styleString);

                if (styleClassIds.Add(className))
                {
                    this.style.Add("." + className + "{" + styleString + "}");
                }
            }

            return className;
        }

        [Operation("ET")]
        private void ET_EndText()
        {
            var styleToClassNameLookup = new Dictionary<object, string?>();

            foreach (var paragraph in PrepareSvgSpans(textBuilder.paragraphs, TextRenderingMode.Fill | TextRenderingMode.Stroke))
            {
                if (paragraph.Content.Count == 0)
                {
                    continue;
                }

                var singleSpan = paragraph.Content.Count == 1 ? paragraph.Content[0] : null;

                var textEl = new XElement(ns + "text");
                var paragraphWidth = 0.0;

                var x = paragraph.X;
                var y = paragraph.Y;

                if (singleSpan != null)
                {
                    x += singleSpan.SpaceBefore;
                    y += singleSpan.Style.TextRisePx;
                }

                if (paragraph.Matrix.IsIdentity)
                {
                    textEl.SetAttributeValue("x", SvgConversion.FormatCoordinate(x));
                    textEl.SetAttributeValue("y", SvgConversion.FormatCoordinate(y));
                }
                else
                {
                    textEl.SetAttributeValue("transform", SvgConversion.Matrix(Matrix.Translate(x, y, paragraph.Matrix)));
                }

                if (singleSpan != null)
                {
                    if (!styleToClassNameLookup.TryGetValue(singleSpan.Style, out string? className))
                    {
                        styleToClassNameLookup[singleSpan.Style] = className = BuildCssClass(singleSpan.Style);
                    }

                    if (className != null)
                    {
                        textEl.SetAttributeValue("class", className);
                    }

                    if (singleSpan.Style.TextScaling != 100)
                    {
                        textEl.SetAttributeValue("textLength", SvgConversion.FormatCoordinate(singleSpan.Width) + "px");
                        textEl.SetAttributeValue("lengthAdjust", "spacingAndGlyphs");
                    }

                    textEl.Value = singleSpan.Value;

                    paragraphWidth = singleSpan.Width;
                }
                else
                {
                    var currentYOffset = 0.0;

                    var classNames = new string?[paragraph.Content.Count];
                    var multipleClasses = false;

                    for (var i = 0; i < classNames.Length; i++)
                    {
                        var style = paragraph.Content[i].Style;

                        if (!styleToClassNameLookup.TryGetValue(style, out var className))
                        {
                            styleToClassNameLookup[style] = className = BuildCssClass(style);
                        }

                        classNames[i] = className;

                        if (i > 0 && className != classNames[i - 1])
                        {
                            multipleClasses = true;
                        }
                    }

                    if (!multipleClasses)
                    {
                        textEl.SetAttributeValue("class", classNames[0]);
                    }

                    for (var i = 0; i < classNames.Length; i++)
                    {
                        var span = paragraph.Content[i];
                        var tspan = new XElement(ns + "tspan");

                        if (multipleClasses)
                        {
                            tspan.SetAttributeValue("class", classNames[i]);
                        }

                        if (span.SpaceBefore != 0)
                        {
                            tspan.SetAttributeValue("dx", SvgConversion.FormatCoordinate(span.SpaceBefore));
                        }

                        if (span.Style.TextRisePx != currentYOffset)
                        {
                            tspan.SetAttributeValue("dy", SvgConversion.FormatCoordinate(currentYOffset - span.Style.TextRisePx));
                            currentYOffset = span.Style.TextRisePx;
                        }

                        tspan.Value = span.Value;
                        textEl.Add(tspan);

                        paragraphWidth += span.Width + span.SpaceBefore;
                    }
                }

                // TODO test simplification of clip path
                if (paragraph.Matrix.IsIdentity &&
                    graphicsState.ClipPath != null &&
                    graphicsState.ClipPath.Parent == null &&
                    graphicsState.ClipPath.IsRectangle)
                {
                    var maxFontSize = paragraph.Content.Max(span => span.Style.FontSize);

                    // This is an approximation of the text bounding rectangle. It is not entirely correct since
                    // we don't have all the font metrics to determine the height above and below the baseline.
                    var textBoundingRect = RectangleUtils.GetBoundingRectangleAfterTransform(
                        new Rectangle(
                            paragraph.X, paragraph.Y - maxFontSize,
                            paragraph.X + paragraphWidth, paragraph.Y + maxFontSize / 2
                        ),
                        paragraph.Matrix);

                    if (graphicsState.ClipPath.Rectangle.Contains(textBoundingRect))
                    {
                        // We can be reasonably sure the text is entiely contained within the clip rectangle.
                        // Skip clipping. This significally increases the print quality in Internet Explorer, 
                        // which seems to rasterize all clipped graphics before printing.
                        clipWrapper = null;
                        clipWrapperId = null;
                        rootGraphics.Add(textEl);
                        continue;
                    }
                }

                AppendClipped(textEl);
            }
        }

        #endregion

        #region Text positioning operators

        [Operation("Tm")]
        private void Tm_SetTextMatrix(double a, double b, double c, double d, double e, double f)
        {
            graphicsState.TextMatrix = graphicsState.LineMatrix = new Matrix(a, b, c, d, e, f);
            textBuilder.UpdateLineMatrix(graphicsState);
        }

        [Operation("T*")]
        private void Tx_StartOfLine()
        {
            Td(0, -graphicsState.TextLeading);
        }

        [Operation("Td")]
        private void Td(double tx, double ty)
        {
            graphicsState.TextMatrix = graphicsState.LineMatrix = Matrix.Translate(tx, ty, graphicsState.LineMatrix);
            textBuilder.UpdateLineMatrix(graphicsState);
        }

        [Operation("TD")]
        private void TD(double tx, double ty)
        {
            TL_Leading(-ty);
            Td(tx, ty);
        }

        #endregion

    }
}
