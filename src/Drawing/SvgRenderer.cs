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
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg.Drawing
{
    internal class SvgRenderer
    {
        private readonly static XNamespace ns = "http://www.w3.org/2000/svg";
        
        private TextState textState => graphicsState.TextState;
        private GraphicsState graphicsState = new GraphicsState();

        private Stack<GraphicsState> graphicsStateStack = new Stack<GraphicsState>();

        private XElement svg;

        private PathData currentPath = new PathData();

        private double currentPointX, currentPointY;

        private ResourceCache resources;

        private XElement defs = new XElement(ns + "defs");

        private HashSet<string> defIds = new HashSet<string>();

        private XElement clipWrapper;
        private string clipWrapperId;

        private static readonly OperationDispatcher dispatcher = new OperationDispatcher(typeof(SvgRenderer));

        private XElement style = new XElement(ns + "style", "text { white-space: pre; }");
        private HashSet<string> styleClassIds = new HashSet<string>();
        private HashSet<string> fontFaceNames = new HashSet<string>();

        private TextBuilder textBuilder = new TextBuilder();

        private SvgConversionOptions options;

        private Dictionary<string, ClipPath> clipPaths = new Dictionary<string, ClipPath>();

        private SvgRenderer(PdfDictionary pageDict, SvgConversionOptions options)
        {
            this.options = options;

            resources = new ResourceCache(
                pageDict.GetValueOrDefault<PdfDictionary>(Names.Resources, null) ?? new PdfDictionary());
            
            Rectangle cropBox;

            if (!pageDict.TryGetRectangle(Names.CropBox, out cropBox) &&
                !pageDict.TryGetRectangle(Names.MediaBox, out cropBox))
            {
                // Default to A4
                cropBox = RectangleUtils.GetA4();
            }

            svg = new XElement(ns + "svg",
                new XAttribute("width", cropBox.Width.ToString("0", CultureInfo.InvariantCulture)),
                new XAttribute("height", cropBox.Height.ToString("0", CultureInfo.InvariantCulture)),
                new XAttribute("preserveAspectRatio", "xMidYMid meet"),
                new XAttribute("viewBox", 
                    string.Format(CultureInfo.InvariantCulture, "0 0 {0:0.####} {1:0.####}",
                    cropBox.Width, cropBox.Height
                )),
                style,
                defs);

            // PDF coordinate system has its origin in the bottom left corner in opposite to SVG, 
            // which has its origin in the upper left corner.
            graphicsState.Transform = Matrix.Translate(0, -cropBox.Height, Matrix.Scale(1, -1));

            // Move origin
            graphicsState.Transform = Matrix.Translate(-cropBox.X1, -cropBox.Y1, graphicsState.Transform);
        }

        private void Convert(Stream contentStream)
        {
            // TODO For debugging, remove once no longer needed
            // var reader = new StreamReader(contentStream);
            // var content = reader.ReadToEnd();

            foreach (var op in ContentParser.Parse(contentStream))
            {
                dispatcher.Dispatch(this, op.Operator, op.Operands);
            }

            // Add clip paths
            AddClipPaths(clipPaths.Values);
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

        public static XElement Convert(PdfDictionary pageDict, SvgConversionOptions options)
        {
            var renderer = new SvgRenderer(pageDict, options);
            var contentStream = ContentStream.Combine(pageDict);
            renderer.Convert(contentStream);
            return renderer.svg;
        }

        public static async Task<XElement> ConvertAsync(PdfDictionary pageDict, SvgConversionOptions options)
        {
            var renderer = new SvgRenderer(pageDict, options);
            var contentStream = await ContentStream.CombineAsync(pageDict);
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
            graphicsState.LineWidth = lineWidth;
        }

        [Operation("J")]
        [Operation("gs/LC")]
        private void J_LineCap(int lineCap)
        {
            graphicsState.LineCap = lineCap;
        }

        [Operation("j")]
        [Operation("gs/LJ")]
        private void j_LineJoin(int lineJoin)
        {
            graphicsState.LineJoin = lineJoin;
        }

        [Operation("M")]
        [Operation("gs/ML")]
        private void M_MiterLimit(double miterLimit)
        {
            graphicsState.MiterLimit = miterLimit;
        }

        [Operation("d")]
        private void d_DashArray(int[] dashArray, int dashPhase)
        {
            graphicsState.DashArray = dashArray;
            graphicsState.DashPhase = dashPhase;
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
        
        private string GetSvgImageId(PdfDictionary imageObject, out int width, out int height)
        {
            if (imageObject.Stream != null &&
                imageObject.TryGetName(Names.Subtype, out var subtype) && subtype == Names.Image &&
                imageObject.TryGetInteger(Names.Width, out width) &&
                imageObject.TryGetInteger(Names.Height, out height))
            {
                var colorSpace = GetColorSpace(imageObject[Names.ColorSpace]);
                if (colorSpace == null)
                {
                    // TODO handle for masks
                    // PDF spec 1.7, Table 89
                    return null;
                }

                var image = ImageFactory.Create(imageObject, colorSpace);
                if (image != null)
                {
                    // TODO add Dictinary<Image, string> to ensure stable ids
                    var imageResolver = options.ImageResolver ?? new DataUriImageResolver();
                    var imageUrl = imageResolver.ResolveImageUrl(image);
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
                            svgImage.SetAttributeValue("image-rendering", "pixelated");
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
            q_SaveState();
            
            var previousResources = resources;

            resources = new ResourceCache(xobject.GetValueOrDefault<PdfDictionary>(Names.Resources, null) ?? new PdfDictionary());

            if (xobject.TryGetArray<double>(Names.Matrix, out var matrix) && matrix.Length == 6)
            {
                graphicsState.Transform = graphicsState.Transform *
                    new Matrix(matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5]);
            }

            if (xobject.TryGetArray<double>(Names.BBox, out var bbox) && bbox.Length == 4)
            {
                re_Rectangle(bbox[0], bbox[1], bbox[2] - bbox[0], bbox[3] - bbox[1]);
                W_Clip_NonZero();
                n_EndPath();
            }

            foreach (var operation in ContentParser.Parse(xobject.Stream.OpenDecoded()))
            {
                dispatcher.Dispatch(this, operation.Operator, operation.Operands);
            }

            resources = previousResources;
            
            Q_RestoreState();
        }

        private void RenderImage(PdfDictionary xobject)
        {
            var imageId = GetSvgImageId(xobject, out var imageWidth, out var imageHeight);
            if (imageId == null)
            {
                return;
            }

            var imageAttributes = new List<XAttribute>();

            // Positioning
            var imageTransform = Matrix.Translate(0, -1) * Matrix.Scale(1, -1) * graphicsState.Transform;

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

            var clipPath = new ClipPath
            {
                Parent = graphicsState.ClipPath,
                EvenOdd = evenOdd,
                Data = path,
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

                return svg;
            }
            else
            {
                if (clipWrapperId != graphicsState.ClipPath.Id)
                {
                    clipWrapper = new XElement(ns + "g", new XAttribute("clip-path", "url(#" + graphicsState.ClipPath.Id + ")"));
                    clipWrapperId = graphicsState.ClipPath.Id;
                    svg.Add(clipWrapper);

                    var cursor = graphicsState.ClipPath;
                    while (cursor != null && !cursor.Referenced)
                    {
                        cursor.Referenced = true;
                        cursor = cursor.Parent;
                    }
                }

                return clipWrapper;
            }
        }

        private void AppendClipped(XElement el)
        {
            GetClipParent().Add(el);
        }

        private void DrawPath(bool stroke, bool fill, bool evenOddWinding)
        {
            currentPath = currentPath.Transform(graphicsState.Transform);

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
                var lineWidth = graphicsState.LineWidth;
                if (lineWidth == 0)
                {
                    lineWidth = 1;
                }

                attributes.Add(new XAttribute("stroke", SvgConversion.FormatColor(graphicsState.StrokeColor)));
                attributes.Add(new XAttribute("stroke-width", lineWidth.ToString("0.####", CultureInfo.InvariantCulture)));

                if (graphicsState.LineCap == 1)
                {
                    attributes.Add(new XAttribute("stroke-linecap", "round"));
                }
                else if (graphicsState.LineCap == 2)
                {
                    attributes.Add(new XAttribute("stroke-linecap", "square"));
                }

                if (graphicsState.LineJoin == 1)
                {
                    attributes.Add(new XAttribute("stroke-linejoin", "round"));
                }
                else if (graphicsState.LineJoin == 2)
                {
                    attributes.Add(new XAttribute("stroke-linejoin", "bevel"));
                }

                if (graphicsState.DashArray != null)
                {
                    attributes.Add(new XAttribute("stroke-dasharray", string.Join(" ",
                        graphicsState.DashArray.Select(x => x.ToString(CultureInfo.InvariantCulture)))));

                    if (graphicsState.DashPhase != 0)
                    {
                        attributes.Add(new XAttribute("stroke-dashoffset", 
                            graphicsState.DashPhase.ToString(CultureInfo.InvariantCulture)));
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
                svg.Add(el);
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

        private ColorSpace GetColorSpace(object definition)
        {
            if (definition is PdfName name)
            {
                return resources.GetColorSpace(name);
            }

            return ColorSpace.Parse(definition, resources.Dictionary.GetValueOrDefault(Names.ColorSpace, PdfDictionary.Null));
        }

        [Operation("CS")]
        private void CS_StrokeColorSpace(PdfName name)
        {
            graphicsState.StrokeColorSpace = GetColorSpace(name) ?? new DeviceRgbColorSpace();
            graphicsState.StrokeColor = graphicsState.StrokeColorSpace.GetDefaultRgbColor();
            textBuilder.InvalidateStyle();
        }
        
        [Operation("cs")]
        private void cs_FillColorSpace(PdfName name)
        {
            graphicsState.FillColorSpace = GetColorSpace(name) ?? new DeviceRgbColorSpace();
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
            textState.CharSpacing = charSpace;
            textBuilder.InvalidateStyle();
        }

        [Operation("Tw")]
        private void Tw_WordSpace(double wordSpace)
        {
            textState.WordSpacing = wordSpace;
        }

        [Operation("Tz")]
        private void Tz_Scale(double scale)
        {
            textState.Scaling = scale;
            textBuilder.InvalidateStyle();
        }

        [Operation("TL")]
        private void TL_Leading(double leading)
        {
            textState.Leading = leading;
        }

        [Operation("Tf")]
        private void Tf_Font(PdfName fontName, double fontSize)
        {
            textState.Font = resources.GetFont(fontName, options.FontResolver ?? DefaultFontResolver.Instance);
            textState.FontSize = fontSize;

            if (textState.Font == null)
            {
                Log.WriteLine($"Could not find a font replacement for {fontName}.");
                textState.Font = InternalFont.Fallback;
            }

            textBuilder.InvalidateStyle();
            textBuilder.UpdateLineMatrix(graphicsState);
        }

        [Operation("Tr")]
        private void Tr_Renderer(int renderer)
        {
            switch (renderer)
            {
                case 0: textState.RenderingMode = TextRenderingMode.Fill; break;
                case 1: textState.RenderingMode = TextRenderingMode.Stroke; break;
                case 2: textState.RenderingMode = TextRenderingMode.Fill | TextRenderingMode.Stroke; break;
                case 3: textState.RenderingMode = TextRenderingMode.None; break;
                case 4: textState.RenderingMode = TextRenderingMode.Fill | TextRenderingMode.Clip; break;
                case 5: textState.RenderingMode = TextRenderingMode.Stroke | TextRenderingMode.Clip; break;
                case 6: textState.RenderingMode = TextRenderingMode.Fill | TextRenderingMode.Stroke | TextRenderingMode.Clip; break;
                case 7: textState.RenderingMode = TextRenderingMode.Clip; break;
                default: textState.RenderingMode = TextRenderingMode.Fill; break;
            }

            textBuilder.InvalidateStyle();
        }

        [Operation("Ts")]
        private void Ts_Rise(double rise)
        {
            textState.Rise = rise;
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
            textState.WordSpacing = wordSpacing;
            textState.CharSpacing = charSpacing;
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
            graphicsState.TextState.TextMatrix = Matrix.Identity;
            textBuilder.Clear();
            textBuilder.UpdateLineMatrix(graphicsState);
        }

        private List<TextParagraph> PrepareSvgSpans(List<TextParagraph> paragraphs, TextRenderingMode includedModes)
        {
            // Horizontal scaling is resalized using the textLength attribute, which ensures any stroke
            // is not affected by the scaling. However, the support for textLength is rather buggy in 
            // some browsers (looking at you, Chrome), so we need to ensure textLength is only used on
            // <text> elements without any child <tspan>.

            var result = new List<TextParagraph>(paragraphs.Count);

            foreach (var paragraph in paragraphs)
            {
                var x = paragraph.X;
                TextParagraph newParagraph = null;

                foreach (var span in paragraph.Content)
                {
                    if ((span.Style.RenderingMode & includedModes) != 0)
                    {
                        if (newParagraph == null ||
                            newParagraph.Content.Last().Style.Scaling != 100 ||
                            span.Style.Scaling != 100)
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

        private string BuildCssClass(TextStyle style)
        {
            var className = default(string);
            var cssClass = new CssPropertyCollection();

            if (style.RenderingMode.HasFlag(TextRenderingMode.Fill))
            {
                if (style.Fill != RgbColor.Black)
                {
                    cssClass["fill"] = SvgConversion.FormatColor(style.Fill);
                }
            }
            else if (style.RenderingMode.HasFlag(TextRenderingMode.Stroke))
            {
                cssClass["fill"] = "none";
            }

            if (style.RenderingMode.HasFlag(TextRenderingMode.Stroke))
            {
                cssClass["stroke"] = SvgConversion.FormatColor(style.Stroke);
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

            if (style.CharSpacingPx != 0)
            {
                cssClass["letter-spacing"] = SvgConversion.FormatCoordinate(style.CharSpacingPx) + "px";
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
            var styleToClassNameLookup = new Dictionary<TextStyle, string>();

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
                    y += singleSpan.Style.RisePx;
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
                    if (!styleToClassNameLookup.TryGetValue(singleSpan.Style, out var className))
                    {
                        styleToClassNameLookup[singleSpan.Style] = className = BuildCssClass(singleSpan.Style);
                    }

                    if (className != null)
                    {
                        textEl.SetAttributeValue("class", className);
                    }

                    if (singleSpan.Style.Scaling != 100)
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

                    var classNames = new string[paragraph.Content.Count];
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

                        tspan.SetAttributeValue("class", classNames[i]);

                        if (span.SpaceBefore != 0)
                        {
                            tspan.SetAttributeValue("dx", SvgConversion.FormatCoordinate(span.SpaceBefore));
                        }

                        if (span.Style.RisePx != currentYOffset)
                        {
                            tspan.SetAttributeValue("dy", SvgConversion.FormatCoordinate(currentYOffset - span.Style.RisePx));
                            currentYOffset = span.Style.RisePx;
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
                        svg.Add(textEl);
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
            textState.TextMatrix = textState.LineMatrix = new Matrix(a, b, c, d, e, f);
            textBuilder.UpdateLineMatrix(graphicsState);
        }

        [Operation("T*")]
        private void Tx_StartOfLine()
        {
            Td(0, -textState.Leading);
        }

        [Operation("Td")]
        private void Td(double tx, double ty)
        {
            textState.TextMatrix = textState.LineMatrix = Matrix.Translate(tx, ty, textState.LineMatrix);
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
