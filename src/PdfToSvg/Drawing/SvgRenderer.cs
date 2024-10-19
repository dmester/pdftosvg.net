﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing.Paths;
using PdfToSvg.Drawing.Patterns;
using PdfToSvg.Fonts;
using PdfToSvg.Imaging;
using PdfToSvg.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        private static readonly XNamespace annotNs = "https://pdftosvg.net/xmlns/annotations";

        private const double MaxAlpha = 0.999;
        private const double MinAlpha = 0.001;

        private static readonly string BrokenImageSymbolId = StableID.Generate("im", "brokenimg");
        private static readonly string RootClassName = StableID.Generate("g", "PdfToSvg_Root");
        private static readonly string NoPrintClassName = StableID.Generate("g", "PdfToSvg_NoPrint");
        private static readonly string NoScreenClassName = StableID.Generate("g", "PdfToSvg_NoScreen");

        private static readonly string LinkStyle = "." + RootClassName + " a:active path{fill:#ffe4002e;}";
        private static readonly string TextStyle = "." + RootClassName + " text{white-space:pre;}";
        private static readonly string NoPrintStyle = "@media print{." + NoPrintClassName + "{display:none;}}";
        private static readonly string NoScreenStyle = "@media screen{." + NoScreenClassName + "{display:none;}}";

        private GraphicsState graphicsState = new GraphicsState();
        private Stack<GraphicsState> graphicsStateStack = new Stack<GraphicsState>();

        private bool markedContentVisible = true;
        private Stack<MarkedContent> markedContentStack = new Stack<MarkedContent>();

        private XElement svg;
        private XElement rootGraphics;
        private XElement currentTransparencyGroup;

        private bool annotNamespaceAdded;

        private bool svgHasDefaultMiterLimit;

        private bool inType3Glyph;
        private bool ignoreColorChange;

        private PathData currentPath = new PathData();

        private double currentPointX, currentPointY;

        private DocumentCache documentCache;
        private OptionalContentGroupManager optionalContentGroupManager;
        private ResourceCache resources;

        private XElement defs = new XElement(ns + "defs");
        private Dictionary<object, string?> imageIds = new();
        private Dictionary<object, string?> patternIds = new();

        private HashSet<string> defIds = new HashSet<string>();

        private XElement? clipWrapper;
        private string? clipWrapperId;

        private static readonly OperationDispatcher dispatcher = new OperationDispatcher(typeof(SvgRenderer));

        private XElement style = new XElement(ns + "style");

        private HashSet<string> styleClassNames = new HashSet<string>();
        private HashSet<string> fontFaceNames = new HashSet<string>();

        private TextBuilder textBuilder;
        private bool hasTextStyle;

        private readonly PdfDictionary pageDict;
        private readonly Rectangle cropBox;
        private readonly SvgConversionOptions options;
        private readonly CancellationToken cancellationToken;
        private Matrix originalTransform;

        private Dictionary<string, ClipPath> clipPaths = new Dictionary<string, ClipPath>();

        private SvgRenderer(PdfDictionary pageDict, SvgConversionOptions? options, DocumentCache documentCache, OptionalContentGroupManager optionalContentGroupManager, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                options = new SvgConversionOptions();
            }

            this.options = options;
            this.pageDict = pageDict;
            this.documentCache = documentCache;
            this.optionalContentGroupManager = optionalContentGroupManager;
            this.cancellationToken = cancellationToken;

            textBuilder = new TextBuilder(options);
            resources = new ResourceCache(pageDict.GetDictionaryOrEmpty(Names.Resources));

            cropBox = GetCropBox(pageDict);

            var pageWidth = cropBox.Width;
            var pageHeight = cropBox.Height;

            rootGraphics = new XElement(ns + "g");
            currentTransparencyGroup = rootGraphics;

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

            var userUnit = pageDict.GetValueOrDefault(Names.UserUnit, 1.0);
            if (userUnit <= 0)
            {
                userUnit = 1.0;
            }

            svg = new XElement(ns + "svg",
                new XAttribute("width", (pageWidth * userUnit).ToString("0", CultureInfo.InvariantCulture)),
                new XAttribute("height", (pageHeight * userUnit).ToString("0", CultureInfo.InvariantCulture)),
                new XAttribute("preserveAspectRatio", "xMidYMid meet"),
                new XAttribute("viewBox",
                    string.Format(CultureInfo.InvariantCulture, "0 0 {0:0.####} {1:0.####}",
                    pageWidth, pageHeight
                )),
                new XComment(" Generator: PdfToSvg.NET "),
                style,
                defs);

            // PDF coordinate system has its origin in the bottom left corner in opposite to SVG, 
            // which has its origin in the upper left corner.
            graphicsState.Transform = MoveOriginToTopLeft(cropBox);
            originalTransform = graphicsState.Transform;
        }

        private static Rectangle GetCropBox(PdfDictionary pageDict)
        {
            var hasCropBox = pageDict.TryGetRectangle(Names.CropBox, out var cropBox);
            var hasMediaBox = pageDict.TryGetRectangle(Names.MediaBox, out var mediaBox);

            if (hasCropBox && hasMediaBox)
            {
                // The spec says the CropBox should be considered to be the intersection
                // if the CropBox extends outside the MediaBox.
                return Rectangle.Intersection(cropBox, mediaBox);
            }

            if (hasCropBox)
            {
                return cropBox;
            }

            if (hasMediaBox)
            {
                return mediaBox;
            }

            // Default
            return RectangleUtils.GetA4();
        }

        private static Matrix MoveOriginToTopLeft(Rectangle bbox)
        {
            var matrix = Matrix.Scale(1, -1);

            matrix = Matrix.Translate(0, -bbox.Height, matrix);

            matrix = Matrix.Translate(-bbox.X1, -bbox.Y1, matrix);

            return matrix;
        }

        private void AfterDispatch()
        {
            graphicsState = new GraphicsState();
            graphicsState.Transform = originalTransform;
            graphicsStateStack.Clear();
            currentTransparencyGroup = rootGraphics;

            if (options.IncludeAnnotations)
            {
                AddAnnotations();
            }

            if (options.IncludeLinks)
            {
                AddHyperlinks();
            }

            AddClipPaths(clipPaths.Values);

            if (!defs.HasElements)
            {
                defs.Remove();
            }

            if (style.IsEmpty)
            {
                style.Remove();
            }

            var optimizedRootGraphics = SvgAttributeOptimizer.Optimize(rootGraphics);
            svg.Add(optimizedRootGraphics);
        }

        private void AddStyle(string defintion)
        {
            style.Add("\n" + defintion);
        }

        private void AddStyle(string prefix, CssPropertyCollection content, out string className)
        {
            var styleString = content.ToString();

            className = StableID.Generate(prefix, styleString);

            if (styleClassNames.Add(className))
            {
                style.Add("\n." + className + "{" + styleString + "}");
            }
        }

        private void AddAnnotations()
        {
            if (!pageDict.TryGetArray<PdfDictionary>(Names.Annots, out var annots))
            {
                return;
            }

            var numFiles = 0;

            foreach (var annot in annots)
            {
                if (annot.TryGetDictionary(Names.OC, out var oc) && !optionalContentGroupManager.GroupVisible(oc))
                {
                    continue;
                }

                var fileIndex = (int?)null;

                var attachment = FileAttachment.Create(annot);
                if (attachment != null)
                {
                    fileIndex = numFiles++;
                }

                RenderAnnotationAppearance(annot, fileIndex);
            }
        }

        private void AddHyperlinks()
        {
            if (pageDict.TryGetArray<PdfDictionary>(Names.Annots, out var annots))
            {
                var hasLink = false;

                foreach (var annot in annots)
                {
                    if (annot.TryGetDictionary(Names.OC, out var oc) && !optionalContentGroupManager.GroupVisible(oc))
                    {
                        continue;
                    }

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
                    AddStyle(LinkStyle);
                }
            }
        }

        private void AddClipPaths(IEnumerable<ClipPath> paths)
        {
            foreach (var path in paths)
            {
                if (path.Referenced && defIds.Add(path.Id))
                {
                    defs.Add(New.XElement(ns + "clipPath",
                        new XAttribute("id", path.Id),
                        path.Parent != null ? new XAttribute("clip-path", "url(#" + path.Parent.Id + ")") : null,
                        path.ClipElement));

                    AddClipPaths(path.Children.Values);
                }
            }
        }

        [Conditional("DEBUG")]
        private void DebugLogOperation(string op, object?[] operands, bool isGs = false)
        {
#if DEBUG
            if (!options.DebugLogOperators)
            {
                return;
            }

            XElement target;

            if (graphicsState.ClipPath == null ||
                clipWrapperId != graphicsState.ClipPath.Id)
            {
                target = currentTransparencyGroup;
                clipWrapper = null;
                clipWrapperId = null;
            }
            else
            {
                target = clipWrapper ?? currentTransparencyGroup;
            }

            void FormatArray(StringBuilder output, IEnumerable enumerable, int depth)
            {
                var index = 0;

                foreach (var item in enumerable)
                {
                    if (index++ > 50)
                    {
                        output.Append("... ");
                        break;
                    }

                    if (depth > 10)
                    {
                        output.Append("item ");
                    }
                    else
                    {
                        Format(output, item, depth);
                        output.Append(" ");
                    }
                }
            }

            void Format(StringBuilder output, object? value, int depth)
            {
                if (value == null)
                {
                    output.Append("null");
                }
                else if (value is PdfString pdfString)
                {
                    var decodedText = graphicsState.Font.DecodeString(pdfString).Value;
                    decodedText = SvgConversion.ReplaceInvalidChars(decodedText);
                    output.Append('(');
                    output.Append(decodedText);
                    output.Append(')');
                }
                else if (value is IFormattable formattable)
                {
                    output.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                }
                else if (value is IEnumerable enumerable && !(value is string))
                {
                    output.Append("[ ");
                    FormatArray(output, enumerable, depth);
                    output.Append("]");
                }
                else
                {
                    output.Append(value.ToString());
                }
            }

            var text = new StringBuilder();

            if (isGs)
            {
                text.Append("   ");
                text.Append(op);
                text.Append(' ');
                FormatArray(text, operands, 0);
            }
            else
            {
                text.Append(' ');
                FormatArray(text, operands, 0);
                text.Append(op);
                text.Append(' ');
            }

            target.Add(new XComment(text.ToString()));
#endif
        }

        public static XElement Convert(PdfDictionary pageDict, SvgConversionOptions? options, DocumentCache documentCache, OptionalContentGroupManager optionalContentGroupManager, CancellationToken cancellationToken)
        {
            var renderer = new SvgRenderer(pageDict, options, documentCache, optionalContentGroupManager, cancellationToken);

            using (var contentStream = ContentStream.Combine(pageDict, cancellationToken))
            {
                foreach (var op in ContentParser.Parse(contentStream))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    renderer.DebugLogOperation(op.Operator, op.Operands);
                    dispatcher.Dispatch(renderer, op.Operator, op.Operands);
                }
            }

            renderer.AfterDispatch();
            return renderer.svg;
        }

