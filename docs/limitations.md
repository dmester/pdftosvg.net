# Limitations

PDF is a very feature rich file format, and PdfToSvg.NET does not support all of them. If a PDF is using an unsupported feature, the output might not look idential to what you see in other PDF readers.

This is a incomplete summary of non-supported features.

## Embedded fonts

Fonts embedded in a PDF file are not extracted by PdfToSvg.NET. Currently this is not a high priority feature to implement, since the main usage is to produce compact SVG markup to be inlined on websites without having to serve the complete PDF files.

Usually subsetted fonts are embedded into PDF files. Embedded fonts might consequently differ from file to file, even for the same original font, depending on the characters used in the file.

To leverage better client side caching of font resources, it is because of this better to use fonts located outside the SVG. By default PdfToSvg.NET will try to detect commonly used fonts and replace them with an appropriate substitute font, but you can also specify a [custom font resolver](font-resolver.md) to specify which font to be used as substitute for the embedded fonts.

## Image formats

The by far most commonly used image formats in PDFs are images encoded with DCTDecode (JPEG) and FlateDecode (PNG alike). Those are supported, with the exception that DCTDecode is only supported for RGB images.

The following image formats are not supported:

* CCITT Fax
* JPEG 2000
* CMYK JPEG
* JBIG2

## Color spaces

The commonly used color spaces are supported and transformed to RGB. PDF does however support a lot of color spaces, and not all of them are supported. If an unsupported color space is encountered, you might see areas that are black, but shouldn't be.

Non-supported color spaces:

* CalGray
* CalRGB
* Lab
* DeviceN
* Pattern
* ICCBased

## Encrypted files

It is possible to encrypt PDF files, with and without a password. PdfToSvg.NET does not support decrypting encrypted PDFs. If an encrypted PDF is encountered, an `EncryptedPdfException` is thrown.

## Other features

The following features are not supported:

* Blending modes
* PDF forms
* Embedded JavaScript
* Annotations
* Document links
