# Limitations

PDF is a very feature rich file format, and PdfToSvg.NET does not support all of them. If a PDF is using an unsupported feature, the output might not look identical to what you see in other PDF readers.

This is a incomplete summary of non-supported features.

## Image formats

The by far most commonly used image formats in PDFs are images encoded with DCTDecode (JPEG) and FlateDecode (PNG alike). Those are supported, with the exception that DCTDecode is not supported for
progressive CMYK or YCCK images.

The following image formats are not supported:

* Progressive CMYK or YCCK JPEG

## Color spaces

The commonly used color spaces are supported and transformed to RGB. For ICCBased color spaces, the converter uses the
profile's `/Alternate` color space when present. Otherwise it selects DeviceGray, DeviceRGB or DeviceCMYK based on the
number of components. This fallback bypasses the ICC profile's color transform.

## Other features

The following features are not supported:

* Interactive PDF form fields
* Embedded JavaScript
* Interactive annotations other than URI hyperlinks
* Links to destinations within the PDF

Annotations with a normal appearance stream can be rendered as static SVG, including form widgets such as checkboxes
and radio buttons. URI hyperlinks are exported subject to the configured link filter.
