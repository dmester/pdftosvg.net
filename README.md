<div align="center">
<img src="docs/images/logo.svg" alt="PdfToSvg.NET Logo" width="180" height="180">
<br><br>

[![Tests](https://img.shields.io/github/workflow/status/dmester/pdftosvg.net/Build%20and%20test/master?style=flat-square)](https://github.com/dmester/pdftosvg.net/actions)
[![NuGet](https://img.shields.io/nuget/vpre/PdfToSvg.NET?style=flat-square)](https://www.nuget.org/packages/PdfToSvg.NET/)
[![License MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://github.com/dmester/pdftosvg.net/blob/master/LICENSE)

</div>

# PdfToSvg.NET
Fully managed library for converting PDF files to SVG. Potential usage is seamlessly embedding PDFs on your site without the need of a PDF reader.

🚀 [**Test it online**](https://pdftosvg.net/) &nbsp;&nbsp;
🏠 [**Homepage**](https://pdftosvg.net/) &nbsp;&nbsp;
📦 [**NuGet package**](https://www.nuget.org/packages/PdfToSvg.NET/) &nbsp;&nbsp;
📜 [**Release notes**](https://github.com/dmester/pdftosvg.net/releases)

## Features

* Extracts text and images from PDF files into SVG.
* Supports .NET 5, .NET Core 1.0, .NET Standard 1.6, .NET Framework 4.0 and later.
* Focus on producing compact SVG markup ready for the web.
* Almost dependency free.

## State
There are PDF features not yet implemented by this library. Before using it, please do rigorous testing of PDFs from the PDF producer you intend to convert, to ensure it does not use any features not supported by PdfToSvg.NET.

📖 [Read more about limitations](docs/limitations.md)

⚠️ New versions of the library might include breaking changes to the public API until version 1.0 is released.

## Quick start
Install the [PdfToSvg.NET NuGet package](https://www.nuget.org/packages/PdfToSvg.NET/).

```
PM> Install-Package PdfToSvg.NET
```

Open a PDF document by calling `PdfDocument.Open`. Call `SaveAsSvg()` on each page to convert it to an SVG file.

```csharp
using (var doc = PdfDocument.Open("input.pdf"))
{
    var pageNo = 1;

    foreach (var page in doc.Pages)
    {
        page.SaveAsSvg($"output-{pageNo++}.svg");
    }
}
```

Note that if you parse the XML returned from PdfToSvg.NET, you need to preserve space and not add indentation.
Otherwise text will not be rendered correctly in the modified markup.

## Issues
Did the conversion fail? Before reporting an issue, please read the documentaton about [current limitations](docs/limitations.md). If you believe it is a bug, ensure the  PDF file causing problems is attached in the issue.
