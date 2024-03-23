# PdfToSvg.NET

[![Tests](https://img.shields.io/github/actions/workflow/status/dmester/pdftosvg.net/build.yml?branch=master&style=flat-square)](https://github.com/dmester/pdftosvg.net/actions)
[![NuGet](https://img.shields.io/nuget/vpre/PdfToSvg.NET?style=flat-square)](https://www.nuget.org/packages/PdfToSvg.NET/)
[![License MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://github.com/dmester/pdftosvg.net/blob/master/LICENSE)

Fully managed library for converting PDF files to SVG. Potential usage is seamlessly embedding PDFs on your site without the need of a PDF reader.

🚀 [**Test it online**](https://pdftosvg.net/) &nbsp;&nbsp;
🏠 [**Homepage**](https://pdftosvg.net/) &nbsp;&nbsp;
📖 [**API documentation**](https://pdftosvg.net/api/) &nbsp;&nbsp;
📜 [**Release notes**](https://github.com/dmester/pdftosvg.net/releases)

## Features

* Extracts text, fonts and images from PDF files into SVG
* Supports .NET 5, .NET Core 1.0, .NET Standard 1.6, .NET Framework 4.0 and later
* Focus on producing compact SVG markup ready for the web
* Dependency free

## Quick start
Install the [PdfToSvg.NET NuGet package](https://www.nuget.org/packages/PdfToSvg.NET/).

```
PM> Install-Package PdfToSvg.NET
```

Open a PDF document by calling [`PdfDocument.Open`](https://pdftosvg.net/api/M_PdfToSvg_PdfDocument_Open_1). Call [`SaveAsSvg()`](https://pdftosvg.net/api/M_PdfToSvg_PdfPage_SaveAsSvg_1) on each page to convert it to an SVG file.

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

## Command line usage

You can also download the [CLI version](https://github.com/dmester/pdftosvg.net/releases/latest/download/pdftosvg.exe) of PdfToSvg.NET and use it from the command line.

```
pdftosvg.exe input.pdf output.svg
```

[Command line reference](https://pdftosvg.net/cli)
