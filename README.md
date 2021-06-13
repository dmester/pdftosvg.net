<div align="center">
    <img src="src/logo.svg" alt="PdfToSvg.NET Logo" width="200" height="200">
</div>

# PdfToSvg.NET
[![Tests](https://img.shields.io/github/workflow/status/dmester/pdftosvg.net/Tests/master?style=flat-square)](https://github.com/dmester/pdftosvg.net/actions)
[![NuGet](https://img.shields.io/nuget/vpre/PdfToSvg.NET?style=flat-square)](https://www.nuget.org/packages/PdfToSvg.NET/)
[![License MIT](https://img.shields.io/badge/license-MIT-green.svg?style=flat-square)](https://github.com/dmester/pdftosvg.net/blob/master/LICENSE)

Fully managed library for converting PDF files to SVG. Potential usage is embedding PDFs on your site without the need of loading a PDF reader.

## State
The library is still under active development and should not yet be used in production code.

## Install
Install the [PdfToSvg.NET NuGet package](https://www.nuget.org/packages/PdfToSvg.NET/).

```
PM> Install-Package PdfToSvg.NET
```

## Usage

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

## Limitations
Not all PDF features are supported. Here is a summary of non-supported features:

* Embedded fonts from the PDF are not exported to the SVG. There is however an API for specifying substitute fonts during the conversion.
* Opening encrypted PDF files.
* Blending modes.
* PDF forms.
* JavaScript embedded in the PDF.

Non-supported image formats:
* CCITT Fax
* JPEG 2000
* CMYK JPEG
* JBIG2

Non-supported color spaces:
* CalGray
* CalRGB
* Lab
* DeviceN
* Pattern
* ICCBased
