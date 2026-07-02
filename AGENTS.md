

## Coding

Main project `src/PdfToSvg`

## General guidelines

1. No external package dependencies are allowed in the `src/PdfToSvg/PdfToSvg.csproj` project
2. External package dependencies are allowed in other projects if needed
3. It is not allowed to port code from other projects, even if it is open source, without asking the user
4. Specifications are the main source of truth. Open source projects with licenses MIT, BSD or Apache may be used as reference if necessary
5. Corrupt PDFs are very common. We should try to decode as much information as possible, even if the content does not strictly adheres to the specification
6. If implementing code from a specification, include short references to the spec, like `// ITU-T T.800 (06/2019) Section 4.5`
7. Smaller shims for members missing in older target frameworks can be added to the `PdfToSvg.Shims` namespace

## Code style (`.cs` files)

* Indent with 4 spaces; `.csproj` files use 2 spaces
* UTF-8 with BOM, final newline required
* Use LF line endings
* Lines may be up to 120 columns. Fill lines toward that limit - do not wrap at 80 or 100 columns when more fits.
* Always specify accessibility modifiers on non-interface members
* Always call `ConfigureAwait(false)` on awaited `Task`s in library code
* `CancellationToken` parameters must come last, and must be forwarded to any awaited calls that accept one
* New source files need the standard header:

  ```
  Copyright (c) PdfToSvg.NET contributors.
  https://github.com/dmester/pdftosvg.net
  Licensed under the MIT License.
  ```
* Don't use single line or bracket-less `if` statements. Use full `if` statements instead:
  ```
  if (condition)
  {
    // Do stuff
  }
  ```

## Tests

Run `tests/PdfToSvg.Tests` during development (`tests/PdfToSvg.RealLifeTests` are intended for regression testing before release).

If you are modifying vectorized code, also run `tests/PdfToSvg.Tests.Scalar` and `tests/PdfToSvg.Tests.Vector128`.

End-to-end tests (`tests/TestFiles/Own/input`) using test PDFs are preferred.

Test PDF rules:
* Should as far as possible be plain text, to allow inspection in text editors
* Binary object streams should be encoded with `/ASCIIHexDecode`
* Test files must be crafted. Third party PDFs are generally not allowed
* Object dictionaries should be indented
* Objects should be separated by a blank line
* PDFs testing fonts should have a file name prefixed `fonts-`. Those are tested with fonts both embedded and not embedded.
* Test projects run NUnit

Unit tests:
* NUnit is used
* Put test classes in the same folder structure as the main codebase
* Name test classes `<ClassUnderTest>Tests`
* Name test methods `<MethodUnderTest>_<VeryShortSummary>()`
* Prefer the `Assert.AreEqual` family over `Assert.That`
