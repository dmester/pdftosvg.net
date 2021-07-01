# Getting started

The easiest way of getting started with PdfToSvg.NET is by installing the NuGet package.

```
PM> Install-Package PdfToSvg.NET
```

Open a PDF document by calling `PdfDocument.Open`. Call `SaveAsSvg()` on each page to convert it to an SVG file.

```csharp
using (var doc = PdfDocument.Open("input.pdf"))
{
    var pageIndex = 0;

    foreach (var page in doc.Pages)
    {
        page.SaveAsSvg($"output-{pageIndex++}.svg");
    }
}
```

Note that if you parse the XML returned from PdfToSvg.NET, you need to preserve space and not add indentation.
Otherwise text will not be rendered correctly in the modified markup.
