<div align="center">
    <img src="docs/images/banner.png" alt="PdfToSvg.NET Logo" style="width: 100%">
</div>
<div align="center">

[![Tests](https://img.shields.io/github/workflow/status/dmester/pdftosvg.net/Build%20and%20test/master?style=flat-square)](https://github.com/dmester/pdftosvg.net/actions)
[![NuGet](https://img.shields.io/nuget/vpre/PdfToSvg.NET?style=flat-square)](https://www.nuget.org/packages/PdfToSvg.NET/)
[![License MIT](https://img.shields.io/badge/license-MIT-green.svg?style=flat-square)](https://github.com/dmester/pdftosvg.net/blob/master/LICENSE)

</div>

# PdfToSvg.NET
Fully managed library for converting PDF files to SVG. Potential usage is embedding PDFs on your site without the need of loading a PDF reader.

## State
There are PDF features not yet implemented by this library. Don't use it as a general PDF conversion tool for any PDF. However it should be fine if used on PDFs from a specific PDF producer, after thorough testing on PDFs created by that particular producer.

New versions of the library might include breaking changes to the public API until version 1.0 is released.

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