#if HAVE_ASYNC
        public static async Task<XElement> ConvertAsync(PdfDictionary pageDict, SvgConversionOptions? options, DocumentCache documentCache, OptionalContentGroupManager optionalContentGroupManager, CancellationToken cancellationToken)
        {
            var renderer = new SvgRenderer(pageDict, options, documentCache, optionalContentGroupManager, cancellationToken);

            using (var contentStream = await ContentStream.CombineAsync(pageDict, cancellationToken).ConfigureAwait(false))
            {
                foreach (var op in ContentParser.Parse(contentStream))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    renderer.DebugLogOperation(op.Operator, op.Operands);
                    await dispatcher.DispatchAsync(renderer, op.Operator, op.Operands).ConfigureAwait(false);
                }
            }

            renderer.AfterDispatch();
            return renderer.svg;
        }
#endif


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

        [Operation("BDC")]
        private void BDC_BeginMarkedContent(PdfName tag, PdfName ocName)
        {
            if (tag == Names.OC &&
                resources.Dictionary.TryGetDictionary(Names.Properties / ocName, out var ocgDict))
            {
                markedContentVisible = markedContentVisible && optionalContentGroupManager.GroupVisible(ocgDict);
            }

            markedContentStack.Push(new MarkedContent { Visible = markedContentVisible });
        }

        [Operation("EMC")]
        private void EMC_EndMarkedContent()
        {
            if (markedContentStack.Count > 0)
            {
                markedContentStack.Pop();
                markedContentVisible = markedContentStack.Count == 0 || markedContentStack.Peek().Visible;
            }
        }

        [Operation("cm")]
        private void cm_SetMatrix(double a, double b, double c, double d, double e, double f)
        {
            graphicsState.Transform = new Matrix(a, b, c, d, e, f) * graphicsState.Transform;
            textBuilder.UpdateLineMatrix(graphicsState);
        }

        [Operation("w")]
        [Operation("gs/LW")]
        private void w_LineWidth(double lineWidth)
        {
            if (graphicsState.StrokeWidth != lineWidth)
            {
                graphicsState.StrokeWidth = lineWidth;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("J")]
        [Operation("gs/LC")]
        private void J_LineCap(int lineCap)
        {
            if (graphicsState.StrokeLineCap != lineCap)
            {
                graphicsState.StrokeLineCap = lineCap;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("j")]
        [Operation("gs/LJ")]
        private void j_LineJoin(int lineJoin)
        {
            if (graphicsState.StrokeLineJoin != lineJoin)
            {
                graphicsState.StrokeLineJoin = lineJoin;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("M")]
        [Operation("gs/ML")]
        private void M_MiterLimit(double miterLimit)
        {
            if (graphicsState.StrokeMiterLimit != miterLimit)
            {
                graphicsState.StrokeMiterLimit = miterLimit;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("d")]
        private void d_DashArray(int[] dashArray, int dashPhase)
        {
            graphicsState.StrokeDashArray = dashArray;
            graphicsState.StrokeDashPhase = dashPhase;
            textBuilder.InvalidateStyle();
        }

        [Operation("gs/D")]
        private void gs_D_DashArray(object[] args)
        {
            dispatcher.Dispatch(this, "d", args);
        }

        [Operation("gs/CA")]
        private void gs_CA_StrokeAlpha(double alpha)
        {
            if (graphicsState.StrokeAlpha != alpha)
            {
                graphicsState.StrokeAlpha = alpha;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("gs/ca")]
        private void gs_ca_FillAlpha(double alpha)
        {
            if (graphicsState.FillAlpha != alpha)
            {
                graphicsState.FillAlpha = alpha;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("gs/BM")]
        private void gs_BM_BlendMode(PdfName mode)
        {
            if (!Enum.TryParse(mode.Value, ignoreCase: true, out BlendMode modeValue))
            {
                modeValue = BlendMode.Normal;
            }

            if (graphicsState.BlendMode != modeValue)
            {
                graphicsState.BlendMode = modeValue;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("gs/BM")]
        private void gs_BM_BlendMode(PdfName[] modes)
        {
            var modeValue = BlendMode.Normal;

            foreach (var mode in modes)
            {
                if (Enum.TryParse(mode.Value, ignoreCase: true, out modeValue))
                {
                    break;
                }
                else
                {
                    modeValue = BlendMode.Normal;
                }
            }

            if (graphicsState.BlendMode != modeValue)
            {
                graphicsState.BlendMode = modeValue;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("gs/SMask")]
        private void gs_SMask_SoftMask(object smask)
        {
            // According to PDF 1.7 section 11.6.4.3, the value can be either an SMask dict or /None.
            // We will interpret all invalid values as /None.
            var id = smask is PdfDictionary smaskDict ? RenderSMask(smaskDict) : null;
            if (graphicsState.SMaskId != id)
            {
                graphicsState.SMaskId = id;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("gs")]
        private void gs_GraphicsStateFromDictionary(PdfName dictName)
        {
            if (resources.Dictionary.TryGetDictionary(Names.ExtGState / dictName, out var extGState))
            {
                foreach (var state in extGState)
                {
                    DebugLogOperation(state.Key.ToString(), new[] { state.Value }, isGs: true);
                    dispatcher.Dispatch(this, "gs" + state.Key, new[] { state.Value });
                }
            }
        }

        #endregion

        #region XObject operators

        private string? GetSvgImageId(PdfDictionary imageObject)
        {
            if (imageObject.Stream != null &&
                imageObject.TryGetName(Names.Subtype, out var subtype) && subtype == Names.Image &&
                imageObject.TryGetInteger(Names.Width, out var width) &&
                imageObject.TryGetInteger(Names.Height, out var height))
            {
                // graphicsState may be accessed below
                var interpolate = imageObject.GetValueOrDefault(Names.Interpolate, false);
                if (!interpolate)
                {
                    // Only respect disabling of interpolation for upscaled images.
                    // Most readers does not seem to consider the /Interpolate option for downscaled images.
                    graphicsState.Transform.DecomposeScaleXY(out var transformScaleX, out var transformScaleY);

                    if (transformScaleX < width * 4 && transformScaleY < height * 4)
                    {
                        interpolate = true;
                    }
                }

                var imageLookupKey = Tuple.Create(new ReferenceEquatableBox(imageObject), interpolate);

                // graphicsState must not be accessed below this line

                if (imageIds.TryGetValue(imageLookupKey, out var imageId))
                {
                    return imageId;
                }

                ColorSpace colorSpace;

                if (imageObject.GetValueOrDefault(Names.ImageMask, false))
                {
                    colorSpace = new IndexedColorSpace(new DeviceRgbColorSpace(), new byte[]
                    {
                        /* 0 */ 255, 255, 255,
                        /* 1 */ 0, 0, 0,
                    });
                }
                else
                {
                    colorSpace = GetColorSpace(imageObject[Names.ColorSpace]);

                    if (colorSpace is UnsupportedColorSpace)
                    {
                        return null;
                    }
                }

                var image = ImageFactory.Create(imageObject, colorSpace);
                if (image != null)
                {
                    var imageResolver = options.ImageResolver;
                    var imageUrl = imageResolver.ResolveImageUrl(image, cancellationToken);

                    // Note that the interpolation parameter unfortunately must be part of the id, since the image-rendering
                    // attribute must be specified on the shared <image> element.
                    imageId = StableID.Generate("im", imageUrl, interpolate);

                    if (defIds.Add(imageId))
                    {
                        var svgImage = new XElement(ns + "image");

                        svgImage.SetAttributeValue("id", imageId);
                        svgImage.SetAttributeValue("href", imageUrl);
                        svgImage.SetAttributeValue("width", "1");
                        svgImage.SetAttributeValue("height", "1");
                        svgImage.SetAttributeValue("preserveAspectRatio", "none");

                        if (!interpolate)
                        {
                            svgImage.SetAttributeValue("image-rendering", "pixelated");
                        }

                        defs.Add(svgImage);
                    }
                }

                // Cache result to prevent having to recode the same image again if it is used multiple times on the
                // same page.
                imageIds[imageLookupKey] = imageId;
                return imageId;
            }

            return null;
        }

        private XElement? RenderTilingPattern(PdfDictionary pattern, Matrix transform, RgbColor color)
        {
            // PDF 1.7 spec Table 75
            if (pattern.Stream == null)
            {
                return null;
            }

            if (!pattern.TryGetRectangle(Names.BBox, out var bbox))
            {
                return null;
            }

            if (pattern.TryGetMatrix(Names.Matrix, out var patternTransform))
            {
                transform = patternTransform * transform;
            }

            var xstep = pattern.GetValueOrDefault(Names.XStep, 0);
            var ystep = pattern.GetValueOrDefault(Names.YStep, 0);

            // Move origin to prevent unnecessary transforms in the content
            var originTransform = MoveOriginToTopLeft(bbox);

            transform = originTransform.Invert() * transform;

            var patternEl = New.XElement(ns + "pattern",
                new XAttribute("width", SvgConversion.FormatCoordinate(xstep)),
                new XAttribute("height", SvgConversion.FormatCoordinate(ystep)),
                new XAttribute("patternUnits", "userSpaceOnUse"),
                transform.IsIdentity
                    ? null
                    : new XAttribute("patternTransform", SvgConversion.Matrix(transform)),
                new XAttribute("viewBox",
                    "0 " +
                    "0 " +
                    SvgConversion.FormatCoordinate(xstep) + " " +
                    SvgConversion.FormatCoordinate(ystep))
                );

            RenderXObject(pattern, () =>
            {
                graphicsState = new GraphicsState();
                graphicsState.Transform = originTransform;

                originalTransform = graphicsState.Transform;

                currentTransparencyGroup = patternEl;

                if (bbox.Width < xstep ||
                    bbox.Height < ystep)
                {
                    // Clipping needed
                    AppendClipping(new Rectangle(0, 0, bbox.Width, bbox.Height), rectIsTransformed: true);
                }

                var paintType = pattern.GetValueOrDefault(Names.PaintType, 1);
                if (paintType == 2)
                {
                    // Potential improvement: use currentColor to make the mask reusable for different colors
                    graphicsState.FillColor = color;
                    graphicsState.StrokeColor = color;
                    ignoreColorChange = true;
                }
                else
                {
                    ignoreColorChange = false;
                }
            });

            return patternEl;
        }

        private string? RenderSMask(PdfDictionary smask)
        {
            // PDF 1.7 spec Table 144
            if (!smask.TryGetDictionary(Names.G, out var group) || group.Stream == null)
            {
                return null;
            }

            var transform = graphicsState.Transform;

            var maskEl = new XElement(ns + "mask");

            // Possible values: Alpha and Luminosity. Luminosity is the default in SVG.
            var alphaMask = smask.GetValueOrDefault(Names.S, Names.Luminosity) == Names.Alpha;
            if (alphaMask)
            {
                maskEl.Add(new XAttribute("mask-type", "alpha"));
            }

            RenderXObject(group, () =>
            {
                graphicsState = new GraphicsState();
                currentTransparencyGroup = maskEl;

                graphicsState.Transform = originalTransform;
                
                transform = originalTransform.Invert() * transform;

                if (!transform.IsIdentity)
                {
                    var maskContentEl = new XElement(ns + "g", new XAttribute("transform", SvgConversion.Matrix(transform)));
                    currentTransparencyGroup.Add(maskContentEl);
                    currentTransparencyGroup = maskContentEl;
                }

                if (group.TryGetRectangle(Names.BBox, out var bbox))
                {
                    AppendClipping(bbox, rectIsTransformed: false);
                }
            });

            var id = StableID.Generate("sm", maskEl);

            if (defIds.Add(id))
            {
                maskEl.Add(new XAttribute("id", id));
                defs.Add(maskEl);
            }

            return id;
        }

        private void RenderForm(PdfDictionary xobject, IList<object>? additionalGroupContent = null)
        {
            // Optional content group
            if (!markedContentVisible)
            {
                // Hidden by BDC content operator
                return;
            }
            if (xobject.TryGetDictionary(Names.OC, out var ocDict) &&
                !optionalContentGroupManager.GroupVisible(ocDict))
            {
                // Hidden by /OC property on XObject dict
                return;
            }

            RenderXObject(xobject, () =>
            {
                var groupAttributes = new List<XAttribute>();

                var isTransparencyGroup = xobject.GetNameOrNull(Names.Group / Names.S) == Names.Transparency;
                if (isTransparencyGroup)
                {
                    if (graphicsState.FillAlpha < MaxAlpha)
                    {
                        groupAttributes.Add(new XAttribute("opacity", SvgConversion.FormatCoordinate(graphicsState.FillAlpha)));
                    }

                    if (graphicsState.SMaskId != null)
                    {
                        groupAttributes.Add(new XAttribute("mask", "url(#" + graphicsState.SMaskId + ")"));
                    }

                    if (graphicsState.BlendMode != BlendMode.Normal)
                    {
                        groupAttributes.Add(new XAttribute("style", "mix-blend-mode:" + SvgConversion.FormatBlendMode(graphicsState.BlendMode)));
                    }

                    graphicsState.FillAlpha = 1d;
                    graphicsState.StrokeAlpha = 1d;
                    graphicsState.SMaskId = null;
                    graphicsState.BlendMode = BlendMode.Normal;

                    // Isolated groups and knockout groups are currently not supported
                }

                if (groupAttributes.Count > 0 ||
                    additionalGroupContent != null && additionalGroupContent.Count > 0)
                {
                    var newGroup = New.XElement(ns + "g", groupAttributes, additionalGroupContent);

                    currentTransparencyGroup.Add(newGroup);
                    currentTransparencyGroup = newGroup;

                    clipWrapper = null;
                    clipWrapperId = null;
                }

                if (xobject.TryGetMatrix(Names.Matrix, out var matrix))
                {
                    graphicsState.Transform = matrix * graphicsState.Transform;
                }

                if (xobject.TryGetRectangle(Names.BBox, out var bbox))
                {
                    AppendClipping(bbox, rectIsTransformed: false);
                }
            });
        }

        private void RenderAnnotationAppearance(PdfDictionary annot, int? fileIndex)
        {
            if (!annot.TryGetDictionary(Names.AP / Names.N, out var normalAppearance) ||
                !annot.TryGetRectangle(Names.Rect, out var rect))
            {
                return;
            }

            var appearanceStream = normalAppearance;

            // ISO-32000-2 Table 170
            // The appearance can either be a stream, or a dictionary of streams
            if (annot.TryGetName(Names.AS, out var appearanceState) &&
                normalAppearance.TryGetDictionary(appearanceState, out var appearanceStateStream) &&
                appearanceStateStream.Stream != null)
            {
                appearanceStream = appearanceStateStream;
            }

            if (!appearanceStream.TryGetMatrix(Names.Matrix, out var matrix))
            {
                matrix = Matrix.Identity;
            }

            if (appearanceStream.TryGetRectangle(Names.BBox, out var bbox))
            {
                var additionalGroupContent = new List<object>();

                // METADATA
                {
                    if (!annotNamespaceAdded)
                    {
                        svg.Add(new XAttribute(XNamespace.Xmlns + "annot", annotNs));
                        annotNamespaceAdded = true;
                    }

                    var subtype = annot.GetValueOrDefault<PdfName?>(Names.Subtype)?.Value;
                    if (subtype != null)
                    {
                        additionalGroupContent.Add(new XAttribute(annotNs + "type", subtype));
                    }

                    var creationDate = annot.GetValueOrDefault<DateTimeOffset?>(Names.CreationDate);
                    if (creationDate != null)
                    {
                        additionalGroupContent.Add(new XAttribute(annotNs + "created", creationDate));
                    }

                    if (annot.ContainsKey(Names.Popup))
                    {
                        var title = annot.GetValueOrDefault(Names.T, PdfString.Empty).ToString();
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            additionalGroupContent.Add(new XAttribute(annotNs + "popup-title", title));
                        }

                        var contents = annot.GetValueOrDefault(Names.Contents, PdfString.Empty).ToString();
                        if (!string.IsNullOrWhiteSpace(contents))
                        {
                            additionalGroupContent.Add(new XAttribute(annotNs + "popup-contents", contents));
                        }
                    }

                    if (fileIndex != null)
                    {
                        additionalGroupContent.Add(new XAttribute(annotNs + "file-index", fileIndex));
                    }
                }

                // VISIBILITY
                if (annot.TryGetValue<int>(Names.F, out var intFlags))
                {
                    var flags = (AnnotationFlags)intFlags;

                    if (flags.HasFlag(AnnotationFlags.Hidden) ||
                        flags.HasFlag(AnnotationFlags.Invisible))
                    {
                        return;
                    }

                    var printVisible = flags.HasFlag(AnnotationFlags.Print);
                    var screenVisible = !flags.HasFlag(AnnotationFlags.NoView);

                    if (!printVisible && !screenVisible)
                    {
                        return;
                    }

                    if (printVisible != screenVisible)
                    {
                        var cssStyle = printVisible ? NoScreenStyle : NoPrintStyle;
                        var cssStyleClassName = printVisible ? NoScreenClassName : NoPrintClassName;

                        if (styleClassNames.Add(cssStyleClassName))
                        {
                            AddStyle(cssStyle);
                        }

                        additionalGroupContent.Add(new XAttribute("class", cssStyleClassName));
                    }
                }

                // TRANSFORM
                {
                    // Transform is described in PDF spec 2.0 section 12.5.5 Appearance streams
                    var bboxCorners = new[]
                    {
                        bbox.BottomLeft,
                        bbox.TopLeft,
                        bbox.TopRight,
                        bbox.BottomRight,
                    };

                    var quadrilateral = bboxCorners
                        .Select(p => matrix * p)
                        .ToList();

                    var transformedAppearanceBox = new Rectangle(
                        quadrilateral.Min(p => p.X),
                        quadrilateral.Min(p => p.Y),
                        quadrilateral.Max(p => p.X),
                        quadrilateral.Max(p => p.Y)
                    );

                    graphicsState.Transform =
                        Matrix.Translate(-transformedAppearanceBox.X1, -transformedAppearanceBox.Y1) *
                        Matrix.Scale(rect.Width / transformedAppearanceBox.Width, rect.Height / transformedAppearanceBox.Height) *
                        Matrix.Translate(rect.X1, rect.Y1) *
                        originalTransform;
                }

                RenderForm(appearanceStream, additionalGroupContent);
            }
        }

        private void RenderXObject(PdfDictionary xobject, Action preparations)
        {
            var contentStream = xobject.Stream;
            if (contentStream != null)
            {
                var originalGraphicsStateStack = graphicsStateStack;
                var originalGraphicsState = graphicsState;
                var originalMarkedContentStack = markedContentStack;
                var originalIgnoreColorChange = ignoreColorChange;
                var originalTransparencyGroup = currentTransparencyGroup;
                var originalOriginalTransform = originalTransform;
                var originalResources = resources;
                var originalTextBuilder = textBuilder;

                try
                {
                    // In older PDF's the resources are specified on the page instead of the form.
                    if (xobject.TryGetDictionary(Names.Resources, out var resourcesDict))
                    {
                        resources = new ResourceCache(resourcesDict);
                    }

                    markedContentStack = new Stack<MarkedContent>();

                    graphicsStateStack = new Stack<GraphicsState>();
                    graphicsState = graphicsState.Clone();

                    textBuilder = new TextBuilder(options);

                    preparations();

                    // Buffer content since we might need to access the input file while rendering the page
                    using var bufferedFormContent = new MemoryStream();
                    using (var decodedFormContent = contentStream.OpenDecoded(cancellationToken))
                    {
                        decodedFormContent.CopyTo(bufferedFormContent);
                    }
                    bufferedFormContent.Position = 0;

                    foreach (var operation in ContentParser.Parse(bufferedFormContent))
                    {
                        DebugLogOperation(operation.Operator, operation.Operands);
                        dispatcher.Dispatch(this, operation.Operator, operation.Operands);
                    }
                }
                finally
                {
                    graphicsStateStack = originalGraphicsStateStack;
                    graphicsState = originalGraphicsState;
                    markedContentStack = originalMarkedContentStack;
                    markedContentVisible = markedContentStack.Count == 0 || markedContentStack.Peek().Visible;
                    ignoreColorChange = originalIgnoreColorChange;
                    currentTransparencyGroup = originalTransparencyGroup;
                    originalTransform = originalOriginalTransform;
                    resources = originalResources;
                    textBuilder = originalTextBuilder;

                    clipWrapper = null;
                    clipWrapperId = null;

                    textBuilder.InvalidateStyle();
                }
            }
        }

        private void RenderImage(PdfDictionary xobject)
        {
            // Optional content group
            if (!markedContentVisible)
            {
                // Hidden by BDC content operator
                return;
            }
            if (xobject.TryGetDictionary(Names.OC, out var ocDict) &&
                !optionalContentGroupManager.GroupVisible(ocDict))
            {
                // Hidden by /OC property on XObject dict
                return;
            }

            var imageAttributes = new List<XAttribute>();

            // Positioning
            var imageTransform = Matrix.Translate(0, -1) * Matrix.Scale(1, -1) * graphicsState.Transform;
            imageAttributes.Add(new XAttribute("transform", SvgConversion.Matrix(imageTransform)));

            // Constant alpha
            if (graphicsState.FillAlpha < MinAlpha)
            {
                return; // Don't render at all
            }
            else if (graphicsState.FillAlpha < MaxAlpha)
            {
                imageAttributes.Add(new XAttribute("opacity", SvgConversion.FormatCoordinate(graphicsState.FillAlpha)));
            }

            // Blend mode
            if (graphicsState.BlendMode != BlendMode.Normal)
            {
                imageAttributes.Add(new XAttribute("style", "mix-blend-mode:" + SvgConversion.FormatBlendMode(graphicsState.BlendMode)));
            }

            var imageId = GetSvgImageId(xobject);
            if (imageId == null)
            {
                // Missing image
                // Replace with broken image icon

                if (defIds.Add(BrokenImageSymbolId))
                {
                    var brokenImageSymbol = BrokenImageSymbol.Create();
                    brokenImageSymbol.SetAttributeValue("id", BrokenImageSymbolId);
                    defs.AddAfterSelf(brokenImageSymbol);
                }

                AppendClipped(new XElement(ns + "g",
                    imageAttributes,
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

            var isStencilMask = xobject.GetValueOrDefault(Names.ImageMask, false);

            // Decide mask
            var graphicsStateSMaskIsApplicable = true;
            PdfDictionary? maskImage;

            if (isStencilMask)
            {
                maskImage = xobject;
            }
            else if (xobject.TryGetDictionary(Names.SMask, out var smask))
            {
                maskImage = smask;

                // According to ISO 32000-2-2020 Table 87 SMask description:
                graphicsStateSMaskIsApplicable = false;
            }
            else if (xobject.TryGetDictionary(Names.Mask, out var stencilMask))
            {
                maskImage = stencilMask;
            }
            else
            {
                maskImage = null;
            }

            // Create mask definition
            if (maskImage != null)
            {
                var maskImageId = GetSvgImageId(maskImage);
                if (maskImageId != null)
                {
                    var maskId = StableID.Generate("m", maskImageId);

                    if (defIds.Add(maskId))
                    {
                        defs.Add(new XElement(
                            ns + "mask",
                            new XAttribute("id", maskId),
                            new XElement(
                                ns + "use",
                                new XAttribute("href", "#" + maskImageId))));
                    }

                    imageAttributes.Add(new XAttribute("mask", "url(#" + maskId + ")"));
                }
            }

            // Output
            var imageEl = isStencilMask
                ? new XElement(ns + "g", imageAttributes,
                    new XElement(ns + "path",
                        new XAttribute("d", "M0 0V1H1V-1z"),
                        new XAttribute("fill", SvgConversion.FormatColor(graphicsState.FillColor)))
                    )
                : new XElement(ns + "g", imageAttributes,
                    new XElement(ns + "use", new XAttribute("href", "#" + imageId))
                    );

            // Soft mask from graphics state
            if (graphicsStateSMaskIsApplicable)
            {
                imageEl = ApplySMask(imageEl, graphicsState.SMaskId);
            }
            
            AppendClipped(imageEl);
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

        private Rectangle GetShadingClipRectangle()
        {
            var result = cropBox;
            var clipPath = graphicsState.ClipPath;

            while (clipPath != null)
            {
                if (clipPath.IsRectangle)
                {
                    result = Rectangle.Intersection(result, clipPath.Rectangle);
                    break;
                }
                clipPath = clipPath.Parent;
            }

            return result;
        }

        private string? GetSvgPatternId(Pattern pattern, RgbColor color, Matrix transform)
        {
            var key = Tuple.Create(new ReferenceEquatableBox(pattern), color, transform);

            if (patternIds.TryGetValue(key, out var patternId))
            {
                return patternId;
            }

            // The pattern should not be affected by the current transform matrix, but it is in SVG, so we need to
            // invert it. The page transform is double inverted, so that gradient coordinates can be in PDF coordinates.
            // This is quite ugly and could be improved.
            transform = (transform * originalTransform).Invert();

            XElement? patternEl = null;

            switch (pattern)
            {
                case TilingPattern tilingPattern:
                    patternEl = RenderTilingPattern(tilingPattern.Definition, transform, color);
                    break;

                case ShadingPattern shadingPattern:
                    patternEl = shadingPattern.Shading?.GetShadingElement(shadingPattern.Matrix * transform, GetShadingClipRectangle(), inPattern: true);
                    break;
            }

            if (patternEl != null)
            {
                patternId = StableID.Generate("pt", patternEl);
                patternEl.SetAttributeValue("id", patternId);

                if (defIds.Add(patternId))
                {
                    defs.Add(patternEl);
                }
            }

            patternIds[key] = patternId;
            return patternId;
        }

        [Operation("sh")]
        private void sh_Shading(PdfName shadingName)
        {
            if (!markedContentVisible)
            {
                return;
            }

            var shading = resources.GetShading(shadingName, cancellationToken);
            if (shading == null)
            {
                Log.WriteLine($"Could not find shading {shadingName}.");
                return;
            }

            var gradientEl = shading.GetShadingElement(graphicsState.Transform, GetShadingClipRectangle(), inPattern: false);
            if (gradientEl == null)
            {
                return;
            }

            var gradientId = StableID.Generate("pt", gradientEl);
            if (defIds.Add(gradientId))
            {
                defs.Add(gradientEl);
                gradientEl.SetAttributeValue("id", gradientId);
            }

            Rectangle fillRect;
            Matrix fillRectTransform;

            if (shading.BBox == null)
            {
                fillRect = cropBox;
                fillRectTransform = originalTransform;
            }
            else
            {
                fillRect = shading.BBox.Value;
                fillRectTransform = graphicsState.Transform;
            }

            var points = new Point[]
            {
                fillRectTransform * fillRect.TopLeft,
                fillRectTransform * fillRect.TopRight,
                fillRectTransform * fillRect.BottomRight,
                fillRectTransform * fillRect.BottomLeft,
            };

            var bboxClipped = false;
            var clipPath = graphicsState.ClipPath;

            if (clipPath != null &&
                clipPath.Parent == null &&
                clipPath.IsRectangle &&
                fillRectTransform.B == 0 &&
                fillRectTransform.C == 0)
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var x = MathUtils.Clamp(points[i].X, clipPath.Rectangle.X1, clipPath.Rectangle.X2);
                    var y = MathUtils.Clamp(points[i].Y, clipPath.Rectangle.Y1, clipPath.Rectangle.Y2);
                    points[i] = new Point(x, y);
                }
                bboxClipped = true;
            }

            var path = new PathData();
            path.MoveTo(points[0].X, points[0].Y);
            path.LineTo(points[1].X, points[1].Y);
            path.LineTo(points[2].X, points[2].Y);
            path.LineTo(points[3].X, points[3].Y);
            path.ClosePath();

            var el = new XElement(ns + "path");
            el.SetAttributeValue("d", SvgConversion.PathData(path));
            el.SetAttributeValue("fill", "url(#" + gradientId + ")");

            if (graphicsState.FillAlpha < MaxAlpha)
            {
                el.SetAttributeValue("opacity", SvgConversion.FormatCoordinate(graphicsState.FillAlpha));
            }

            if (graphicsState.SMaskId != null)
            {
                el.SetAttributeValue("mask", "url(#" + graphicsState.SMaskId + ")");
            }

            if (graphicsState.BlendMode != BlendMode.Normal)
            {
                el.SetAttributeValue("style", "mix-blend-mode:" + SvgConversion.FormatBlendMode(graphicsState.BlendMode));
            }

            if (bboxClipped)
            {
                clipWrapper = null;
                clipWrapperId = null;
                currentTransparencyGroup.Add(el);
            }
            else
            {
                AppendClipped(el);
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

        private void AppendClipping(ClipPath clipPath)
        {
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

        private void AppendClipping(IList<XElement> elements)
        {
            var parent = graphicsState.ClipPath;

            var id = StableID.Generate("cl",
                "el",
                parent?.Id,
                elements);

            AppendClipping(new ClipPath(parent, id, elements));
        }

        private void AppendClipping(Rectangle rect, bool rectIsTransformed)
        {
            if (rectIsTransformed)
            {
                var parent = graphicsState.ClipPath;

                // Merge with parent if possible
                if (parent != null && parent.IsRectangle)
                {
                    rect = Rectangle.Intersection(rect, parent.Rectangle);
                    parent = parent.Parent;
                }

                var id = StableID.Generate("cl",
                    "rect",
                    parent?.Id,
                    rect.X1, rect.X2, rect.Y1, rect.Y2);

                var formattedX = SvgConversion.FormatCoordinate(rect.X1);
                var formattedY = SvgConversion.FormatCoordinate(rect.Y1);

                var element = New.XElement(ns + "rect",
                    formattedX != "0" ? new XAttribute("x", formattedX) : null,
                    formattedY != "0" ? new XAttribute("y", formattedY) : null,
                    new XAttribute("width", SvgConversion.FormatCoordinate(rect.Width)),
                    new XAttribute("height", SvgConversion.FormatCoordinate(rect.Height)));

                AppendClipping(new ClipPath(parent, id, element, rect));
            }
            else
            {
                re_Rectangle(rect.X1, rect.Y1, rect.Width, rect.Height);
                W_Clip_NonZero();
                n_EndPath();
            }
        }

        private void AppendClipping(bool evenOdd)
        {
            var path = currentPath.Transform(graphicsState.Transform);
            var parent = graphicsState.ClipPath;

            if (path.Count == 0)
            {
                Log.WriteLine("Encountered a zero length clipping path. The path is ignored.");
                return;
            }

            if (PathConverter.TryConvertToRectangle(path, out var rect))
            {
                AppendClipping(rect, rectIsTransformed: true);
            }
            else
            {
                var svgPathData = SvgConversion.PathData(path);

                var id = StableID.Generate("cl",
                    "pathdata",
                    parent?.Id,
                    svgPathData, evenOdd);

                var element = New.XElement(ns + "path",
                    new XAttribute("d", svgPathData),
                    evenOdd ? new XAttribute("clip-rule", "evenodd") : null);

                AppendClipping(new ClipPath(parent, id, element));
            }
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

                return currentTransparencyGroup;
            }
            else
            {
                if (clipWrapperId != graphicsState.ClipPath.Id)
                {
                    clipWrapper = new XElement(ns + "g", new XAttribute("clip-path", "url(#" + graphicsState.ClipPath.Id + ")"));
                    clipWrapperId = graphicsState.ClipPath.Id;
                    currentTransparencyGroup.Add(clipWrapper);

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

        private XElement ApplySMask(XElement target, string? smaskId)
        {
            if (smaskId != null)
            {
                // We don't want the transform to affect the mask, as the mask is already transformed according to
                // the transform that was active when the mask was applied
                if (target.Attribute("transform") == null)
                {
                    target.SetAttributeValue("mask", "url(#" + smaskId + ")");
                }
                else
                {
                    target = new XElement(ns + "g",
                        new XAttribute("mask", "url(#" + smaskId + ")"),
                        target);
                }
            }

            return target;
        }

        private string GetFill(GraphicsState state, Matrix currentTransform)
        {
            if (state.FillColorSpace is PatternColorSpace)
            {
                if (state.FillPattern != null)
                {
                    var patternId = GetSvgPatternId(state.FillPattern, state.FillColor, currentTransform);
                    if (patternId != null)
                    {
                        return "url(#" + patternId + ")";
                    }
                }
                return "#000";
            }

            return SvgConversion.FormatColor(state.FillColor);
        }

        private string GetStroke(GraphicsState state, Matrix currentTransform)
        {
            if (state.StrokeColorSpace is PatternColorSpace)
            {
                if (state.StrokePattern != null)
                {
                    var patternId = GetSvgPatternId(state.StrokePattern, state.StrokeColor, currentTransform);
                    if (patternId != null)
                    {
                        return "url(#" + patternId + ")";
                    }
                }
                return "#000";
            }

            return SvgConversion.FormatColor(state.StrokeColor);
        }

        private void DrawPath(bool stroke, bool fill, bool evenOddWinding)
        {
            if (!markedContentVisible)
            {
                return;
            }

            var remainingTransform = graphicsState.Transform;
            var pathTransformed = false;

            remainingTransform.DecomposeScaleXY(out var scaleX, out var scaleY);

            if (!stroke || scaleX == scaleY)
            {
                currentPath = currentPath.Transform(remainingTransform);
                remainingTransform = Matrix.Identity;
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

            var visible = false;
            var attributes = new List<object>();

            if (!remainingTransform.IsIdentity)
            {
                attributes.Add(new XAttribute("transform", SvgConversion.Matrix(remainingTransform)));
            }

            if (evenOddWinding)
            {
                attributes.Add(new XAttribute("fill-rule", "evenodd"));
            }

            if (fill && graphicsState.FillAlpha > MinAlpha)
            {
                attributes.Add(new XAttribute("fill", GetFill(graphicsState, remainingTransform)));

                if (graphicsState.FillAlpha < MaxAlpha)
                {
                    attributes.Add(new XAttribute("fill-opacity", SvgConversion.FormatCoordinate(graphicsState.FillAlpha)));
                }

                visible = true;
            }
            else
            {
                attributes.Add(new XAttribute("fill", "none"));
            }

            if (stroke && graphicsState.StrokeAlpha > MinAlpha)
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

                attributes.Add(new XAttribute("stroke", GetStroke(graphicsState, remainingTransform)));
                attributes.Add(new XAttribute("stroke-width", SvgConversion.FormatCoordinate(strokeWidth)));

                if (graphicsState.StrokeAlpha < MaxAlpha)
                {
                    attributes.Add(new XAttribute("stroke-opacity", SvgConversion.FormatCoordinate(graphicsState.StrokeAlpha)));
                }

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

                if (graphicsState.StrokeDashArray != null &&
                    graphicsState.StrokeDashArray.Length > 0)
                {
                    attributes.Add(new XAttribute("stroke-dasharray", string.Join(" ",
                        graphicsState.StrokeDashArray.Select(x => x.ToString(CultureInfo.InvariantCulture)))));

                    if (graphicsState.StrokeDashPhase != 0)
                    {
                        attributes.Add(new XAttribute("stroke-dashoffset",
                            graphicsState.StrokeDashPhase.ToString(CultureInfo.InvariantCulture)));
                    }
                }

                visible = true;
            }

            if (graphicsState.BlendMode != BlendMode.Normal)
            {
                attributes.Add(new XAttribute("style", "mix-blend-mode:" + SvgConversion.FormatBlendMode(graphicsState.BlendMode)));
            }

            if (visible)
            {
                var el = new XElement(
                    ns + "path",
                    new XAttribute("d", pathString.ToString()),
                    attributes
                    );

                el = ApplySMask(el, graphicsState.SMaskId);

                if (graphicsState.ClipPath != null && contained)
                {
                    clipWrapper = null;
                    clipWrapperId = null;
                    currentTransparencyGroup.Add(el);
                }
                else
                {
                    AppendClipped(el);
                }
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

            return ColorSpaceParser.Parse(
                definition, resources.Dictionary.GetDictionaryOrNull(Names.ColorSpace), cancellationToken);
        }

        private void SetFillColor(RgbColor newColor)
        {
            if (!ignoreColorChange)
            {
                if (graphicsState.FillColor != newColor)
                {
                    graphicsState.FillColor = newColor;
                    textBuilder.InvalidateStyle();
                }
            }
        }

        private void SetStrokeColor(RgbColor newColor)
        {
            if (!ignoreColorChange)
            {
                if (graphicsState.StrokeColor != newColor)
                {
                    graphicsState.StrokeColor = newColor;
                    textBuilder.InvalidateStyle();
                }
            }
        }

        [Operation("CS")]
        private void CS_StrokeColorSpace(PdfName name)
        {
            if (!ignoreColorChange)
            {
                graphicsState.StrokeColorSpace = GetColorSpace(name);
                SetStrokeColor(graphicsState.StrokeColorSpace.GetDefaultRgbColor());
            }
        }

        [Operation("cs")]
        private void cs_FillColorSpace(PdfName name)
        {
            if (!ignoreColorChange)
            {
                graphicsState.FillColorSpace = GetColorSpace(name);
                SetFillColor(graphicsState.FillColorSpace.GetDefaultRgbColor());
            }
        }

        [Operation("SC")]
        public void SC_StrokeColor(params float[] components)
        {
            if (!ignoreColorChange)
            {
                SetStrokeColor(new RgbColor(graphicsState.StrokeColorSpace, components));
            }
        }

        [Operation("sc")]
        public void sc_FillColor(params float[] components)
        {
            if (!ignoreColorChange)
            {
                SetFillColor(new RgbColor(graphicsState.FillColorSpace, components));
            }
        }

        [Operation("SCN")]
        public void SCN_Stroke([VariadicParam] float[] components, PdfName? patternName = null)
        {
            if (!ignoreColorChange)
            {
                if (patternName != null &&
                    graphicsState.StrokeColorSpace is PatternColorSpace patternColorSpace)
                {
                    SetStrokeColor(new RgbColor(patternColorSpace.AlternateSpace, components));

                    var newStrokePattern = resources.GetPattern(patternName, cancellationToken);
                    if (graphicsState.StrokePattern != newStrokePattern)
                    {
                        graphicsState.StrokePattern = newStrokePattern;
                        textBuilder.InvalidateStyle();
                    }
                }
                else
                {
                    SetStrokeColor(new RgbColor(graphicsState.StrokeColorSpace, components));
                }
            }
        }


        [Operation("scn")]
        public void scn_Fill([VariadicParam] float[] components, PdfName? patternName = null)
        {
            if (!ignoreColorChange)
            {
                if (patternName != null &&
                    graphicsState.FillColorSpace is PatternColorSpace patternColorSpace)
                {
                    SetFillColor(new RgbColor(patternColorSpace.AlternateSpace, components));

                    var newFillPattern = resources.GetPattern(patternName, cancellationToken);
                    if (graphicsState.FillPattern != newFillPattern)
                    {
                        graphicsState.FillPattern = newFillPattern;
                        textBuilder.InvalidateStyle();
                    }
                }
                else
                {
                    SetFillColor(new RgbColor(graphicsState.FillColorSpace, components));
                }
            }
        }

        [Operation("G")]
        private void G_StrokeGray(float gray)
        {
            if (!ignoreColorChange)
            {
                graphicsState.StrokeColorSpace = new DeviceGrayColorSpace();
                SetStrokeColor(new RgbColor(graphicsState.StrokeColorSpace, gray));
            }
        }

        [Operation("g")]
        private void g_FillGray(float gray)
        {
            if (!ignoreColorChange)
            {
                graphicsState.FillColorSpace = new DeviceGrayColorSpace();
                SetFillColor(new RgbColor(graphicsState.FillColorSpace, gray));
            }
        }

        [Operation("RG")]
        private void RG_StrokeRgb(float r, float g, float b)
        {
            if (!ignoreColorChange)
            {
                graphicsState.StrokeColorSpace = new DeviceRgbColorSpace();
                SetStrokeColor(new RgbColor(graphicsState.StrokeColorSpace, r, g, b));
            }
        }

        [Operation("rg")]
        private void rg_FillRgb(float r, float g, float b)
        {
            if (!ignoreColorChange)
            {
                graphicsState.FillColorSpace = new DeviceRgbColorSpace();
                SetFillColor(new RgbColor(graphicsState.FillColorSpace, r, g, b));
            }
        }

        [Operation("K")]
        private void K_StrokeCmyk(float c, float m, float y, float k)
        {
            if (!ignoreColorChange)
            {
                graphicsState.StrokeColorSpace = new DeviceCmykColorSpace();
                SetStrokeColor(new RgbColor(graphicsState.StrokeColorSpace, c, m, y, k));
            }
        }

        [Operation("k")]
        private void k_FillCmyk(float c, float m, float y, float k)
        {
            if (!ignoreColorChange)
            {
                graphicsState.FillColorSpace = new DeviceCmykColorSpace();
                SetFillColor(new RgbColor(graphicsState.FillColorSpace, c, m, y, k));
            }
        }

        #endregion

        #region Text state operators

        [Operation("Tc")]
        private void Tc_CharSpace(double charSpace)
        {
            if (graphicsState.TextCharSpacingPx != charSpace)
            {
                graphicsState.TextCharSpacingPx = charSpace;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("Tw")]
        private void Tw_WordSpace(double wordSpace)
        {
            graphicsState.TextWordSpacingPx = wordSpace;
        }

        [Operation("Tz")]
        private void Tz_Scale(double scale)
        {
            if (graphicsState.TextScaling != scale)
            {
                graphicsState.TextScaling = scale;
                textBuilder.InvalidateStyle();
                textBuilder.UpdateLineMatrix(graphicsState);
            }
        }

        [Operation("TL")]
        private void TL_Leading(double leading)
        {
            graphicsState.TextLeading = leading;
        }

        [Operation("Tf")]
        private void Tf_Font(PdfName fontName, double fontSize)
        {
            var newFont = resources.GetFont(fontName, options.FontResolver, documentCache, cancellationToken);
            if (newFont == null)
            {
                Log.WriteLine($"Could not find a font replacement for {fontName}.");
                newFont = BaseFont.Fallback;
            }

            SetFont(newFont, fontSize);
        }

#if HAVE_ASYNC
        [Operation("Tf")]
        private async Task Tf_FontAsync(PdfName fontName, double fontSize)
        {
            var newFont = await resources
                .GetFontAsync(fontName, options.FontResolver, documentCache, cancellationToken)
                .ConfigureAwait(false);

            if (newFont == null)
            {
                Log.WriteLine($"Could not find a font replacement for {fontName}.");
                newFont = BaseFont.Fallback;
            }

            SetFont(newFont, fontSize);
        }
#endif

        [Operation("gs/Font")]
        private void gs_Font(object[] args)
        {
            var font = graphicsState.Font;
            var fontSize = graphicsState.FontSize;

            if (args.Length > 0 && args[0] is PdfDictionary fontDict)
            {
                font = BaseFont.Create(fontDict, options.FontResolver, cancellationToken);
            }

            if (args.Length > 1 && MathUtils.ToDouble(args[1], out var dblFontSize))
            {
                fontSize = dblFontSize;
            }

            SetFont(font, fontSize);
        }

        private void SetFont(BaseFont newFont, double fontSize)
        {
            var outputStyleChanged =
                graphicsState.FontSize != fontSize ||
                !graphicsState.Font.SubstituteFont.Equals(newFont.SubstituteFont);

            graphicsState.Font = newFont;
            graphicsState.FontSize = fontSize;

            if (outputStyleChanged)
            {
                textBuilder.InvalidateStyle();
            }

            textBuilder.UpdateLineMatrix(graphicsState);
        }

        [Operation("Tr")]
        private void Tr_Renderer(int renderer)
        {
            var newRenderingMode = renderer switch
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

            if (graphicsState.TextRenderingMode != newRenderingMode)
            {
                graphicsState.TextRenderingMode = newRenderingMode;
                textBuilder.InvalidateStyle();
            }
        }

        [Operation("Ts")]
        private void Ts_Rise(double rise)
        {
            if (graphicsState.TextRisePx != rise)
            {
                graphicsState.TextRisePx = rise;
                textBuilder.InvalidateStyle();
            }
        }

        #endregion

        #region Text showing operators

        [Operation("Tj")]
        private void Tj_Show(PdfString text)
        {
            if (markedContentVisible)
            {
                textBuilder.AddSpan(graphicsState, text);
            }
            else
            {
                textBuilder.SkipSpan(graphicsState, text);
            }
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

            if (graphicsState.TextCharSpacingPx != charSpacing)
            {
                graphicsState.TextCharSpacingPx = charSpacing;
                textBuilder.InvalidateStyle();
            }

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
                    if (markedContentVisible)
                    {
                        textBuilder.AddSpan(graphicsState, text);
                    }
                    else
                    {
                        textBuilder.SkipSpan(graphicsState, text);
                    }
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

        private static List<TextParagraph> PrepareSvgSpans(List<TextParagraph> paragraphs, bool includeHiddenText)
        {
            // Horizontal scaling is resalized using the textLength attribute, which ensures any stroke
            // is not affected by the scaling. However, the support for textLength is rather buggy in 
            // some browsers (looking at you, Chrome), so we need to ensure textLength is only used on
            // <text> elements without any child <tspan>.

            var result = new List<TextParagraph>(paragraphs.Count);

            foreach (var paragraph in paragraphs)
            {
                if (paragraph.Type3Content != null)
                {
                    result.Add(paragraph);
                    continue;
                }

                var x = paragraph.X;
                TextParagraph? newParagraph = null;

                foreach (var span in paragraph.Content)
                {
                    var appendClipping =
                        (span.Style.TextRenderingMode & TextRenderingMode.Clip) != 0;

                    var visible =
                        (span.Style.TextRenderingMode & TextRenderingMode.Fill) != 0 ||
                        (span.Style.TextRenderingMode & TextRenderingMode.Stroke) != 0;

                    if (includeHiddenText || visible || appendClipping)
                    {
                        if (newParagraph == null ||

                            // Scaling is realized using textLength, which some renderers only reliable support on <text> elements, not <tspan>.
                            newParagraph.Content.Last().Style.TextScaling != 100 ||
                            span.Style.TextScaling != 100 ||

                            // Soft masks needs to be applied outside text transform => split paragraph when mask changes
                            newParagraph.Content.Last().Style.SMaskId != span.Style.SMaskId ||

                            // Split spans with separate display modes into separate paragraphs
                            visible != newParagraph.Visible ||
                            appendClipping != newParagraph.AppendClipping)
                        {
                            newParagraph = new TextParagraph
                            {
                                Matrix = paragraph.Matrix,
                                X = x,
                                Y = paragraph.Y,
                                Visible = visible,
                                AppendClipping = appendClipping,
                            };
                            result.Add(newParagraph);
                        }

                        newParagraph.Content.Add(span);
                    }

                    x += span.Width;
                }
            }

            return result;
        }

        private string? BuildTextCssClass(GraphicsState style, Matrix appliedTextTransform)
        {
            var className = default(string);
            var cssClass = new CssPropertyCollection();

            string? fontFamily = null;
            string? fontWeight = null;
            string? fontStyle = null;
            string? fontSize = SvgConversion.FormatFontMetric(style.FontSize) + "px";

            // Font
            var substituteFont = style.Font.SubstituteFont;
            if (style.HasGlyph0Reference && substituteFont is WebFont)
            {
                substituteFont = style.Font.GetSubstituteFontWithDuplicatedGlyph0(options.FontResolver, cancellationToken);
            }

            if (substituteFont is LocalFont localFont)
            {
                fontFamily = localFont.FontFamily;
                fontWeight = SvgConversion.FormatFontWeight(localFont.FontWeight);
                fontStyle = SvgConversion.FormatFontStyle(localFont.FontStyle);
            }
            else if (substituteFont is WebFont webFont)
            {
                if (webFont.FallbackFont == null)
                {
                    fontFamily = webFont.FontFamily;
                }
                else
                {
                    fontFamily = webFont.FontFamily + "," + webFont.FallbackFont.FontFamily;
                    fontWeight = SvgConversion.FormatFontWeight(webFont.FallbackFont.FontWeight);
                    fontStyle = SvgConversion.FormatFontStyle(webFont.FallbackFont.FontStyle);
                }

                if (fontFaceNames.Add(webFont.FontFamily))
                {
                    var sourceFonts = new[]
                    {
                        new { Format = "woff2", Url = webFont.Woff2Url },
                        new { Format = "woff", Url = webFont.WoffUrl },
                        new { Format = "opentype", Url = webFont.OpenTypeUrl },
                        new { Format = "truetype", Url = webFont.TrueTypeUrl },
                    };

                    var src = string.Join(",", sourceFonts
                        .Where(x => !string.IsNullOrEmpty(x.Url))
                        .Select(x => $"url('{x.Url}') format('{x.Format}')"));

                    var fontFace = new CssPropertyCollection
                    {
                        { "font-family", webFont.FontFamily },
                        { "font-weight", SvgConversion.FormatFontWeight(webFont.FallbackFont?.FontWeight) },
                        { "font-style", SvgConversion.FormatFontStyle(webFont.FallbackFont?.FontStyle) },
                        { "src", src },
                    };

                    AddStyle("@font-face{" + fontFace + "}");
                }
            }
            else
            {
                throw new PdfException("Unexpected font type.");
            }

            // Shorthand font property not used since it does not seem to be supported by Microsoft Office apps
            cssClass["font-family"] = fontFamily;
            cssClass["font-weight"] = fontWeight;
            cssClass["font-style"] = fontStyle;
            cssClass["font-size"] = fontSize;

            if (style.Font.HasGlyphSubstitutions)
            {
                // If the font contains glyph substitutions, e.g. ligatures, let's turn substitutions off in CSS, since
                // we will provide the correct Unicode char in the output SVG.
                cssClass["font-variant"] = "none";
            }

            if (style.TextCharSpacingPx != 0)
            {
                cssClass["letter-spacing"] = SvgConversion.FormatFontMetric(style.TextCharSpacingPx) + "px";
            }

            // Word spacing is precalculated and applied by the <text> position, since PDF and CSS don't
            // interpret word spacing the same way.

            // Fill
            if (style.TextRenderingMode.HasFlag(TextRenderingMode.Fill))
            {
                if (graphicsState.FillAlpha < MinAlpha)
                {
                    cssClass["fill"] = "none";
                }
                else
                {
                    var fill = GetFill(style, appliedTextTransform);
                    if (fill != "#000")
                    {
                        cssClass["fill"] = fill;
                    }

                    if (graphicsState.FillAlpha < MaxAlpha)
                    {
                        cssClass["fill-opacity"] = SvgConversion.FormatCoordinate(graphicsState.FillAlpha);
                    }
                }
            }
            else
            {
                cssClass["fill"] = "none";
            }

            // Stroke
            if (style.TextRenderingMode.HasFlag(TextRenderingMode.Stroke) && graphicsState.StrokeAlpha > MinAlpha)
            {
                // Color
                cssClass["stroke"] = GetStroke(style, appliedTextTransform);

                // Opacity
                if (graphicsState.StrokeAlpha < MaxAlpha)
                {
                    cssClass["stroke-opacity"] = SvgConversion.FormatCoordinate(graphicsState.StrokeAlpha);
                }

                // Width
                if (style.StrokeWidth != 1d)
                {
                    cssClass["stroke-width"] = SvgConversion.FormatCoordinate(style.StrokeWidth);
                }

                // Line cap
                if (style.StrokeLineCap == 1)
                {
                    cssClass["stroke-linecap"] = "round";
                }
                else if (style.StrokeLineCap == 2)
                {
                    cssClass["stroke-linecap"] = "square";
                }

                // Line join
                if (style.StrokeLineJoin == 1)
                {
                    cssClass["stroke-linejoin"] = "round";
                }
                else if (style.StrokeLineJoin == 2)
                {
                    cssClass["stroke-linejoin"] = "bevel";
                }
                else
                {
                    // Default to miter join

                    // Miter limit is applicable
                    // Default in SVG: 4
                    // Default in PDF: 10 (PDF 1.7 spec, Table 52)

                    if (style.StrokeMiterLimit != 10)
                    {
                        cssClass["stroke-miterlimit"] = SvgConversion.FormatCoordinate(style.StrokeMiterLimit);
                    }
                    else if (!svgHasDefaultMiterLimit)
                    {
                        // Change default miter limit on root element
                        rootGraphics.Add(new XAttribute("stroke-miterlimit", SvgConversion.FormatCoordinate(style.StrokeMiterLimit)));
                        svgHasDefaultMiterLimit = true;
                    }
                }

                // Dash
                if (style.StrokeDashArray != null &&
                    style.StrokeDashArray.Length > 0)
                {
                    cssClass["stroke-dasharray"] = string.Join(" ",
                        style.StrokeDashArray.Select(x => x.ToString(CultureInfo.InvariantCulture)));

                    if (style.StrokeDashPhase != 0)
                    {
                        cssClass["stroke-dashoffset"] = style.StrokeDashPhase.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            // Blend mode
            if (graphicsState.BlendMode != BlendMode.Normal)
            {
                cssClass["mix-blend-mode"] = SvgConversion.FormatBlendMode(graphicsState.BlendMode);
            }

            if (cssClass.Count > 0)
            {
                AddStyle("tx", cssClass, out className);
            }

            return className;
        }

        [Operation("ET")]
        private void ET_EndText()
        {
            var styleToClassNameLookup = new Dictionary<object, string?>();
            var clipElements = new List<XElement>();

            if (!hasTextStyle)
            {
                AddStyle(TextStyle);
                rootGraphics.SetAttributeValue("class", RootClassName);
                defs.SetAttributeValue("class", RootClassName);
                hasTextStyle = true;
            }

            foreach (var paragraph in PrepareSvgSpans(textBuilder.paragraphs, options.IncludeHiddenText))
            {
                if (paragraph.Type3Content != null)
                {
                    RenderType3Paragraph(paragraph);
                }
                else
                {
                    RenderTextParagraph(paragraph, styleToClassNameLookup, clipElements);
                }
            }

            if (clipElements.Count > 0)
            {
                AppendClipping(clipElements);
            }
        }

        private void RenderType3Paragraph(TextParagraph paragraph)
        {
            if (paragraph.Type3Content == null)
            {
                throw new ArgumentException("There must be Type 3 content specified for the paragraph.", nameof(paragraph));
            }

            var originalGraphicsStateStack = graphicsStateStack;
            var originalGraphicsState = graphicsState;
            var originalIgnoreColorChange = ignoreColorChange;
            var originalInType3Glyph = inType3Glyph;

            try
            {
                graphicsStateStack = new Stack<GraphicsState>();
                graphicsState = (paragraph.Type3Style ?? graphicsState).Clone();
                inType3Glyph = true;

                graphicsState.Transform = Matrix.Translate(paragraph.X, paragraph.Y, paragraph.Matrix);

                using (var contentStream = new MemoryStream(paragraph.Type3Content))
                {
                    foreach (var op in ContentParser.Parse(contentStream))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        dispatcher.Dispatch(this, op.Operator, op.Operands);
                    }
                }
            }
            finally
            {
                graphicsStateStack = originalGraphicsStateStack;
                graphicsState = originalGraphicsState;
                ignoreColorChange = originalIgnoreColorChange;
                inType3Glyph = originalInType3Glyph;
            }
        }

        private void RenderTextParagraph(TextParagraph paragraph, Dictionary<object, string?> styleToClassNameLookup, List<XElement> clipElements)
        {
            if (paragraph.Content.Count == 0)
            {
                return;
            }

            var singleSpan = paragraph.Content.Count == 1 ? paragraph.Content[0] : null;

            var textEl = new XElement(ns + "text");
            var paragraphWidth = 0.0;

            var x = paragraph.X;
            var y = paragraph.Y;
            var appliedTransform = Matrix.Identity;

            if (singleSpan != null)
            {
                x += singleSpan.SpaceBefore.FirstOrDefault();
                y -= singleSpan.Style.TextRisePx;
            }

            if (paragraph.Matrix.IsIdentity)
            {
                textEl.SetAttributeValue("x", SvgConversion.FormatCoordinate(x));
                textEl.SetAttributeValue("y", SvgConversion.FormatCoordinate(y));
            }
            else
            {
                appliedTransform = Matrix.Translate(x, y, paragraph.Matrix);
                textEl.SetAttributeValue("transform", SvgConversion.Matrix(appliedTransform));
            }

            if (singleSpan != null)
            {
                if (!styleToClassNameLookup.TryGetValue(singleSpan.Style, out string? className))
                {
                    className = BuildTextCssClass(singleSpan.Style, appliedTransform);
                    styleToClassNameLookup[singleSpan.Style] = className;
                }

                if (className != null)
                {
                    textEl.SetAttributeValue("class", className);
                }

                if (singleSpan.Style.TextScaling != 100)
                {
                    var textWidth = singleSpan.Width - singleSpan.SpaceBefore.FirstOrDefault();
                    textEl.SetAttributeValue("textLength", SvgConversion.FormatCoordinate(textWidth) + "px");
                    textEl.SetAttributeValue("lengthAdjust", "spacingAndGlyphs");
                }

                if (singleSpan.SpaceBefore.Count > 1)
                {
                    // The spacing will also be scaled when rendered, so we need to reverse the scaling
                    var dxMultiplier = 100 / singleSpan.Style.TextScaling;
                    var dx = "0 " + string.Join(" ", singleSpan.SpaceBefore.Skip(1).Select(x => SvgConversion.FormatTextOffset(x * dxMultiplier)));
                    textEl.SetAttributeValue("dx", dx);
                }

                textEl.Value = singleSpan.Value.ToString();

                paragraphWidth = singleSpan.Width;
            }
            else
            {
                var currentYOffset = 0.0;

                var classNames = new string?[paragraph.Content.Count];
                var multipleClasses = false;
                var totalLength = 0;

                for (var i = 0; i < classNames.Length; i++)
                {
                    var span = paragraph.Content[i];
                    var style = span.Style;

                    if (!styleToClassNameLookup.TryGetValue(style, out var className))
                    {
                        styleToClassNameLookup[style] = className = BuildTextCssClass(style, appliedTransform);
                    }

                    classNames[i] = className;

                    if (i > 0 && className != classNames[i - 1])
                    {
                        multipleClasses = true;
                    }

                    totalLength += span.Value.Length;
                    paragraphWidth += span.Width;
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

                    if (span.SpaceBefore.Count > 0)
                    {
                        var dx = string.Join(" ", span.SpaceBefore.Select(x => SvgConversion.FormatTextOffset(x)));
                        tspan.SetAttributeValue("dx", dx);
                    }

                    var dy = SvgConversion.FormatTextOffset(currentYOffset - span.Style.TextRisePx);
                    if (dy != "0")
                    {
                        tspan.SetAttributeValue("dy", dy);
                        currentYOffset = span.Style.TextRisePx;
                    }

                    tspan.Value = span.Value.ToString();
                    textEl.Add(tspan);
                }
            }

            // Soft mask
            textEl = ApplySMask(textEl, paragraph.Content[0].Style.SMaskId);

            if (paragraph.Visible || options.IncludeHiddenText)
            {
                var ignoreClipRect = false;

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
                        // We can be reasonably sure the text is entirely contained within the clip rectangle.
                        // Skip clipping. This significally increases the print quality in Internet Explorer, 
                        // which seems to rasterize all clipped graphics before printing.
                        ignoreClipRect = true;
                    }
                }

                if (ignoreClipRect)
                {
                    clipWrapper = null;
                    clipWrapperId = null;
                    currentTransparencyGroup.Add(textEl);
                }
                else
                {
                    AppendClipped(textEl);
                }
            }

            if (paragraph.AppendClipping)
            {
                clipElements.Add(textEl);
            }
        }

        [Operation("d0")]
        private void d0_Type3Width(double wx, double wy)
        {
        }

        [Operation("d1")]
        private void d1_Type3WidthAndBbox(double wx, double wy, double llx, double lly, double urx, double ury)
        {
            if (inType3Glyph)
            {
                ignoreColorChange = true;
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
