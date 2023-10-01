# Supported features

This document summarizes implemented features in PdfToSvg.NET.

✅ = Implemented  ❌ = Not implemented

## Text decoding

Impl | Description
:--: | :----------
✅ | CMap
✅ | ToUnicode
✅ | StandardEncoding
✅ | MacRomanEncoding
✅ | MacExpertEncoding
✅ | WinAnsiEncoding
✅ | PDFDocEncoding
✅ | Custom encoding
✅ | Symbol
✅ | ZapfDingbats


## Fonts

Impl | Description
:--: | :----------
✅ | TrueType fonts
✅ | CFF fonts
✅ | CID fonts
✅ | OpenType fonts
✅ | Type 1 fonts
✅ | Type 3 fonts


## Images

Impl | Description
:--: | :----------
✅ | DCTDecode (JPEG)
✅ | DCTDecode (JPEG) CMYK
✅ | DCTDecode (JPEG) YCCK
❌ | DCTDecode (JPEG) Progressive CMYK
❌ | DCTDecode (JPEG) Progressive YCCK
✅ | FlateDecode without predictor
✅ | FlateDecode with PNG predictor
✅ | FlateDecode with TIFF predictor
✅ | FlateDecode CMYK
✅ | CCITTFaxDecode
❌ | JBIG2Decode
❌ | JPXDecode (JPEG2000)
✅ | Crypt
❌ | External images

## Compression

Impl | Description
:--: | :----------
✅ | FlateDecode
✅ | ASCII85Decode
✅ | ASCIIHexDecode
✅ | RunLengthDecode
✅ | LZWDecode

## Color spaces

Impl | Description
:--: | :----------
✅ | DeviceRGB
✅ | DeviceCMYK
✅ | DeviceGray
✅ | CalRGB
✅ | CalGray
✅ | Lab
❌ | ICCBased
✅ | Pattern
✅ | Indexed
✅ | Separation
✅ | DeviceN

## Graphics state parameter dictionary

Impl | Key | Description
:--: | :-- | :----------
✅ | LW | Line width
✅ | LC | Line cap
✅ | LJ | Line join
✅ | ML | Miter limit
✅ | D | Dash pattern
❌ | RI | Rendering intent
❌ | OP | Overprinting
❌ | op | Overprinting
❌ | OPM | Overprint mode
✅ | Font | [font size]
❌ | BG | Black generation func (RGB->CMYK)
❌ | BG2 | Black generation func (RGB->CMYK)
❌ | UCR | Undercolor-removal func (RGB->CMYK)
❌ | UCR2 | Undercolor-removal func (RGB->CMYK)
❌ | TR | Transfer func
❌ | TR2 | Transfer func
❌ | HT | Halftones
❌ | FL | Flatness tolerance
❌ | SM | Smoothness tolerance
❌ | SA | Automatic stroke adjustment
❌ | BM | Blend mode
✅ | SMask | Soft mask
✅ | CA | Stroking alpha
✅ | ca | Non-stroking alpha
❌ | AIS | Alpha source flag
❌ | TK | Text knockout

## Operators / Text

Impl | Operator | Description
:--: | :------- | :----------
✅ | BT | Begin text
✅ | ET | End text
✅ | T* | Move to start of next line
✅ | Tc | Char spacing
✅ | Td | Move text pos
✅ | TD | Move text pos and set leading
✅ | Tf | Set font and size
✅ | Tj | Show text
✅ | TJ | Show text with positioning
✅ | TL | Set leading
✅ | Tm | Set Tm and Tlm
✅ | Tr | Set rendering mode
✅ | Ts | Text rise
✅ | Tw | Word spacing
✅ | Tz | Horizontal scaling
✅ | ' | Move next line and show text
✅ | " | Word/char spacing, next line, show text

## Operators / Colors

Impl | Operator | Description
:--: | :------- | :----------
✅ | CS | Set color space, stroke
✅ | cs | Set color space, nonstroke
✅ | G | Set gray, stroke
✅ | g | Set gray, nonstroke
✅ | K | Set CMYK, stroke
✅ | k | Set CMYK, nonstroke
✅ | RG | Set RGB, stroke
✅ | rg | Set RGB, nonstroke
✅ | SC | Set color, stroke
✅ | sc | Set color, nonstroke
✅ | SCN | Set ICC color, stroke
✅ | scn | Set ICC color, nonstroke

## Operators / Drawing

Impl | Operator | Description
:--: | :------- | :----------
✅ | b | Closepath, fill, stroke
✅ | B | Fill, stroke
✅ | b* | Closepath, eofill, stroke
✅ | B* | Eofill, stroke
✅ | c | Curve to
✅ | cm | Update transform matrix
✅ | d | Set line dash pattern
✅ | f | Fill non-zero
✅ | F | Fill non-zero
✅ | f* | Fill even-odd
✅ | gs | ExtGState
✅ | h | Close subpath
❌ | i | Set flatness tolerance
✅ | j | Line join style
✅ | J | Line cap style
✅ | l | Line to
✅ | m | Move to
✅ | M | Miter limit
✅ | n | End path without fill/stroke
✅ | q | Save graphics
✅ | Q | Restore graphics
✅ | re | Rectangle
❌ | ri | Rendering intent
✅ | s | Close path and stroke
✅ | S | Stroke
✅ | sh | Paint area shading pattern
✅ | v | Curve to
✅ | w | Line width
✅ | W | Update clip path, non-zero
✅ | W* | Update clip path, even-odd
✅ | y | Curve to

## Operators / Other

Impl | Operator | Description
:--: | :------- | :----------
❌ | BDC | Begin marked-content seq with props
✅ | BI | Begin inline image
❌ | BMC | Begin marked-content seq
❌ | BX | Begin compatiblity section
✅ | d0 | Glyph width (Type 3 font)
✅ | d1 | Glyph width & bbox (Type 3 font)
✅ | Do | Invoke XObject (form & image)
❌ | DP | Define marked-content point
✅ | EI | End inline image
❌ | EMC | End marked-content seq
❌ | EX | End compatibility section
✅ | ID | Begin inline image data
❌ | MP | Define marked-content point
