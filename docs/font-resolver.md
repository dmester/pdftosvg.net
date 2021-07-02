# Font resolver

Fonts embedded in a PDF file are not extracted by PdfToSvg.NET. By default PdfToSvg.NET will try to detect commonly used fonts and replace them with an appropriate substitute font, but you can also specify a custom font resolver to specify which font to be used as substitute for the embedded fonts.

A font resolver is implemented as a class implementing the `IFontResolver` interface.

## Interface `IFontResolver`

### `Font ResolveFont(string fontName, CancellationToken cancellationToken)`
Method called by PdfToSvg.NET to resolve a substitute font by a font name specified in the PDF file.

## Example

Here is a simple implementation overriding the custom behavior to support Open Sans.

```csharp
class MyFontResolver : IFontResolver 
{
    public Font ResolveFont(string fontName, CancellationToken cancellationToken)
    {
        var font = DefaultFontResolver.Instance.ResolveFont(fontName, cancellationToken);

        if (fontName.Contains("OpenSans", StringComparison.InvariantCultureIgnoreCase) &&
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
        FontResolver = new MyFontResolver()
    };

    var pageIndex = 0;

    foreach (var page in doc.Pages)
    {
        page.SaveAsSvg($"output-{pageIndex++}.svg", options);
    }
}
```

In this example, a font called "Open Sans" must be available when the SVG is displayed, either installed locally on the client machine, or by including it as a web font outside the SVG.

## Class `WebFont`
You can also return a `WebFont` instance from the font resolver.

Note that external resources are not allowed in standalone SVG files when displayed in browsers, so if you intend to use external SVG files, you need to return a `WebFont` instance using [data URLs](https://en.wikipedia.org/wiki/Data_URI_scheme) only.
