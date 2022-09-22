# Font resolver

Fonts embedded in a PDF file are since version 0.8.0 by default extracted by PdfToSvg.NET. You can also choose to use locally installed fonts, which will reduce the SVG size, by specifying a font resolver.

## Standard font resolvers

PdfToSvg.NET comes with the following bundled font resolvers:

| Font resolver | Description |
| ------------- | ------------|
| `FontResolver.LocalFonts` | Font resolver substituting fonts in the PDF with commonly available fonts. No fonts are embedded in the resulting SVG. The resolved fonts need to be available on the viewing machine. |
| `FontResolver.EmbedWoff` | Font resolver converting fonts in the PDF to WOFF format and embedding them in the output SVG. If the font cannot be converted, this resolver falls back to the `LocalFonts` resolver. |
| `FontResolver.EmbedOpenType` | Font resolver converting fonts in the PDF to OpenType (.otf) format and embedding them in the output SVG. If the font cannot be converted, this resolver falls back to the `LocalFonts` resolver. |
| `FontResolver.Default` | Gets the default font resolver used when no resolver is explicitly specified. Currently `EmbedWoff` is the default font resolver, but this can change in the future. |

## Custom font resolver

A custom font resolver can be created by implementing the `FontResolver` abstract class. The font resolver should return any of the following font types:

| Font type | Description |
| --------- | ----------- |
| `LocalFont` | A font that is assumed to be installed on the machine viewing the SVG. |
| `WebFont` | Use a provided TrueType, OpenType, WOFF or WOFF2 font. Note that external resources are not allowed in standalone SVG files when displayed in browsers, so if you intend to use external SVG files, you need to return a `WebFont` instance using [data URIs](https://en.wikipedia.org/wiki/Data_URI_scheme) only. |

Here is a simple implementation using an locally installed Open Sans font.

```csharp
class OpenSansFontResolver : FontResolver
{
    public override Font ResolveFont(SourceFont sourceFont, CancellationToken cancellationToken)
    {
        var font = FontResolver.LocalFonts.ResolveFont(sourceFont, cancellationToken);

        if (sourceFont.Name != null &&
            sourceFont.Name.Contains("OpenSans", StringComparison.InvariantCultureIgnoreCase) &&
            font is LocalFont localFont)
        {
            font = new LocalFont("'Open Sans',sans-serif", localFont.FontWeight, localFont.FontStyle);
        }

        return font;
    }
}
```

The font resolver can be passed to any of the conversion methods:

```csharp
using (var doc = PdfDocument.Open("input.pdf"))
{
    var options = new SvgConversionOptions
    {
        FontResolver = new OpenSansFontResolver()
    };

    var pageIndex = 0;

    foreach (var page in doc.Pages)
    {
        page.SaveAsSvg($"output-{pageIndex++}.svg", options);
    }
}
```

In this example, a font called "Open Sans" must be available when the SVG is displayed, either installed locally on the client machine, or by including it as a web font outside the SVG.
