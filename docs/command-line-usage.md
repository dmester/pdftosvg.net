# Command line usage

There is a command line tool for PdfToSvg.NET that can be used for converting PDF files to SVG.

📥 [**Download latest Windows binaries**](https://github.com/dmester/pdftosvg.net/releases/latest/download/pdftosvg.exe)

.NET Framework 4.5 is required to run the tool.

## Usage
```
pdftosvg.exe [OPTIONS...] <input> [<output>]
```

## Options

#### `<input>`
Path to the input PDF file.

#### `<output>`
Path to the output SVG file(s). A page number will be appended to the filename.

**Default:** Same as `<input>`, but with `.svg` as extension.

#### `--pages <pages>`
Pages to convert.

Syntax:

| Example  | Description                   |
| -------- | ----------------------------- |
| `12..15` | Converts page 12 to 15.       |
| `12,15`  | Converts page 12 and 15.      |
| `12..`   | Converts page 12 and forward. |
| `..15`   | Converts page 1 to 15.        |

**Default:** all pages

#### `--password <password>`
Owner or user password for opening the input file. By specifying the owner password, any access restrictions are bypassed.

#### `--no-color`
Disables colored text output in the console.

## Examples

```
pdftosvg.exe input.pdf output.svg --pages 1..2,9
```

Converts page 1, 2 and 9 from `input.pdf` to the output files:

* `output-1.svg`
* `output-2.svg`
* `output-9.svg`
